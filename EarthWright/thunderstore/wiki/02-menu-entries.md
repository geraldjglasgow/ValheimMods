# Menu Entries

EarthWright works through the hoe's and the cultivator's build menus. The game's own terrain entries stay (and gain the brush), and EarthWright adds its own after them. Each entry's description in the menu ends with the keys that work with it, as you have bound them (setting `Show Key Hints In Descriptions`).

## The hoe

In menu order. "Starts at" is the brush an entry has the first time you select it; the `Size Multiplier` setting and `EarthWright.Brushes.yml` can change it.

| Entry | What a click does | Starts at |
| --- | --- | --- |
| Level ground (game) | Levels the ground inside the brush toward the target height in the chosen level style, and paints dirt. | radius 3 m, edge hardness 0.3, max step 1 m, style from `Default Level Style` (Ease) |
| Raise ground (game) | Raises the ground by the set amount and paints dirt a little wider (1.25 times the radius). Repeated clicks add up to the raise limit. | radius 2 m, amount 1 m, hardness 0.8 |
| Pathen (game) | Paints a dirt path; the height stays. | radius 2 m, hard edge |
| Paved road (game) | Levels toward the target height and paves the middle (0.73 times the radius), as the game does. With `Paved Road Levels` off it only paves, across the whole brush. | radius 3 m, hardness 0.3, max step 1 m |
| Lower ground | Lowers the ground by the set amount with a soft edge (a smooth hollow), painting dirt 1.25 times as wide. Repeated clicks dig down to the dig limit. | radius 2 m, amount 1 m, hardness 0 |
| Smooth ground | Evens out bumps and ridges: each point moves toward the average of its neighbours by the strength. The slope itself stays. | radius 3 m, strength 0.5, hardness 0.2 |
| Paint ground | Paints only; the height stays. Its own paint is paved; the paint key picks another. | radius 2 m, hard edge |
| Reset ground | Returns the ground inside the brush to the world's original height and texture. Buildings, trees and rocks are left alone. | radius 3 m, hardness 0.8 |
| Ramp | Click the start, the end, then a third time to build a ramp. See [Ramps and Roads](wiki:Ramps and Roads). | width 4 m |
| Road | Each click places a waypoint; the carve key cuts a curved road through them. See [Ramps and Roads](wiki:Ramps and Roads). | width 4 m |
| Groundbreaker | One swing clears trees, rocks and shrubs inside the brush (only while clearing is switched on), levels toward the target height and paves. | radius 3 m, hardness 0.5, max step 1 m, style from `Default Level Style` |
| Clear objects | Removes trees, stumps, logs, shrubs, rocks and pickables inside the brush. Listed only while `Clearing Enabled` is on. See [Clearing and Reset](wiki:Clearing and Reset). | radius 4 m |
| Terraform (admin) | Sets every point inside the brush to the target height in one swing (Instant), past the height limits while `Terraform Ignores Height Limits` is on. Listed only for admins. | radius 5 m, hardness 0.8 |

## The cultivator

| Entry | What a click does | Starts at |
| --- | --- | --- |
| Cultivate (game) | Evens out the ground toward the target height and tills it. With `Cultivate Levels` off it only tills. | radius 3 m, hardness 0.3, max step 1 m |
| Replant (game) | Brings the grass back: removes dirt, paving and tilled soil. | radius 2.2 m, hard edge |
| Till | Tills the ground without changing its height. | radius 3 m, hard edge |
| Uproot | Pulls up wild pickables inside the brush (berry bushes, mushrooms, flowers, thistle, branches, stones). Crops are left alone. See [Tools and Extras](wiki:Tools and Extras). | radius 3 m |

Seeds and saplings stay as the game has them; the cultivator's seed grid is described in [Tools and Extras](wiki:Tools and Extras).

## Level styles

Entries that level (Level ground, Paved road, Cultivate, Groundbreaker, Terraform) approach the target height in one of three styles. The level style key (L) cycles them; the server's `Default Level Style` sets where every player starts.

| Style | What a click does |
| --- | --- |
| Ease | Moves gently toward the target, at most the max step per click, fading toward a soft edge. This is how the unmodded game levels. |
| Step | Moves straight toward the target by at most the max step per click, at full strength inside the hard part of the brush. |
| Instant | Sets every covered point to the target at once: a flat plateau. A soft edge still blends into the ground around it. |

The max step is the "amount" value of a levelling entry in Ease and Step (0.1 to 1000 m). The server caps it with `Max Step` in section 3.

The hard level key (F9) makes the next click an Instant, hard-edged level to the current target height. It works with a levelling entry or Raise ground selected and costs what a normal click of that entry costs.

## Paints

The paint key (P) cycles what a click paints: the entry's own paint first, then every paint the server lists in `Allowed Paints`.

| Paint | What it does |
| --- | --- |
| entry's own | Whatever the entry paints by itself (dirt for Level ground, paved for Paint ground, and so on). |
| Dirt | Dirt, as a hoe path. |
| Paved | Paved stone. Paving needs a stonecutter nearby while `Paved Road Needs Stonecutter` is on. |
| Cultivated | Tilled soil. In the Deep North it piles snow instead, as the game's cultivator does there. |
| Grass | The game's replant paint: clears dirt, tilling and paving. |
| Original | The world's own ground texture for that place comes back. |
| Vegetation | A grass density brush. The density (0 to 1) is the entry's "amount" value. |
| Clear vegetation | Removes grass and ground clutter. |
| Keep | Changes only the height; the ground keeps its paint. Offered for entries that change the height, and for ramps and roads. |

Painting keeps a crisp edge even with a soft brush: `Paint Edge Hardness` (section 3) is the least hardness paint uses.

## What every entry shares

- The brush keys work with every entry (see [Brush and Target](wiki:Brush and Target)), except that ramps and roads take only a width, a paint and a target height.
- EarthWright's own entries cost no materials and need no crafting station by themselves. What a swing costs is set in section 8, see [Costs and Stations](wiki:Costs and Stations).
- You learn EarthWright's entries silently, without a "new piece" message for each.
- The server can switch off any entry, the game's included, with the `Enable ...` settings of section 12. A switched-off entry disappears from every player's menu.
- With `Full Build Menu` on (your own choice, default on) the hoe and cultivator get the full build menu the hammer has: a search field, recent pieces and favourites (middle click an entry).
- Tool levels can lock entries, shapes and level styles until the tool is upgraded (`Level Unlocks`, see [Tools and Extras](wiki:Tools and Extras)).

## Custom entries

Server admins can add entries of their own to either menu in `EarthWright.Entries.yml`. A custom entry runs a console command when clicked, filled in with the brush's position, size and target height, for example `ew reset {radius}` or the admin command `ew terrain level {radius} {target} at=brush shape=square`. Custom entries can be admin-only and can repeat while the button is held. The file format is in [YAML Files](wiki:YAML Files); the commands are in [Console Commands](wiki:Console Commands).

A custom entry is charged like a click of an EarthWright entry before its command runs.
