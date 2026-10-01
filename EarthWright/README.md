# EarthWright

Terraforming for Valheim's hoe and cultivator, inside the game's build menu.

## Features
- Brush: size (Alt + mouse wheel), five shapes, rotation, soft or hard edge, grid snapping.
- Exact target heights, instant plateaus, raise and lower by exact amounts, smooth, paint and reset.
- Ramps and curved roads, coloured by slope for carts.
- Undo and redo (`Ctrl + Z` / `Ctrl + Y`).
- New hoe and cultivator entries (Groundbreaker, Uproot and more); each entry's description lists its keys.
- A preview of every changed point, the cost on the HUD, a world grid and an exact-values panel (`F6`).
- Server rules: costs, height limits per biome (past the game's 8 m too), terrain lock, admin zones; wards cover the
  whole brush.
- Tools: longer reach, a light, a left-hand torch, levels up to 6; faster sprinting on roads; a seed grid.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `EarthWright.dll` in
`BepInEx/plugins`. Keep it installed once you have raised or dug past 8 m, or that ground is drawn clamped to the
game's limits.

## Configuration
`BepInEx/config/milkyteam.earthwright.cfg` and YAML files (brushes, costs, entries, limits, zones). Every setting is
described in the file and applies without a restart; the server's values bind every player, keys and display options
stay your own. Console: `ew help`; all commands with arguments:
https://github.com/geraldjglasgow/ValheimMods/blob/main/EarthWright/CLAUDE.md#console-commands

## Links
Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
