# EarthWright

Terraforming for Valheim's hoe and cultivator, inside the game's build menu.

## Features
- Brush: size (Alt + mouse wheel), five shapes (circle, square, rectangle, ring, frame), rotation, soft or hard edge,
  grid snapping.
- Exact target heights and three level styles for flat ground (below).
- New hoe and cultivator entries (below); the game's own gain the brush, and each entry's description lists its keys.
- Ramps and curved roads, coloured by slope for carts.
- Undo and redo (`Ctrl + Z` / `Ctrl + Y`) and named area snapshots.
- A preview of every changed point, the cost on the HUD, a world grid and an exact-values panel (`F6`).
- Server rules: costs, height limits per biome (past the game's 8 m too), terrain lock, admin zones; wards cover the
  whole brush.
- Custom entries: server admins add menu entries that run console commands.
- Tools: longer reach, a light, a left-hand torch, levels up to 6; faster sprinting on roads; a seed grid.

## Hoe entries
- Level ground: levels toward the target height in the chosen level style.
- Raise ground / Lower ground: raises or digs by an exact amount, up to the height limits.
- Pathen / Paved road: paints a dirt path, or levels and paves a road.
- Smooth ground: evens out bumps and ridges, keeping the slope.
- Paint ground: paints only: dirt, paved, tilled, grass, original, vegetation and more.
- Reset ground: returns the ground to the world's original height and texture.
- Ramp: three clicks build a ramp: straight, soft joins, soft ends or S-curve.
- Road: click waypoints, then carve a curved road through them.
- Groundbreaker: levels and paves in one swing, clearing trees and rocks first when allowed.
- Clear objects: removes trees, stumps, logs, shrubs, rocks and pickables (when the server allows).
- Terraform (admins): sets the whole brush to the target height in one swing.

## Cultivator entries
- Cultivate / Replant: tills while levelling, or brings the grass back.
- Till: tills without changing the height.
- Uproot: pulls up wild berries, mushrooms, flowers and stones; crops are left alone.

## Level styles
- Ease: moves gently toward the target, as the unmodded game does.
- Step: moves straight toward the target, at most one step per click.
- Instant: sets every point to the target at once: a flat plateau.

## Target heights
- Feet: the ground under you, as the game does.
- Aimed: the ground under the crosshair.
- Continued: the height of earlier flat ground next to the crosshair.
- Locked: one fixed height for every click until released.
- Exact: a typed or stepped height.
- Floor: the top of the building piece under the crosshair.

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
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
