# Code Style

A short list of what we do right now. It will change as the project grows. If something here gets in the way, discuss it and update this file.

## Architecture

**MVP** (Model-View-Presenter) with [VContainer](https://github.com/hadashiA/VContainer) for dependency injection. Unity stays at the edges: views, scopes and a few infrastructure services talk to the engine, the rest is plain C#.

| Layer | Folder | What it does | Uses Unity |
|---|---|---|---|
| Model | `Model/` | Plain data: `GameConfig` (content from the server), `GameState` (the profile the server sends), `FishingSessionState` (one run) | No |
| Domain services | `Services/Domain/` | Game rules: resources, upgrades, quests, fishing runs | No |
| Infrastructure services | `Services/Infrastructure/` | Backend (`IApiClient`), the current profile, server time | Only to wrap a Unity API |
| Presenters | `Hub/UI/`, `Fishing/UI/` | Listen to view and service events, decide what the view shows | No |
| Views | `Hub/UI/`, `Fishing/UI/`, `App/` | MonoBehaviours with UI references: `Show…` methods, `…Clicked` events | Yes |
| Scopes | `App/`, `Hub/`, `Fishing/` | `LifetimeScope`s that register services, views and presenters | Yes |

- Services and presenters are plain C# classes that get their dependencies through the constructor. MonoBehaviours get services through an `[Inject]` method and scene objects through the Inspector.
- A view has no game logic and doesn't know about services. Pure UI state (the open tab, a pulsing button) can stay in the view.
- A presenter works with its view only through the view's methods and events, never through its GameObjects or components.
- Only the server changes `GameState`. A domain service checks what it can locally (to grey out buttons), sends the action through `IApiClient` and passes the call to `ProfileSyncService.TryApplyAsync`, which takes the profile the server returns.
- Everything the backend owns goes through `IApiClient`. A new action means a new endpoint in `Server/` (and in `Server/README.md` and the Postman collection) and a method on `IApiClient`. The rule itself lives only on the server.
- The server in `Server/` is C++ but follows the same naming and formatting; its model structs use camelCase fields to match the JSON.
- New code that needs an engine API (time, scenes, app events, files) gets it from an infrastructure service instead of calling Unity directly.
- Exception: the fishing minigame. Its rules run every frame on scene objects, so they live in `FishingController`, `Hook` and `Fish`, and `FishingHudPresenter` reads them directly.

## Naming

| What | Style | Example |
|---|---|---|
| Classes, methods, properties, enums, events | `PascalCase` | `FishingService`, `StartRun()`, `HitsLeft` |
| Interfaces | `IPascalCase` | `IApiClient` |
| Private fields | `_camelCase` | `_caught`, `_slowdownLeft` |
| Inspector fields | `camelCase` | `[SerializeField] private float sideSpeed` |
| Local variables, parameters | `camelCase` | `var depth = ...` |

- Classes are named after their role: `…Service`, `…View`, `…Presenter`, `…Scope`.
- Methods that may fail return `bool` and start with `Try`: `TryBuy()`, `TryCatch()`.
- Events are named after what happened: `FishShot`.

## Unity

- Inspector fields are `[SerializeField] private`, not `public`. If another script needs the value, add a read-only property.
- Link objects in the Inspector (`[SerializeField] private Hook hook;`) instead of `GameObject.Find`.
- Use `[Header]` and `[Tooltip]` on balance numbers so they're easy to tweak.
- Check Unity objects with `== null`, not `?.` or `??` (they don't work correctly with destroyed objects).
- UI is built in the scene and prefabs; scripts only update it. Buttons call methods through **OnClick** in the Inspector.
- Input goes through the **Fishing** action map (`Assets/Settings/InputSystem_Actions`), not by reading the mouse or keyboard directly.

## Formatting

- Braces on their own line, and always use them, even for one-line `if`s.
- `var` when the type is obvious.
- One-line members can use `=>`: `public bool IsFull => _caught.Count >= Capacity;`
- Member order: fields → constructor → events → properties → Unity methods (`Awake`, `Update`...) → public methods → private methods.

## Files

- One class per file, named after the class. Small enums or data classes used only by that class can stay in the same file.
- Folders in `Scripts/` follow the architecture: `App/` (startup, root scope, scene loading), `Model/`, `Services/Domain/`, `Services/Infrastructure/`, one folder per scene (`Hub/`, `Fishing/`) with its views and presenters in `UI/`, and `Editor/` for editor-only scripts.
- `Prefabs/` uses the same `App/`, `Hub/`, `Fishing/` split.

## Comments

- Every class gets a one-line `/// <summary>` saying what it does.
- Comment *why*, not *what*.
- Unfinished things: `// TODO: what needs doing`.

## Git

- Move or rename assets inside Unity
- Don't commit `Library/`, `Temp/`, `Logs/` or IDE folders (`.gitignore` handles these).
