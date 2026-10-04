# Getting Started

This wiki describes version 0.3.1.

EarthWright is terraforming for Valheim's hoe and cultivator, inside the game's own build menu. You still pick an entry, aim and click, but every brush can be resized and reshaped, levelled to an exact height, and previewed point by point before you swing. Server admins decide the rules: costs, height limits, who may dig where.

## What it adds

- A brush for every terrain entry: size, five shapes (circle, square, rectangle, ring, frame), rotation, soft or hard edges, grid snapping.
- Exact target heights: your feet, the aimed ground, a locked height, a typed height, the height of a floor piece, or "continue the flat" of earlier edits.
- Three level styles (Ease, Step, Instant), raise and lower by an exact amount, smoothing, painting and resetting the ground to the world's original.
- Ramps with four profiles, a quick ramp from your feet, and curved roads coloured by slope for carts.
- Undo and redo (Ctrl+Z / Ctrl+Y) and named area snapshots.
- New hoe and cultivator entries: Lower, Smooth, Paint, Reset, Ramp, Road, Groundbreaker, Clear objects, Terraform (admins), Till and Uproot, plus your own entries that run console commands.
- A preview of every point a click changes, the cost of the next swing on the HUD, a world grid and a panel for typing exact values (F6).
- Server rules: stamina, wear and material costs, crafting stations, a cooldown, free build for admins, height limits per biome (also past the game's 8 m), a terrain lock, admin zones, and wards that cover the whole brush.
- Tools: longer reach, a light, a torch in the other hand, upgrade levels up to 6; faster sprinting on roads; a seed grid.

## Install

- EarthWright is needed on the server and on every client, in the same version. A player without it, or with another version, is refused when joining (and a client with it cannot join a server without it).
- Install with r2modman or the Thunderstore app, or put `EarthWright.dll` into `BepInEx/plugins`. It needs BepInExPack Valheim.
- Keep it installed once you have raised or dug the ground more than 8 m: without EarthWright that ground is drawn clamped to the game's own limits.
- In single player and on your own hosted world you count as an admin.

## Configuration

- The settings file is `BepInEx/config/milkyteam.earthwright.cfg`. It is written on the first start, every setting is described in it, and edits apply without a restart.
- Five YAML files sit next to it: `EarthWright.Brushes.yml`, `EarthWright.Costs.yml`, `EarthWright.Entries.yml`, `EarthWright.Limits.yml` and `EarthWright.Zones.yml`. They are reloaded within a few seconds of saving.
- On a server with `Lock Configuration` on (the default), the server's values bind every player; keys and display options always stay each player's own. Admins can still change the server's values from their game.

## Quick start

1. Equip the hoe and open its build menu. The game's four entries are followed by EarthWright's.
2. Select "Level ground" and close the menu. An outline shows the brush on the ground, with dots on every point the click will change and a line of values next to the crosshair.
3. Hold Left Alt and turn the mouse wheel to resize the brush. Press N for another shape, L for another level style.
4. Press K to lock the current target height, then click around: every click levels to that height. PageUp and PageDown move it by 0.1 m.
5. Made a mistake? Ctrl+Z undoes it.
6. Each entry's description in the build menu ends with the keys that work with it.

## Pages

- [Menu Entries](wiki:Menu Entries) - every hoe and cultivator entry and what it does
- [Brush and Target](wiki:Brush and Target) - size, shapes, edges, value keys and target heights
- [Preview and Panel](wiki:Preview and Panel) - outline, changed points, HUD, world grid and the F6 panel
- [Ramps and Roads](wiki:Ramps and Roads) - ramps, the quick ramp and curved roads
- [Undo and Snapshots](wiki:Undo and Snapshots) - undo, redo and named area copies
- [Costs and Stations](wiki:Costs and Stations) - stamina, wear, materials, stations, cooldown, free build
- [Limits and Protection](wiki:Limits and Protection) - height limits, wards, lock, admin zones
- [Clearing and Reset](wiki:Clearing and Reset) - resetting ground and clearing trees and rocks
- [Tools and Extras](wiki:Tools and Extras) - tool levels, reach, light, roads, seed grid, uproot
- [Controls and Hotkeys](wiki:Controls and Hotkeys) - every key and button
- [Configuration Reference](wiki:Configuration Reference) - every setting of the .cfg
- [YAML Files](wiki:YAML Files) - brushes, costs, custom entries, limits, zones, translations
- [Console Commands](wiki:Console Commands) - the `ew` command
- [Multiplayer and Compatibility](wiki:Multiplayer and Compatibility) - servers, sync and other mods

## Links

- Source: https://github.com/geraldjglasgow/ValheimMods
- Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and its version)
