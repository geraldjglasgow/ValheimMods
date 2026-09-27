# EarthWright - PLAN

Terraforming for Valheim's hoe and cultivator: brush size, shapes, edges, exact target heights, level/raise/lower/
smooth/paint/reset, ramps and roads, undo, costs, protection, height limits, a shovel, and more - every feature of
`SPEC.md` (the user's list of 2026-09-27), configurable and working on a dedicated server. This document is the design
and the work split while the mod is unfinished. Written from the user's list and the game's own code only
(`assembly_valheim.dll` decompiled into the session scratch folder, the game's asset bundles read with UnityPy); no
other mod's source, DLL, config or documentation beyond the user's list is read.

> **Changed after 0.1.1 (user, 2026-09-27):** the shovel is removed (item, recipe, levels, the Dig entry and their
> settings); the controls-hint line above the build bar is removed and the entry descriptions list every key. Where
> this plan still mentions the shovel or `HintText`, that part no longer applies.

## How the game edits terrain (decompiled, game build of 2026-09)

- **Heightmaps**: 64 m zones, `m_width` 64, `m_scale` 1, so height vertices sit on whole world metres; 65 x 65 vertices,
  the edge row shared with the neighbour. The generated heights are `m_buildData.m_baseHeights`.
- **TerrainComp** (the "terrain compiler", one per edited heightmap, its own ZDO) holds the edits: per vertex
  `m_modifiedHeight`, `m_levelDelta`, `m_smoothDelta`; per paint cell (same 65 x 65 indexing, `index = y * 65 + x`)
  `m_modifiedPaint`, `m_paintMask` (r dirt, g cultivated, b paved, a vegetation). `Save()` compresses them into the ZDO
  (`s_TCData`), which the game replicates; every client's compiler reloads on a new data revision and pokes its
  heightmap. Shown height = generated + levelDelta + smoothDelta, **clamped to generated ± 8 m** in
  `ApplyToHeightmap` (runs on every client when a heightmap rebuilds).
- **The game's own ops**: the build piece is a prefab with a `TerrainOp`. Placing it runs `TerrainOp.Awake`, which
  calls `ApplyOperation` on the compiler of every heightmap in range (created if missing); that sends
  `RPC_ApplyOperation` to the compiler's owner **carrying only the prefab hash** - the owner applies its own copy of
  the prefab's settings. So a larger radius cannot travel through the game's RPC: EarthWright uses its own.
- Vanilla hoe entries (from the prefabs): `mud_road_v2` Level ground = smooth r3 power 1 toward the ghost height
  (smoothDelta clamped ±1 m in total, hence "eases at most about 1 m"), paint dirt r3; `raise_v2` raise r2 by 1 m,
  paint dirt r2.5; `path_v2` paint dirt r2; `paved_road_v2` smooth r3 + paint paved r2.2. Cultivator: `cultivate_v2`
  smooth r3 power 3 + paint cultivated r3; `replant_v2` paint "reset" (grass) r2.2.
- The level target: `UpdatePlacementGhost` puts a ground piece's ghost at the **player's feet height** unless
  AltPlace (Shift) is held, then at the aimed point.
- Vanilla camera zoom reads the scroll wheel in place mode when the piece cannot rotate - true for every terrain
  piece - so a modifier+scroll must hide the wheel from `GameCamera` (patch `ZInput.GetMouseScrollWheel`).
- The build menu (`BuildUi`) has a search field, recent and favourites, but the hoe and cultivator tables hide them
  (`PieceTable.m_hideAdvancedMenu`); EarthWright's "Full Build Menu" (local, on) shows them for terrain tools.
- Q is AutoRun and TabLeft, E is Use and TabRight, G the radial menu, F the guardian power, T emotes, R hide weapon,
  V auto pickup, C walk, X sit; Ctrl crouches, Shift runs and is AltPlace.

## Architecture: the edit pipeline

```
player's click / key / command
   |  EditFactory.Build (action + BrushState)           special entries: ISpecialAction.OnClick
   v
Dispatcher.Submit / PlacementHook ── EditEvents.Building ── EditGuards.CheckSender ── EditEvents.BeforeSend (undo snapshot)
   |                                                                          
   |-- normal edit ──> each touched TerrainComp's ZNetView.InvokeRPC("EW_TerrainEdit") ──> the compiler's owner
   '-- privileged ──> server "EW_RelayEdit": admin check ──> routed to each compiler's owner (sender = server)
                                                                              |
owner: OwnerHandler.Receive ── drop privileged flags unless relayed ── EditGuards.CheckOwner ── Engine.Apply
       ── TerrainComp.Save() (ZDO, replicated) ── Poke ── grass reset ── EditEvents.Applied
refusals travel back to the sender as "EW_EditRefused" and show as a centre message.
every client: Heightmap rebuild ── TerrainComp.ApplyToHeightmap (patched: EarthWright's height limits)
```

- **Two edit forms** (`Terrain/`): a parametric `BrushStroke` (evaluated by the owner against its current terrain, so
  raise and smooth stay right under lag) and an explicit `VertexSet` (targets computed by the sender: ramps, roads;
  raw restores: undo).
- **The owner decides, everyone draws.** State lives in the compiler's ZDO; only the owner writes it. Height limits
  are synced settings applied in the patched `ApplyToHeightmap` on every client, so everyone sees the same ground.
- **EarthWright is required on the server and every client** (decision): owners must understand EarthWright edits and
  every client must render past-limit ground the same way. The Charter join check refuses mismatched installs.
- The game's own TerrainOps that are not this player's hoe click (pickaxe digging, other mods) still go through the
  game; their clamps use EarthWright's limits (patched `LevelTerrain`/`RaiseTerrain`/`SmoothTerrain`).

## Modules and who builds them

Each module is a folder under `EarthWright/src/` with a `<Name>Module.Initialize(SyncedConfiguration)`, called from
`Plugin.InitializeModules`. Core, Terrain (pipeline) and Actions are the scaffold, written first; the rest is built in
parallel by one agent each. An agent owns its folder and may change only its own files.

| Module | Owner | Scope |
| --- | --- | --- |
| Core, Terrain (pipeline), Actions | main session | scaffold, interfaces, integration |
| Terrain/Engine* , Terrain/Limits* , `EngineSettings` | Engine agent | the math, height limits, gentle slopes, estimates |
| Brush | Brush agent | brush values and keys, scroll capture, zoom block, target height modes, ghost, repeat, gamepad |
| Preview | Preview agent | outline, grid, 3D volume, HUD text, hints, locked badge, world grid overlay, piece highlight, dust, panel |
| Paths | Paths agent | ramp entry (3 clicks, profiles), quick ramp, road entry (waypoints), slope colours, HUD |
| History | History agent | undo/redo, grouping, commands, snapshots; `Core/Language.cs` translation files |
| Costs | Costs agent | stamina, durability, resources, stations, volume costs, radius factors, cooldown, free build, cost HUD |
| Clearing | Clearing agent | reset keys and commands, clear objects entry, survival clearing, Groundbreaker, admin `ew terrain` |
| Protection | Protection agent | wards over the brush, no-build, dungeons, admin zones, terrain lock, admin entries, combat lock, limit override |
| Menu | Menu agent | EarthWright's own entries (cloned prefabs, icons), entry toggles, descriptions with hotkeys, custom YAML entries |
| Gear, Extras | Gear agent | shovel item, tool levels, reach, light, speed, torch; road travel bonus; cultivator extras |

### Shared interfaces (scaffold, do not rename)

- `Terrain`: `TerrainEdit` (`ForStroke`, `ForVertices`, `Flags`, `Source`), `BrushStroke`, `VertexSet`
  (`TargetVertex`, `RawVertex`), `Dispatcher.Submit(edit)` / `SendChecked` / `Compilers(edit)`,
  `EditGuards.AddSender/AddOwner`, `EditEvents.Building/BeforeSend/Sent/Applied`, `Engine.Apply/Estimate`
  (`EditResult`, `EditEstimate`, `VertexChange`), `TerrainRead.TryVertex/GroundHeight/BaseHeight`, `Refusals.Send`.
- `Actions`: `ToolAction`, `ActionCatalog.Register/For/ById/Current/All`, `SpecialActions.Register(key, ISpecialAction)`,
  `EditFactory.Build(action)`, `PlacementHook.SkipPlacedEffect`.
- `Brush`: `BrushState` (read by all, written by Brush), `BrushCaps.LevelMaxRadius/LevelUnlocks` (set by Gear).
- `Core`: `Sections`, `GeneralSettings.Active`, `Language.Add/Localize`, `Messages`, `Keys.Pressed/Held`,
  `Command.Add(name, usage, handler, adminOnly)`, `HudText.Set(key, text, order)`, `HintText.Set`,
  `PreviewStatus.Report(key, reason)`, `PanelSections.Add`, `GameReady.OnObjectDb/OnScene`, `LocalTool`, `Side`,
  `Ticker.OnUpdate/OnGui`, `Safe`.
- Cross-module: `Costs.CostApi.TryCharge/CannotPay/CooldownRemaining`, `History.UndoHooks.AddFirst`,
  `Menu.EntryRegistry.Prefab`, `Menu.MenuHooks`, `Protection.ObjectRules.Refusal` (object removal),
  `Brush.BrushCaps.EntryRefusal`, `ToolAction.IsPathTool`, `Dispatcher.Check`, `Keys.PanelTyping`.
- Integration decisions and the code map are in `CLAUDE.md`.

## Feature map (SPEC section -> behaviour -> module)

Judgement calls are marked **Decision**. Every setting that changes the world is synced and lockable; display-only
preferences (colours, HUD, keys, dust) are local.

### 1. The tool
- Features live on the vanilla hoe and cultivator, plus a craftable **Shovel** (Gear). **Decision:** no second hoe
  item; the shovel is the separate tool and gets the "separate tool" settings: Workbench recipe (configurable),
  repairable, max durability, wear per use.
- **Tool levels** (Gear): max quality of hoe, cultivator and shovel configurable (default 6); upgrade cost per level
  configurable; optional per-level maximum radius and per-level unlocks (off by default) through `BrushCaps`.
- **Light, speed, torch, reach** while a terrain tool is held (Gear): light range/brightness synced, colour local;
  movement multiplier; keep a torch in the left hand; reach default 20 m (5-50), vanilla when put away.
- **Entry toggles, descriptions listing the configured keys, custom YAML entries** (Menu). The search field is the
  game's own, hidden for the hoe by its table flag; the Menu module turns the full build menu on for terrain tools.

### 2. Brush size
- Brush agent: modifier + scroll (default LeftAlt, configurable), `[`/`]` keys, a **value selector** key (B) that
  picks which value modifier+scroll and `[`/`]` change (radius, amount/strength, hardness, rotation, depth/inner,
  target height) - the SmarterHoe-style "pick then scroll" without taking Q/E, which the game uses. Optional
  "plain scroll adjusts while a terrain entry is selected". Step size (negative reverses), Ctrl for x5 steps.
- Radius min/max (defaults 0.5 and 20, settable 0.5-100), per-entry YAML min/max/default (`EarthWright.Brushes.yml`),
  separate hoe/cultivator/shovel defaults, remembered per entry; a global size multiplier with per-kind switches
  (level, raise, smooth, paint); skill-based cap (off by default); level cap from Gear; modded terrain pieces resized
  (switch); paint follows the radius (paint ratio per entry). Radius shown in the HUD, optionally as a message.
- **Decision:** OHN0's "wider levelling by repetition" is covered by one large stroke; not built.

### 3. Shape and edge
- Shapes circle, square, rectangle, ring, frame (key N cycles; Engine evaluates in world metres, so squares have no
  diamond distortion). Rotation: arrow keys 22.5°, Home resets north, or the selector. Grid mode (key I): centre and
  size snap to whole metres, rotation 0, hard edge, works below ground and sea. Hold-to-snap key. Aim at the edge
  (key O). Edge hardness 0-100 % (selector; the HUD also shows the feather width in metres); soft square edges come
  from hardness < 100 %. **Decision:** no separate "square" menu entries - the shape key covers them.

### 4. Target height
- Brush agent: modes Feet (default, as the game's level ground), Aimed (hold Shift as in the game, or mode), Locked
  (K locks the current target; PageUp/PageDown ±0.1 m, hold to repeat, Shift x5), Exact (the selector's target
  height, 0.25 m steps, Ctrl 2 m), Continued ("continue the flat", tolerance setting, falls back to the crosshair),
  Floor (middle mouse copies the aimed floor piece's height). End returns to Feet. Mode cycle key Y. The HUD shows
  the target and its source. Not kept across logout.

### 5. Operations
- Engine: Level in three styles - Ease (vanilla-like, max step per click, default 1 m), Step (max step 1-1000 m),
  Instant (plateau) - style key L, default style setting also applying to the game's own Level ground; Raise/Lower
  by an exact amount (0.05-8 m default range, settable), repeated raises add up to the raise limit; Smooth (soften:
  toward the neighbour average, no flattening); Reset (generated height and paint); SetMin/SetMax/Offset/
  RemoveSurface (admin); paint ops; filters (height band, random share, skip under buildings).
- Brush agent: **no silent blocks** - when the crosshair hits a rock or cliff collider instead of terrain, the ghost is
  put on the terrain beneath it instead of the game hiding it.
- Hard-level hotkey F9 (Brush): an Instant, hard-edged level with the current target while Level or Raise is selected.
- **Decision:** TerraHoe's F8 "full terraforming" is the Instant style plus the height limits; not a separate mode.
- Menu entries (Menu agent), hoe: Lower, Smooth, Paint (paint only; the paint is picked with P), Reset, Ramp, Road,
  Clear, Groundbreaker (clear + level + pave in one swing), Terraform (admin). Cultivator: Till (cultivate, no height
  change), Uproot. Shovel: Dig (lower) plus a configurable list. "Flat and smooth versions" = the style key.
- Paint (P cycles): entry's own, Dirt, Paved, Cultivated, Grass, Original (biome default, paint-only reset),
  Vegetation (grass density brush, density on the selector), Clear vegetation, Keep (height only).
  **Decision:** "other biomes' ground types" and "clutter kinds" cannot be painted - the paint mask has only dirt,
  cultivated, paved and vegetation channels, and clutter kind follows the biome; the vegetation density brush and its
  eraser cover what the mask can do.
- Paved road without levelling (Menu setting on the game's entry), without a stonecutter (Costs), stone per square
  metre paved (Costs), road travel bonus dirt 10 % / paved 20 % (Extras).
- **Decision:** Voxheim's voxel caves are not built: Valheim terrain is a heightmap; overhangs need a separate mesh
  system.

### 5b. Ramps and roads (Paths agent)
- Ramp entry: click start, click end, move sideways (or the selector/`[`/`]`) for the width, click to build. Profiles
  (N while the ramp is selected): straight, straight with soft joins, soft ends, S-curve. Ctrl widens one side only,
  Alt blends the ends. Shoulder blending, paint by P. Preview line and footprint, red when too steep/too big/past the
  height limit. HUD: length, rise, width, points changed, slope. Caps: 1024 points (setting), 75° (setting), width
  1-20 m, max length. Backspace or the undo key removes the last point first.
- Quick ramp (J): from your feet to the aimed point with the current width (the Flattenheim two-point ramp).
- Road entry: click places waypoints (curve through them), H carves, Shift+H carves paved, `[`/`]` width 1-20 m,
  P paint, Backspace removes the last waypoint, segments coloured by slope for carts (≤20° green, 20-25° yellow,
  >25° red; thresholds configurable).

### 5c. Reset and clearing (Clearing agent)
- Reset entry (brush), U resets the brush area, Shift+U resets around you (radius setting), `ew reset [radius]`.
  Reset never touches buildings, trees or rocks; skips ground under buildings (setting, on) and warded ground.
  **Decision:** the U key and the command are free - VentureReset's "charges the selected piece" pitfall is avoided.
- Clear entry (off by default): trees, stumps, logs, shrubs, rocks, pickables in the brush; wards respected; nothing
  drops, or survival mode (needs an axe/pickaxe of sufficient tier, drops wood and stone). Clearing radius separate
  (setting) or the brush. Groundbreaker entry = clear + level + pave.
- Commands: `ew reset`, `ew forestry`, `ew debris`, `ew pieces` (admin), `ew terrain <op> ...` (admin: level, raise,
  lower, min, max, band, offset, slope, remove; filters random share, skip under buildings, include/ignore prefabs).

### 6. Height limits (Engine agent; lock and exceptions: Protection)
- Separate raise and dig limits (default 8/8 = vanilla, settable 0.5-512), per-biome overrides (YAML), applied in
  the patched `ApplyToHeightmap` and in the game's own clamps. The hidden ±8 lifetime cap (`m_levelDelta` clamp) goes.
- Strict mode exceptions: dig limit lifted near listed prefabs (ore, buried treasure) and when standing in tar
  (Protection sets the edit's limit override); admins may override with a key (privileged edit, IgnoreLimits).
- Gentle slopes (off by default): after a height edit, neighbours within a radius are relaxed to a max slope.
- Terrain lock (Protection): all terrain editing off, admins bypass, exempt tools list; applies to pickaxe digging too.
- README warns: keep the mod installed on every machine; ground edited past ±8 m shows wrongly without it.

### 7. Undo (History agent)
- 1-50 steps (default 15), Ctrl+Z / Ctrl+Y, `ew undo` / `ew redo`; a held stroke is one step; covers terrain, paint,
  ramps, roads, resets and clearing's terrain part; costs are not refunded; clears on logout. Ramp/road points first
  (`UndoHooks`). Named in-memory area snapshots: `ew snapshot save|restore|list <name> [radius]`.

### 8. Costs (Costs agent)
- Stamina: vanilla / off / fixed / scaled by radius; durability on/off and factor; resources on/off; an extra item per
  swing; station requirement per tool (game's own: workbench for raise, stonecutter for paving) on/off; stone per m³
  raised (lowering free) and per m² paved; radius factors for materials, stamina, durability; per-entry YAML
  overrides (`EarthWright.Costs.yml`); cooldown; free-build key (F7) allowed by the server; "no discount" (reach never
  lowers stamina); stamina skill (a chosen skill lowers stamina). Live cost line with have/need, preview red when
  short. `CostApi` charges special entries.

### 9. Preview and HUD (Preview agent)
- Outline of the footprint following the ground (circle/square/rect/ring/frame), the game's ghost ring resized or
  hidden, grid of the vertices and paint cells that will change (from `Engine.Estimate`), 3D volume (cylinder/box)
  between ground and target, colours and transparency settings, tile coordinates and height readout, the HUD block
  (`HudText`), the controls hint above the hotbar (`HintText`), a "settings locked by server" badge, a world grid on
  the ground (F8; locked to the world or the aimed piece; spacing, colours), highlight pieces inside the footprint,
  dust effect removal (local), the panel (F6) with typed values, presets (1x2, 5x2, 5x3, 8x3) and module sections,
  also reachable from the Esc menu.
- Camera zoom block modes (while adjusting / always with a terrain entry / off): Brush agent (scroll capture).

### 10. Controls
- Every key a BepInEx `KeyboardShortcut`; optional gamepad buttons (Brush); hold-to-repeat with start delay and rate,
  keeps applying while the mouse is held (Brush); `ew on` / `ew off` (Core; also from chat as `/ew on` if the game's
  chat runs console commands).

### 11. Protection (Protection agent)
- Ward check over the whole footprint on the sender and again on the owner (from ward ZDOs, so it works on any
  machine); no-build locations; refuses in dungeons (interiors); admin zones (`ew zone add <name> <radius> [player]`,
  `ew zone list`, `ew zone remove <name>`; 5-200 m, at most 100, stored on the server, pushed to clients, checked on
  sender and owner; mode: off / only inside zones / never inside zones); admin-only entries; server switch allowing
  terrain tools; combat lock (**Decision:** "fully seen" = a hostile creature within range is alerted and targeting
  you; off by default); admin limit-override key; server-locked settings with admins exempt (Charter already does it).

### 12. Multiplayer and config
- Required everywhere (**Decision**, above); Charter checks versions and syncs; the .cfg and YAML hot reload.
- Languages: English built in; `EarthWright.Language.<Language>.yml` files next to the cfg override it, picked by the
  game's language; `ew language write` dumps the English template for translators (History agent).

### 13. Cultivator extras (Gear agent, Extras folder)
- Till entry (Menu) and square cultivate (shape key); seeds and saplings snap to a grid (spacing setting, toggle key);
  cultivate ground the game forbids (switch); Uproot entry removes natural pickables in the brush (special "uproot").

### 14. Console commands
- One root: `ew` (alias `earthwright`) with subcommands; `ew help` lists them. **Decision:** own names instead of
  other mods' command names, so EarthWright can sit next to them.

### Out of scope
- Related-but-outside items (totem, paving cart, pickaxe shapes, pickaxe flatten) and Voxheim's voxel terrain.

## Default controls (while a terrain tool is out and its menu closed)

| Action | Default |
| --- | --- |
| Change the selected brush value | LeftAlt + wheel, or `]` / `[` (hold Ctrl: x5) |
| Select the next brush value | B |
| Rotate the footprint / reset to north | Right / Left arrow (22.5°), Home |
| Cycle shape (or ramp profile while the ramp is selected) | N |
| Snap to the grid while held | Z |
| Cycle level style | L |
| Lock the target height / back to feet | K / End |
| Target height ±0.1 m | PageUp / PageDown (hold Shift: x5) |
| Cycle target mode | Y |
| Copy the aimed floor's height | Middle mouse |
| Grid mode / aim at edge | I / O |
| Cycle paint | P |
| Hard level | F9 |
| Reset the brush area / around you | U / Shift+U |
| Undo / redo | Ctrl+Z / Ctrl+Y |
| Remove the last ramp or road point | Backspace |
| Quick ramp from your feet | J |
| Carve road / carve paved | H / Shift+H |
| Free build | F7 |
| Admin: ignore the height limits while held | Right Alt |
| Seed grid (a seed selected) | I |
| World grid | F8 |
| Panel | F6 |

## Config sections

`0. General` (Core), `1. Brush` and `2. Target Height` and `10. Controls` (Brush), `3. Operations` and
`6. Height Limits` (Engine), `4. Ramps and Roads` (Paths), `5. Reset and Clearing` (Clearing), `7. Undo` (History),
`8. Costs` (Costs), `9. Preview and HUD` (Preview), `11. Protection` (Protection), `12. Menu` (Menu),
`13. Tools` (Gear), `14. Cultivator` and `15. Road Travel` (Gear agent, Extras folder). Key settings of a module live
in its own section. YAML files: `EarthWright.Brushes.yml` (Brush), `EarthWright.Costs.yml` (Costs),
`EarthWright.Entries.yml` (Menu), `EarthWright.Limits.yml` (Engine), `EarthWright.Zones.yml` (Protection, server data).

## Working rules (every module)

- Methods at most 24 lines, classes at most 300 lines, one responsibility each; split by feature.
- Multiplayer first: terrain changes only through `Dispatcher`; object changes by the ZDO owner (claim ownership or
  use the object's own RPC); everything visible to one player that affects others is visible to everyone.
- Settings through `Plugin.Synced` / the `SyncedConfiguration` passed to Initialize: gameplay synced (default),
  personal display and keys `synced: false`. Words with `Language.Add("ew_<module>_...", "English")`.
- Never read another mod's source, DLL, config or README. The game's decompiled source is in the session scratch
  folder; never decompile into the repository.
- Comments: a summary on every class saying what it is and why; comments explain intent, not the obvious.

## Status and test checklist

All modules built, merged, integration-reviewed by their agents and built together on 2026-09-27; every Harmony patch
target and parameter was checked offline against the game's assemblies. Nothing has run in game yet. The in-game
test checklist is in `CLAUDE.md`; every item is tested in single player and on a dedicated server with two clients.
