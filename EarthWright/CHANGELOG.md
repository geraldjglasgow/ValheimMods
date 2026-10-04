# Changelog

## 0.3.2

- Less work every frame keeping the server's settings in sync.

## 0.3.1

- Shorter store page; the store description no longer lists the removed shovel. Nothing changes in game.

## 0.3.0

- Groundbreaker no longer needs a stonecutter nearby, and `Paved Road Needs Stonecutter` no longer applies to it.
- New `Groundbreaker Needs Stonecutter` (off by default) brings the requirement back.

## 0.2.0

- The shovel is removed with its recipe, levels, Dig entry and settings; shovels made with 0.1.x disappear from
  inventories.
- EarthWright works on the hoe and cultivator only; custom entries take `tool: hoe` or `tool: cultivator`.
- Each entry's description lists every key that works with it; the hint line above the build bar and its settings
  are removed.

## 0.1.1

- New icons for every EarthWright menu entry and the shovel.

## 0.1.0

- First release, for the hoe, cultivator and a new shovel.
- Brush size, five shapes, rotation, edge hardness, grid mode and aim at the edge.
- Target heights: feet, aimed ground, locked, exact, continue the flat, floor height.
- Three level styles, exact raise and lower, smooth, paint, reset, a hard level key.
- Ramps (four profiles, quick ramp), curved roads, slope colours for carts.
- Undo and redo, area snapshots.
- New menu entries with toggles and key hints, custom YAML entries, the full build menu.
- Costs: stamina, wear, materials, stations, stone by volume, cooldown, free build, a live cost line.
- Height limits per biome, for the pickaxe too; admin limit, gentle slopes, strict dig mode.
- Protection: wards over the brush, no-build places, dungeons, terrain lock, tools switch, admin zones and entries,
  combat lock.
- Tools: reach, light, speed, a left-hand torch, levels up to 6.
- Road travel bonus, seed grid, cultivating forbidden ground, console command `ew`, translation files.
