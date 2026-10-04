# Costs and Stations

The server decides what terrain work costs (section 8 of the .cfg and `EarthWright.Costs.yml`). At the defaults everything costs what it costs in the unmodded game: the game's stamina and tool wear per swing, stone for Raise ground and Paved road, a workbench nearby for Raise ground and a stonecutter for Paved road. Costs apply to terrain entries of the hoe and cultivator only; every other tool and piece keeps the game's costs.

## What one swing costs

| Cost | How it is worked out | Settings |
| --- | --- | --- |
| Stamina | Vanilla: the game's own. Off: none. Fixed: `Stamina Per Use` (5). Scaled: the game's cost times `Stamina Factor`, times (brush radius / the entry's normal radius) to the power `Stamina Radius Exponent` (1: grows with the radius, 2: with the area, 0: ignores it). A smaller brush than normal then costs less. A longer reach never lowers it. | `Stamina Mode` (Vanilla), `Stamina Per Use`, `Stamina Factor`, `Stamina Radius Exponent` |
| Stamina skill | A skill of your choice lowers the stamina: by `Stamina Skill Reduction` (50 %) at skill level 100, proportionally less below. | `Stamina Skill` (None), `Stamina Skill Reduction` |
| Tool wear | The game's durability loss per swing, times `Tool Wear Factor`, times (brush radius / normal radius) to the power `Tool Wear Radius Exponent` (0 by default: size does not matter). Off: terrain work never wears the tool. | `Tool Wear`, `Tool Wear Factor`, `Tool Wear Radius Exponent` |
| Materials | The entry's own materials (stone for Raise ground and Paved road; EarthWright's own entries have none), or the list `EarthWright.Costs.yml` gives it. Amounts grow with (brush radius / normal radius) to the power `Material Radius Exponent` (0 by default), rounded up and never below 1. | `Charge Materials`, `Material Radius Exponent` |
| Extra item | One more item every terrain swing costs, for example Stone, Wood or Resin. Charged even while `Charge Materials` is off. | `Extra Item` (empty: none), `Extra Item Amount` (1) |
| Volume stone | `Volume Cost Item` (Stone) per cubic metre of ground raised and per square metre of ground newly paved. Fractions carry over to your next swing, so many small swings cost the same as one big one. Lowering is free; so are resets and admin work. | `Stone Per Cubic Metre Raised` (0: off), `Stone Per Square Metre Paved` (0: off), `Volume Cost Item` |

The HUD line next to the crosshair shows what one swing of the selected entry costs at the current size: have/need per item, red where you are short (`Show Costs`, your own setting). A click you cannot pay is refused before anything happens, and a refused click costs nothing.

## Crafting stations

| Setting | Default | What it does |
| --- | --- | --- |
| `Hoe Needs Stations` | on | Hoe entries need their station nearby, as in the game (a workbench for Raise ground, a stonecutter for Paved road). Off: the hoe works anywhere. |
| `Cultivator Needs Stations` | on | The same for cultivator entries that have a station. |
| `Modded Entries Need Stations` | on | The same for terrain entries other mods add. |
| `Paved Road Needs Stonecutter` | on | Paving with any entry needs a stonecutter nearby, also when the paint key or Paint ground paves. Off: paving works anywhere. Groundbreaker has its own switch. |
| `Groundbreaker Needs Stonecutter` | off | Groundbreaker needs a stonecutter when it paves. Off: it works anywhere, whatever the paved road switch says. |

`EarthWright.Costs.yml` can give an entry another station or none (`station`, `stationRequired`). A world with the game's "no workbench" setting, and free build, need no stations at all.

## Special entries

Ramp, Road, Clear objects, Groundbreaker, Uproot and custom entries are charged once per build, carve or click, as one swing of that entry: stamina, wear, its materials and the extra item, and volume stone where ground is raised or paved. Clear objects and Uproot charge nothing when there is nothing to remove. Reset ground is charged as a normal swing (but owes no volume stone); the reset keys and `ew reset` are free.

## Cooldown

`Cooldown` (0 s, off) is the wait between two terrain uses of a player: brush swings, and ramp, road, clearing and custom-entry work. Hold-to-repeat never runs faster than it. A player's `ew forestry` and `ew debris` pay a click of Clear objects and wait like one. Undo, redo, the reset keys, `ew reset` and the admin commands are never held back.

## Free build

The free build key (F7) switches free build on and off for you while a terrain tool is out, if the server allows it: `Allow Free Build` is Nobody, Admins (default; the server's admin list and the host) or Everyone. While free build is on, your terrain work costs no stamina, tool wear, materials or volume stone and needs no station; the cost line says "Free build". It switches itself off when the server stops allowing it, and starts off every time you join.

The game's own "no cost" cheat (devcommands) and the world's "no build cost" setting also make terrain materials free.

## Per-entry overrides

`EarthWright.Costs.yml` sets costs for single entries by their prefab name: `resources` (replaces the entry's materials; `{}` makes it free), `stamina` (replaces the stamina mode for that entry; the stamina skill still applies), `durability` (multiplies its tool wear), `station` and `stationRequired`. Details in [YAML Files](wiki:YAML Files).
