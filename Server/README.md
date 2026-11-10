# CatchFish server

The authoritative backend: it owns the game config and every player's profile. The client sends
actions ("buy this gear", "complete this quest", "this run ended with these fish"); the server checks
them against the config, changes the profile, saves it and returns it. The client never uploads a profile.

C++17, [cpp-httplib](https://github.com/yhirose/cpp-httplib), [nlohmann/json](https://github.com/nlohmann/json)
and SQLite. CMake downloads all three on the first configure, so all you need is CMake and a C++ compiler.

| File | What it does |
|-|-|
| `src/main.cpp` | Options, HTTP routes, turning errors into responses |
| `src/GameRules.cpp` | The rules below |
| `src/Storage.cpp` | SQLite: one `players` row per player, the profile as JSON |
| `src/Model.h` | C++ copies of the client's `GameConfig`, `GameState` and `RunResult` |

## Build and run

```bash
cmake -S Server -B Server/build -DCMAKE_BUILD_TYPE=Release
cmake --build Server/build
Server/build/catchfish-server
```

Options:

| Option | Default |
|-|-|
| `--host <address>` | `127.0.0.1`: only this machine (and Caddy on it) can connect. `0.0.0.0` accepts other devices directly, over plain HTTP |
| `--port <n>` | `8080` |
| `--config <path>` | `Assets/CatchFish/Resources/LocalBackend/game_config.json`, the same file the client's local mode uses |
| `--db <path>` | `Server/catchfish.db` (delete it to wipe all players) |

The config is read once at startup: restart the server after changing it.

## Connecting the client

`RootScope.prefab` → **Backend**: tick **Use Server** and set **Server Url**.

- The hosted server: `https://fish.thevals.ru:1337` (the default **Server Url**)
- A server running on the same machine as the editor: `http://localhost:8080`

Android builds refuse plain `http://`, so builds always use the hosted address.

With **Use Server** off, the game runs against `LocalApiClient`, a C# copy of these rules that keeps
the profile in a file on the device. Keep both in sync when the rules change.

## Deploying (fish.thevals.ru)

The server runs on the Debian 12 laptop that also hosts Foundry (`foundry.thevals.ru:30000`).
[Caddy](https://caddyserver.com) serves HTTPS on public port 1337 and passes requests to the server, which only
listens on `127.0.0.1:8080`. Caddy gets the Let's Encrypt certificate and renews it by itself.

```
phone ──https──▶ router :1337 ──▶ laptop: Caddy :1337 ──http──▶ catchfish-server 127.0.0.1:8080
```

**1. DNS and router** (once)

- At Selectel (the DNS host for `thevals.ru`), add `fish.thevals.ru`: an A record to the home IP like `foundry`'s
  (`188.242.226.0` on 2026-11-03), or a CNAME to `foundry.thevals.ru` so both follow if the IP changes.
- On the router, forward TCP **1337** and TCP **80** to the laptop. Port 80 is for Let's Encrypt: it checks the
  domain there (never on 1337) when issuing the certificate and at every renewal, about every 60 days.
  Without port 80 the alternative is the DNS challenge: a Caddy build with a Selectel DNS module
  (look for "selectel" on [caddyserver.com/download](https://caddyserver.com/download)) and a Selectel API token.

**2. Build the server** (on the laptop)

```bash
sudo apt install build-essential cmake git
git clone --filter=blob:none --sparse https://github.com/theval-s/CatchFish.git ~/CatchFish
cd ~/CatchFish
git sparse-checkout set Server Assets/CatchFish/Resources/LocalBackend
cmake -S Server -B Server/build -DCMAKE_BUILD_TYPE=Release
cmake --build Server/build
```

The sparse checkout only fetches the server and the game config, not the Unity project and its art.

**3. Run it as a service**, so it starts at boot and restarts if it crashes:

```bash
mkdir -p ~/.config/systemd/user
cp ~/CatchFish/Server/catchfish.service ~/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user enable --now catchfish
sudo loginctl enable-linger $USER
```

`enable-linger` keeps your user's services running without a login. Logs: `journalctl --user -u catchfish -f`.
Check: `curl http://127.0.0.1:8080/time`. If something else already uses 8080 there, add `--port` to `ExecStart`
in the service file and change the Caddyfile to match.

**4. Caddy**

```bash
sudo apt install caddy
```

Replace everything in `/etc/caddy/Caddyfile` with the block below. The default file serves a welcome page on
port 80, which would become public once that port is forwarded.

```
fish.thevals.ru:1337 {
	reverse_proxy 127.0.0.1:8080
}
```

Then `sudo systemctl reload caddy` and watch `journalctl -u caddy -f` for "certificate obtained successfully".
If a firewall (ufw) is on, allow ports 80 and 1337.

**5. Check from outside** (a phone on mobile data): `https://fish.thevals.ru:1337/time`.

**Updating**

```bash
cd ~/CatchFish && git pull && cmake --build Server/build && systemctl --user restart catchfish
```

The config is read at startup, so a config change needs the restart too. Players live in
`~/CatchFish/Server/catchfish.db`: copy it while the service is stopped to back them up.

Anyone who finds the address can create guest players. That's fine for a course demo; the server caps request
bodies at 64 KB.

## Docker

An alternative to steps 2–3 above: `Server/Dockerfile` builds the server from source in a Debian 12 image.
Run these from the repo root, since the image also takes the game config from `Assets/`:

```bash
docker build -f Server/Dockerfile -t catchfish-server .
docker run -d --name catchfish --restart unless-stopped -p 127.0.0.1:8080:8080 -v catchfish-data:/data catchfish-server
```

- `127.0.0.1:8080` keeps the port off the network, and Caddy reaches it exactly as it reaches the native server.
  Without the `127.0.0.1:`, Docker opens 8080 to everyone and bypasses ufw.
- Players live in the `catchfish-data` volume and survive rebuilds.
- The config is baked into the image: after `git pull`, build again, then `docker rm -f catchfish` and run again.
- Logs: `docker logs -f catchfish`.

## API

JSON everywhere, camelCase field names, the same shapes as the client's `GameConfig` and `GameState`.
Times are ISO 8601 UTC strings (`2026-11-03T12:00:00.000Z`).

Everything except `/config`, `/time` and `/auth/guest` needs `Authorization: Bearer <token>`.

| Method | Path | Body | Returns |
|-|-|-|-|
| `GET` | `/config` | | `GameConfig` |
| `GET` | `/time` | | `{ "now": time }` |
| `POST` | `/auth/guest` | | `{ "token": string }`: a new player with a new profile |
| `GET` | `/profile` | | `GameState` |
| `POST` | `/gear/buy` | `{ "gearId": string }` | `GameState` |
| `POST` | `/quests/complete` | `{ "questId": int }` | `GameState` |
| `POST` | `/runs/start` | | `GameState` with `activeRun` set |
| `POST` | `/runs/complete` | `{ "runId": string, "shotFish": { fishId: count } }` | `GameState` with `activeRun` null |

Errors come back as `{ "error": code, "message": text }`:

| Status | `error` | When |
|-|-|-|
| 400 | `bad_request` | Malformed JSON or a missing field |
| 400 | `unknown_gear`, `unknown_quest`, `unknown_fish` | The id isn't in the config |
| 400 | `invalid_run` | The run's results aren't possible with its gear (see below) |
| 401 | `unauthorized` | No token, or the token isn't known (the client then registers again) |
| 409 | `gear_maxed` | The gear is already at its max level |
| 409 | `not_enough_resources` | The price or the quest's requirements aren't covered |
| 409 | `quest_not_active` | The quest isn't open for this player |
| 409 | `no_active_run` | `runId` isn't the player's current run |

## Rules

- **New player**: the profile starts with the config's `startingQuests` open, everything else at zero.
- **Buy gear**: the level must be below `maxLevel` and `dr` must cover `baseCost * costGrowth^level`.
  Spends `dr`, raises the level.
- **Complete quest**: the quest must be active and every requirement covered. Spends the requirements,
  moves the quest to `completed` and opens the quests in `unlocksId`.
- **Start run**: records `activeRun`: an id, the start time, and the line length and hook capacity
  at the current gear levels. A new start replaces a run that was never completed.
- **Complete run**: the client reports which fish were shot, not how much they were worth. The server
  checks the run, then adds the fish values to `dr` (never below 0), collectible fish to `fishCollection`,
  and counts the run in `runsCompleted`. The checks, since the minigame itself runs on the client:
  - `runId` is the active run;
  - every fish exists, and no deeper than the line could reach (5 m margin);
  - no more fish than the hook's capacity;
  - the run lasted at least 2 seconds.
