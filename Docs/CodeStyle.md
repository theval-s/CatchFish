# Code Style

A short list of what we do right now. It will change as the project grows. If something here gets in the way, discuss it and update this file.

## Naming

| What | Style | Example |
|---|---|---|
| Classes, methods, properties, enums, events | `PascalCase` | `GameManager`, `StartRun()`, `HitsLeft` |
| Private fields | `_camelCase` | `_caught`, `_slowdownLeft` |
| Inspector fields | `camelCase` | `[SerializeField] private float sideSpeed` |
| Local variables, parameters | `camelCase` | `var depth = ...` |

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
- Member order: fields → events → properties → Unity methods (`Awake`, `Update`...) → public methods → private methods.

## Files

- One class per file, named after the class. Small enums or data classes used only by that class can stay in the same file.
- `Scripts/` for gameplay, `Scripts/UI/` for UI, `Prefabs/`, `Scenes/`.

## Comments

- Every class gets a one-line `/// <summary>` saying what it does.
- Comment *why*, not *what*.
- Unfinished things: `// TODO: what needs doing`.

## Git

- Move or rename assets inside Unity
- Don't commit `Library/`, `Temp/`, `Logs/` or IDE folders (`.gitignore` handles these).
