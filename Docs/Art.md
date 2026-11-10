# Art

Where art goes in and how to set it. Everything here is set in the Inspector, without code.

## Files

- Images go in `Assets/CatchFish/Art/<area>/` (`Fish`, `Fishing`, `UI`, …). `Art/Placeholder/Square.png` is the
  grey square every placeholder uses.
- PNG, JPG, PSD, fonts and sounds are stored with Git LFS: run `git lfs install` once before committing.
- Import images as **Sprite (2D and UI)**. Anything that stretches (panels, buttons) needs 9-slice borders,
  set in the Sprite Editor, or its corners stretch too.
- Art is referenced from prefabs and ScriptableObjects, not from scenes, so a scene change and an art change
  don't collide in Git.

## Where each piece goes

| What | Where | Notes |
|-|-|-|
| Fish | `Settings/FishSkinCatalog.asset`: one entry per fish id (`f1`, `f2`, …) | Draw them facing right; the game mirrors fish swimming left. Set Pixels Per Unit to the image's height in pixels: the fish config's `size` then sets how tall it is. A new fish id without an entry shows up magenta |
| Boat, fisherman, sky and shore | `Prefabs/Fishing/Surface.prefab` | Placed at the water surface (y = 0). Replace the placeholder squares' sprites, or add more sprites |
| Hook | `Prefabs/Fishing/HookVisual.prefab` | Centered on the hook |
| Water | `Scenery` in the Fishing scene → **Water Sprite** | Tiled over the whole depth and darkened from **Shallow Water** to **Deep Water** (set both to white to show it as drawn). Import with Mesh Type: Full Rect, Wrap Mode: Repeat |
| Sky color, depth stripes | `Scenery` → Colors | |
| Fishing line | `Hook` in the Fishing scene → Line | Color, width, and the rod tip it hangs from (match it to the rod in `Surface.prefab`) |
| Quest, gear and daily reward cards | `Prefabs/Hub/QuestItem`, `GearUpgradeItem`, `DailyRewardDay` | Styling the prefab restyles every card. Daily day colors (claimed, next, later) are fields on `DailyRewardDay` |
| Hub, popups, tab bar | The Hub scene's `UI` canvas | For a background, add a stretched Image as the canvas's first child |
| Loading and startup error screen | `Prefabs/App/RootScope.prefab` → Loading Screen | |

## Draw order in the Fishing scene

Sorting Order on each SpriteRenderer, back to front: water −10, depth stripes −9, fish 1, line 2, hook 3,
boat and fisherman 4.

## Text

UI text uses TextMesh Pro with LiberationSans, which has no reliable Cyrillic: Russian text needs another font
with a TMP font asset. `LiberationSans SDF - Fallback.asset` changes by itself whenever new characters are drawn;
don't commit it with art changes.
