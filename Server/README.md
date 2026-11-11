# CatchFish server

The authoritative backend: it owns the game config and every player's profile. The client sends
actions ("buy this gear", "complete this quest", "this run ended with these fish", "claim today's reward");
the server checks them against the config, changes the profile, saves it and returns it. The client never
uploads a profile, and the game can't run without a server.

C++17, [cpp-httplib](https://github.com/yhirose/cpp-httplib), [nlohmann/json](https://github.com/nlohmann/json),
SQLite and [PicoSHA2](https://github.com/okdshin/PicoSHA2). CMake downloads all of them on the first configure,
so all you need is CMake and a C++ compiler.

| File | What it does |
|-|-|
| `src/main.cpp` | Options, the dev key, starting the HTTP server |
| `src/GameApi.cpp` | Routes the game uses: sign-in, config, profile, player actions |
| `src/DevApi.cpp` | `/dev/…` routes for the team: live config, players |
| `src/GameRules.cpp` | The rules below, and checking a config before it goes live |
| `src/Accounts.cpp` | Guest sign-in and sessions |
| `src/Content.cpp` | The live config: loaded from the database, seeded from `content/game_config.json` |
| `src/Storage.cpp` | SQLite tables and their migrations |
| `src/Model.h` | C++ copies of the client's `GameConfig`, `GameState` and `RunResult` |
| `content/game_config.json` | The config a new database starts with |
| `CatchFish.postman_collection.json` | Every request, ready for Postman |

## Build and run

```bash
cmake -S Server -B Server/build -DCMAKE_BUILD_TYPE=Release
cmake --build Server/build
CATCHFISH_DEV_KEY=<a long random key> Server/build/catchfish-server
```

| Option | Default |
|-|-|
| `--host <address>` | `127.0.0.1`: only this machine (and Caddy on it) can connect. `0.0.0.0` accepts other devices directly, over plain HTTP |
| `--port <n>` | `8080` |
| `--db <path>` | `Server/catchfish.db` (delete it to wipe all players and the live config) |
| `--config <path>` | `Server/content/game_config.json`. Read only when the database has no config yet |

`CATCHFISH_DEV_KEY` (an environment variable, at least 16 characters) turns on the dev API. Without it every
`/dev/…` request answers 404. Make one with `openssl rand -hex 32`. It's the only thing protecting the dev API.

## Connecting the client

`RootScope.prefab` → **Backend** → **Server Url**:

- the hosted server: `https://fish.thevals.ru:1337` (the default);
- a server on the same machine as the editor: `http://localhost:8080`.

Android builds refuse plain `http://`, so builds always use the hosted address.

## Accounts and sessions

A **player** has **identities**, the ways they sign in, and **sessions**, the bearer token every game request
carries. Today the only identity is a guest; Google sign-in would be another identity on the same player.

1. On first launch the client calls `POST /auth/guest` and gets a `guestSecret` (kept on the device) and a session.
2. On later launches it signs in with the secret: `POST /auth/guest/login` → a new session.
3. Sessions last 30 days. On `session_expired` the client signs in again with the secret.
4. If the server doesn't know the secret (`invalid_credentials`), the client does **not** quietly make a new player:
   the startup screen says so and offers Retry or "Start as new player". A server started on the wrong database
   would otherwise lose every player that connected to it.

Guest secrets and session tokens are 256-bit random values. The database stores only their SHA-256, so a copy
of it doesn't let anyone sign in.

## Live config and the dev API

The config lives in the database, not in a file the server reads at startup. `GET /config` serves the current
one with a `version`, and the dev API changes it without a restart:

- every change is checked as a whole (ids unique, every quest and resource id it mentions exists, the gear the
  game needs is there, the daily track has at least one day) and refused with `invalid_config` and the reason
  if anything is off; the config in use stays as it was;
- each accepted change bumps `version`;
- players pick up changes on their next request: a quest with `"autoStart": true` opens for everyone who hasn't
  done it, and a deleted quest disappears from players' open quests. The client reads the config once at startup,
  so it shows a change after a restart.

Import `CatchFish.postman_collection.json` into Postman, then set the collection variables `baseUrl` and `devKey`.
"Create guest" and "Sign in as guest" store the session for the player requests.

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
git sparse-checkout set Server
cmake -S Server -B Server/build -DCMAKE_BUILD_TYPE=Release
cmake --build Server/build
```

The sparse checkout only fetches the server, not the Unity project and its art.

**3. The dev key, and the server as a service** that starts at boot and restarts if it crashes:

```bash
echo "CATCHFISH_DEV_KEY=$(openssl rand -hex 32)" > ~/.config/catchfish.env
chmod 600 ~/.config/catchfish.env
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

The database is migrated on start (see Database). A changed `content/game_config.json` doesn't reach a
running database: upload it with "Replace config" in Postman. Players and the live config live in
`~/CatchFish/Server/catchfish.db`: copy it while the service is stopped to back it up.

Anyone who finds the address can create guest players. That's fine for a course demo; the server caps request
bodies at 256 KB.

## Docker

An alternative to steps 2–3 above: `Server/Dockerfile` builds the server from source in a Debian 12 image.
Run these from the repo root:

```bash
docker build -t catchfish-server Server
docker run -d --name catchfish --restart unless-stopped -p 127.0.0.1:8080:8080 -v catchfish-data:/data --env-file ~/.config/catchfish.env catchfish-server
```

- `127.0.0.1:8080` keeps the port off the network, and Caddy reaches it exactly as it reaches the native server.
  Without the `127.0.0.1:`, Docker opens 8080 to everyone and bypasses ufw.
- Players and the live config live in the `catchfish-data` volume and survive rebuilds.
- After `git pull`, build again, then `docker rm -f catchfish` and run again. Logs: `docker logs -f catchfish`.

## API

JSON everywhere, camelCase field names, the same shapes as the client's `GameConfig` and `GameState`.
Times are ISO 8601 UTC strings (`2026-11-03T12:00:00.000Z`). Give every POST a body, `{}` at least:
cpp-httplib waits 5 s for a body when there's no `Content-Length`.

Everything except `/config`, `/time` and `/auth/guest…` needs `Authorization: Bearer <sessionToken>`.

| Method | Path | Body | Returns |
|-|-|-|-|
| `GET` | `/config` | | `GameConfig` with `version` |
| `GET` | `/time` | | `{ "now": time }` |
| `POST` | `/auth/guest` | | `{ "guestSecret", "playerId", "sessionToken", "expiresAt" }`: a new player |
| `POST` | `/auth/guest/login` | `{ "guestSecret": string }` | `{ "playerId", "sessionToken", "expiresAt" }` |
| `POST` | `/auth/logout` | | `{}`; this session stops working |
| `GET` | `/profile` | | `GameState` |
| `POST` | `/gear/buy` | `{ "gearId": string }` | `GameState` |
| `POST` | `/quests/complete` | `{ "questId": int }` | `GameState` |
| `POST` | `/runs/start` | | `GameState` with `activeRun` set |
| `POST` | `/runs/complete` | `{ "runId": string, "shotFish": { fishId: count } }` | `GameState` with `activeRun` null |
| `POST` | `/daily/claim` | | `GameState` |

Dev API, every request with the header `X-Dev-Key: <key>`:

| Method | Path | Body | What it does |
|-|-|-|-|
| `GET` | `/dev/config` | | The live config with its `version` |
| `PUT` | `/dev/config` | the whole config | Replaces it |
| `PUT` | `/dev/config/daily` | `{ "utcOffsetHours", "days": [ { resourceId: amount } ] }` | Replaces the daily track |
| `PUT` | `/dev/config/quests/{id}` | a quest (its `id` comes from the path) | Adds the quest or replaces it |
| `DELETE` | `/dev/config/quests/{id}` | | Removes it; refused while another quest or `startingQuests` points at it |
| `GET` | `/dev/players?limit=50` | | Recently active players: id, dates, runs, `dr` |
| `GET` | `/dev/players/{id}` | | Their `GameState` |
| `PUT` | `/dev/players/{id}` | a `GameState` | Replaces it: GET, edit, PUT back |

The config changes answer with the new config. Errors come back as `{ "error": code, "message": text }`:

| Status | `error` | When |
|-|-|-|
| 400 | `bad_request` | Malformed JSON or a missing field |
| 400 | `unknown_gear`, `unknown_quest`, `unknown_fish` | The id isn't in the config |
| 400 | `invalid_run` | The run's results aren't possible with its gear (see Rules) |
| 400 | `invalid_config` | A dev change would break the config; `message` says how |
| 401 | `unauthorized` | No session token, or an unknown one |
| 401 | `session_expired` | The session is over 30 days old: sign in again |
| 401 | `invalid_credentials` | No player has this guest secret |
| 403 | `forbidden` | Wrong or missing `X-Dev-Key` |
| 404 | `not_found` | Unknown path, player or quest; or the dev API is off |
| 409 | `gear_maxed` | The gear is already at its max level |
| 409 | `not_enough_resources` | The price or the quest's requirements aren't covered |
| 409 | `quest_not_active` | The quest isn't open for this player |
| 409 | `no_active_run` | `runId` isn't the player's current run |
| 409 | `already_claimed` | Today's daily reward is already taken |

## Rules

- **New player**: the profile starts with the config's `startingQuests` and auto-start quests open,
  everything else at zero.
- **Every request with a profile** first brings it up to date with the config: auto-start quests the player
  hasn't done are opened, open quests that no longer exist are dropped.
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
- **Daily reward**: one claim per calendar day, where the day starts at midnight `utcOffsetHours` from UTC
  (+3 = Moscow). The claim gives `days[daily.nextDay]` and moves `nextDay` on; after the last day it goes back
  to the first. Missed days cost nothing: the next claim gives the next day in the track. If the track gets
  shorter, `nextDay` wraps around it.

## Database

| Table | Rows |
|-|-|
| `players` | `id`, `profile` (the `GameState` as JSON), `created_at`, `updated_at` |
| `identities` | `provider` (`guest`; later `google`), `subject` (for a guest: the SHA-256 of the secret), `player_id` |
| `sessions` | `token_hash` (SHA-256), `player_id`, `created_at`, `expires_at`; expired ones are deleted at sign-in |
| `content` | One row: the live config as JSON and its `version` |

`PRAGMA user_version` holds the schema version, and `Storage::Migrate` upgrades older databases on start.
Version 1 came from the first server, where a token in `players` was the whole account: each token became a
guest secret, so clients that kept it still sign in (the client moves its old `CatchFish.ServerToken` over).
