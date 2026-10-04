# Tools and Extras

What the hoe and cultivator themselves gain (section 13), sprinting on roads (section 15) and the cultivator's extras (section 14). All of it is set by the server except the light's colour and the seed grid, which are your own.

## Tool levels

The hoe and the cultivator can be upgraded at their crafting station like any other tool.

| Setting | Default | What it does |
| --- | --- | --- |
| `Hoe Max Level`, `Cultivator Max Level` | 6 | The highest upgrade level (1 to 10). A tool already above a lowered maximum keeps its level. |
| `Hoe Upgrade Cost`, `Cultivator Upgrade Cost` | empty | What one upgrade costs, as `Item:Amount` pairs separated by commas (for example `Wood:5, Flint:3`). The game multiplies the amounts for higher levels as it does for every tool. Empty: the game's own cost. |
| `Hoe Durability Per Level`, `Cultivator Durability Per Level` | 200 | Durability gained per upgrade level. |
| `Upgrade Station Levels` | `1, 2, 3, 4, 5, 5` | The crafting station level each tool level needs: the first number to craft the tool, the next ones for each upgrade; levels past the end use the last number. Empty: the game's rule (one station level per tool level), which a workbench cannot meet past level 5. |
| `Radius Per Level` | empty | The largest brush radius for each tool level, for example `4, 6, 8, 12, 16, 20`; levels past the end use the last number. Empty: the level does not limit the radius. |
| `Level Unlocks` | empty | What each level unlocks, as `feature:level` pairs, for example `square:2, lower:3, ramp:4, instant:5`. Features not listed are always available. Empty: nothing is locked. |

Features for `Level Unlocks`:

- EarthWright's entries by name: `lower`, `smooth`, `paint`, `reset`, `ramp`, `road`, `groundbreaker`, `clear`, `terraform`, `till`, `uproot`; a custom entry as `custom_<id>`.
- The game's entries by prefab name: `mud_road_v2`, `raise_v2`, `path_v2`, `paved_road_v2`, `cultivate_v2`, `replant_v2`.
- Brush shapes: `circle`, `square`, `rectangle`, `ring`, `frame`.
- Level styles: `ease`, `step`, `instant` (the hard level key counts as instant).

A locked shape or style is skipped by its key. Selecting a locked entry shows why in the preview, and a click with it is refused with the level it needs. Undo, the reset keys and console commands are never gated by tool levels.

## While a terrain tool is in hand

| Setting | Default | What it does |
| --- | --- | --- |
| `Reach` | 20 m | How far away you can use the hoe or cultivator (the game's reach is 5 m; 5 to 50). The game's reach comes back when you put the tool away. |
| `Movement Speed` | 1.1 | Running and sprinting speed as a multiple of normal. Walking, crouching and swimming are unchanged. |
| `Torch In Left Hand` | on | A torch stays in your left hand: equipping the tool keeps the torch, and equipping a torch keeps the tool. Every player sees both hands. |
| `Tool Light` | on | The tool gives off light while held, seen by every player. |
| `Light Range`, `Light Intensity` | 8 m, 1.2 | How far the light reaches and how bright it is. |
| `Light Colour` | `FFD999FF` (warm white) | Your own setting: the colour you see the light in, on your tool and on other players'. |

Reach, speed and the torch apply while EarthWright is on for you (`Use EarthWright`); the light follows the server's switches.

## Road travel

Sprinting on painted ground is faster and costs less sprint stamina:

| Setting | Default | Ground |
| --- | --- | --- |
| `Dirt Road Bonus` | 10 % | Dirt: hoe paths and levelled ground. |
| `Paved Road Bonus` | 20 % | Paved roads. |

0 turns a bonus off. Standing on a building or anything but the ground counts as no road; a jump in the middle of a road keeps the bonus.

## Cultivator extras

### Seed grid

With the cultivator out and a seed or sapling selected, the seed grid key (I) switches the seed grid on and off (`Seed Grid`, your own setting, off at first). Seeds and saplings then snap to a grid on the ground, so crops line up in rows; the HUD shows the spacing. `Seed Grid Spacing` 0 uses each plant's own spacing (twice its grow radius, the closest crops can stand and still grow). The game's own checks (cultivated ground, wards and so on) are redone for the snapped point, so what the ghost shows is what the click plants.

### Cultivate anywhere

With `Cultivate Anywhere` on (off by default), the cultivator's ground entries (Cultivate, Replant, Till) also work where the game refuses with "needs dirt": bare rock, cleared or paved ground. Plants still need the right biome to grow, and dungeons, no-build places and wards are still checked.

### Uproot

The cultivator's Uproot entry removes the wild pickables inside the brush: berry bushes, mushrooms, flowers, thistle, branches, stones and the like. "Wild" means something the world's own vegetation spawns, so crops, treasure and items placed by locations (dragon eggs, remains, dvergr loot) are never uprooted; a plant that saplings also grow into counts as a crop while it stands on cultivated ground.

- With `Uproot Picks Items` on (default) each plant is picked first, so its berries, mushrooms or stones drop. Off: wild plants are removed and nothing drops.
- Plants under someone else's ward, or refused by the protection rules, are skipped. The whole click is refused in dungeons and no-build places.
- One click is charged once, and only when something will be uprooted. Uprooted plants do not come back with undo.
