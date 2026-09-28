# Changelog

## 0.3.0
- Groundbreaker no longer needs a stonecutter nearby. New setting "Groundbreaker Needs Stonecutter" in section 8
  (off by default) brings the requirement back; "Paved Road Needs Stonecutter" no longer applies to Groundbreaker.

## 0.2.0
- The shovel is gone: its item, recipe, upgrade levels, build menu and Dig entry, and its settings ("Shovel ..." in
  section 13, "Shovel Needs Stations" in section 8). Shovels made with 0.1.x disappear from inventories. EarthWright
  now works on the hoe and the cultivator only. Custom entries take `tool: hoe` or `tool: cultivator`.
- Keys are shown in one place: each entry's description now lists every key that works with it (size, value, shape,
  rotate, level style, lock height, target mode, hard level, paint, grid, aim at the edge, reset, reset around you,
  undo / redo; ramps and roads their own keys). The separate hint line above the build bar and its settings
  ("Show Controls Hint", "Hint Height") are removed.

## 0.1.1
- New icons for every EarthWright menu entry (Lower ground, Smooth, Paint, Reset, Ramp, Road, Groundbreaker,
  Clear objects, Terraform, Till, Uproot, Dig) and for the Shovel.

## 0.1.0
- First version. Brush size, shape (circle, square, rectangle, ring, frame), rotation, edge hardness, grid mode and
  aim at the edge for the hoe, the cultivator and a new shovel.
- Target heights: your feet, the aimed ground, a locked height, an exact height, continue the flat, a floor's height.
- Level in three styles (ease, step, instant plateau), exact raise and lower, smooth, paint, reset, hard level key.
- Ramps with four profiles, a quick ramp from your feet, curved roads through waypoints, slope colours for carts.
- Undo and redo, area snapshots.
- New menu entries: Lower ground, Smooth, Paint, Reset, Ramp, Road, Groundbreaker, Clear objects, Terraform (admin),
  Till, Uproot, Dig; entry toggles, key hints in descriptions, custom entries from YAML, the full build menu with
  search for the hoe and cultivator.
- Costs (stamina, wear, materials, stations, stone by volume, cooldown, free build) with a live cost line.
- Height limits for raising and digging, per biome, for the pickaxe too; admin limit; gentle slopes; strict dig mode.
- Protection: wards over the whole brush checked twice, no-build places, dungeons, terrain lock, tools switch, admin
  zones, combat lock, admin-only entries.
- Tools: reach, light, speed and a torch in the left hand while a terrain tool is out; tool levels up to 6.
- Road travel bonus, seed grid, cultivating forbidden ground.
- Console command `ew`, translation files.
