# CLAUDE.md - EarthWright

Terraforming for Valheim's hoe and cultivator: brush size, shapes and edges, exact target heights,
level/raise/lower/smooth/paint/reset, ramps and curved roads, undo, costs, height limits, protection, new menu
entries. The log line `Loading [EarthWright 0.3.0]` confirms the version. Built on 2026-09-27 from the user's
feature list (`SPEC.md`, gitignored) and the game's own code, by a main session and ten module agents following
`PLAN.md`, which holds the design, the feature map and the judgement calls. This file is the code map, the patched
methods, the network names and the in-game test checklist.

Rules that apply to every change:

- No other mod's source, DLL, config or documentation is read (the user asked for this when commissioning the mod).
  Only this workspace, the shared libraries and the game's own assemblies, decompiled into the scratchpad.
- Functions at most 24 lines from brace to brace, classes at most 300 lines, one responsibility per class. Prefix,
  postfix and finalizer patches only; every patch class is applied on its own in `Plugin.PatchEverything`.
- Multiplayer first: terrain changes only through `Terrain.Dispatcher` (the owner of each terrain compiler applies
  them); world objects are changed by their ZDO owner; settings that change the world are synced and lockable,
  keys and display options are `synced: false`.
- A module never reads another module's settings through the `ConfigFile`; cross-module hooks are wired in
  `Plugin.WireModules` or through the small hook classes listed under "Seams".
- Hooks and per-frame callbacks run through `Core.Safe` (logs, never rethrows); PatchGuard's `Guard.Run` rethrows and
  is not used for callbacks the game calls.

## The edit pipeline (see PLAN.md for the diagram)

1. A click: `Actions/PlacementHook` (prefix on `Player.TryPlacePiece`) builds the edit with `EditFactory.Build`
   from the entry's `ToolAction` and `BrushState`, runs `EditEvents.Building` (flags) and the sender guards, and
   parks it; the game charges and instantiates the entry's TerrainOp; the `TerrainOp.Awake` prefix sends the parked
   edit with `Dispatcher.SendChecked` instead of the game's RPC. Special entries (ramp, road, clear, groundbreaker,
   uproot, custom) run their `ISpecialAction` instead and charge through `Costs.CostApi`.
2. Keys and commands build edits and call `Dispatcher.Submit` (or `Check` → charge → `SendChecked`).
3. `Dispatcher` sends each touched terrain compiler the edit over its own ZNetView (`EW_TerrainEdit`); a privileged
   edit goes to the server first (`EW_RelayEdit`), which checks the sender is an admin and forwards it.
4. `Terrain/OwnerHandler` on the compiler's owner drops privileged flags not relayed by the server, runs the owner
   guards, applies through `Engine.Apply`, saves the compiler to its ZDO and pokes the heightmap. Refusals go back as
   `EW_EditRefused`.
5. Every client draws the ZDO's values through the patched `TerrainComp.ApplyToHeightmap` (clamp ±Absolute limit).

## Code map (`EarthWright/src/`)

| Folder | What it holds |
| --- | --- |
| `Core/` | settings section 0, `Command` (`ew`), `Keys`, `Language` + `LanguageFiles` + `WordList` (translations), `Messages`, `HudText`, `PreviewStatus`, `PanelSections`, `GameReady`, `LocalTool`, `Side`, `Ticker`, `Safe` |
| `Terrain/` | edit model (`TerrainEdit`, `BrushStroke`, `VertexSet`, wire), `Dispatcher`, `OwnerHandler`, `ServerRelay`, `Refusals`, `EditGuards`/`EditEvents`, `TerrainRead`; the engine (`Engine*`, `Math/Footprint`, `HeightView`, `ChangeBuffer`, `PaintOps`, `SlopeRelax`), limits (`HeightLimits`, `Limit*`, `EngineBaseHeights`), `ew limits` |
| `Actions/` | `ToolAction`, `ActionCatalog`, `VanillaActions` (the game's six entries), `SpecialActions`, `EditFactory`, `PlacementHook` |
| `Brush/` | `BrushState` (read by all), values and memory per entry, keys, wheel capture, target height modes, ghost placement ("no silent blocks"), repeat, hard level, game key guards, HUD lines, `BrushCaps` |
| `Preview/` | outline, changed points, volume, ghost ring, piece highlight, world grid, HUD overlay, cursor readout, dust, the F6 panel and Esc button |
| `Paths/` | ramp and road tools: geometry (pure), planning, commit, preview, HUD |
| `History/` | undo/redo recorder and restorer, pruning, snapshots, `ew undo/redo/history/snapshot/language` |
| `Costs/` | stamina, wear, materials, stations, volume stone, cooldown, free build, cost line, `CostApi` |
| `Clearing/` | reset keys, clear and groundbreaker entries, survival clearing, `ew reset/forestry/debris/pieces/terrain` |
| `Protection/` | guards (wards, no-build, dungeons, tools switch, lock, admin entries, zones, combat), `ObjectRules`, zones (file, RPC, commands, panel), dig exceptions, admin routing |
| `Menu/` | EarthWright's entries (cloned prefabs, icons), toggles, table layout, full build menu, descriptions with key hints, custom YAML entries, `EntryRegistry` |
| `Gear/` | tool levels and level gate, reach, light, speed, torch |
| `Extras/` | road travel bonus, seed grid, cultivate anywhere, uproot |

Seams between modules: `BrushCaps` (level radius, unlocks, entry refusal), `UndoHooks.AddFirst`, `MenuHooks`,
`Keys.PanelTyping`, `PlacementHook.SkipPlacedEffect`, `HeightLimits.AddDigException`, `ObjectRules.Refusal`,
`CostApi`, `EntryRegistry.Prefab`, `ToolAction.IsPathTool`.

## Network and data names

- RPCs: `EW_TerrainEdit` (compiler ZNetView), `EW_RelayEdit`, `EW_EditRefused`, `EW_ZoneRequest`, `EW_ZoneReply`
  (routed). Wire version `EditWire.Version` = 1.
- Charter: GUID `milkyteam.earthwright`, cfg `milkyteam.earthwright.cfg`, standing article `ew.zones`.
- YAML sets (pattern, sync key): `EarthWright.Brushes*.yml` `earthwright_brushes`, `EarthWright.Costs*.yml`
  `earthwright_costs`, `EarthWright.Entries*.yml` `earthwright_entries`, `EarthWright.Limits*.yml`
  `earthwright_limits`, `EarthWright.Zones.yml` `ew.zones.file` (server data, written back by the server).
- Prefabs: menu pieces `ew_*`, custom entries `ew_custom_<id>` (no items of its own). Words `ew_*`.
- No ZDO keys of its own: terrain lives in the game's compiler ZDO (`s_TCData`).

## Patched game methods

| Module | Method | Kind |
| --- | --- | --- |
| Actions | `Player.TryPlacePiece` | prefix + postfix |
| Actions | `TerrainOp.Awake` | prefix (High) |
| Brush | `Player.UpdatePlacementGhost` | postfix |
| Brush | `ZInput.GetMouseScrollWheel` | postfix |
| Brush | `GameCamera.UpdateCamera` | prefix + postfix (gamepad zoom) |
| Brush | `Player.UpdatePlacement` | prefix (floor key never removes) |
| Brush | `KeyHints.Update` | prefix (F9 does not switch the gamepad layout) |
| Brush | `Player.Update` | prefix + finalizer (debug-mode Z/B/K/L quiet on brush keys) |
| Core | `ObjectDB.Awake`, `ObjectDB.CopyOtherDB`, `ZNetScene.Awake`, `Terminal.InitTerminal`, `Localization.SetupLanguage` | postfix |
| Costs | `Player.UpdatePlacement`, `Player.TryPlacePiece` | scope prefix + finalizer (+ postfix) |
| Costs | `Player.HaveStamina`, `Player.HaveRequirements`, `Player.ConsumeResources` | prefix |
| Costs | `Player.GetBuildStamina`, `Player.GetPlaceDurability` | postfix |
| Extras | `Player.GetRunSpeedFactor`, `Player.UpdatePlacementGhost` | postfix |
| Extras | `Player.CheckRun` | prefix + finalizer; `Player.UseStamina` prefix |
| Gear | `Humanoid.EquipItem` | prefix + postfix (torch) |
| Gear | `Player.GetJogSpeedFactor`, `Player.GetRunSpeedFactor`, `Recipe.GetRequiredStationLevel`, `VisEquipment.SetRightHandEquipped` | postfix |
| History | `Game.Start`, `Game.OnDestroy` | postfix |
| Menu | `Player.AddKnownPiece` | prefix (silent unlock) |
| Preview | `Player.PlacePiece`, `Player.UpdatePlacement` | prefix + finalizer (dust) |
| Preview | `TerrainOp.Awake` | prefix (First, dust) |
| Preview | `Player.TakeInput`, `PlayerController.InInventoryEtc`, `PlayerController.TakeInput`, `GameCamera.UpdateMouseCapture`, `ZInput.GetMouseScrollWheel` | postfix (panel) |
| Preview | `Menu.Start` postfix, `Menu.Update` prefix | Esc button, Esc closes the panel |
| Protection | `Player.TryPlacePiece` | prefix (First: click frame; Low: game terrain pieces under lock) |
| Protection | `Attack.SpawnOnHitTerrain` | prefix (pickaxe under lock) |
| Protection | `Game.Start` | postfix (zone RPCs, host zone reload) |
| Terrain | `TerrainComp.Awake` postfix, `Game.Start` postfix | RPC registration |
| Terrain | `TerrainComp.ApplyToHeightmap`, `TerrainComp.LevelTerrain`, `TerrainComp.RaiseTerrain`, `Heightmap.AtMaxWorldLevelDepth` | prefix replacements (limits) |

All 53 patch classes were checked offline on 2026-09-27 against the game's assemblies (targets resolve, parameter
names and types match): scratch harness `patchcheck`, reflection only.

## Decisions made during integration (beyond PLAN.md)

- The shovel was removed on 2026-09-27 at the user's request (item, recipe, levels, Dig entry, its settings).
- Keys are listed in one place only: the selected entry's description (Menu `KeyHints`, every key the brush honours for
  that entry). The separate controls-hint line above the build bar was removed at the user's request.

- The hoe and cultivator tables hide the game's search/recent/favourites (`m_hideAdvancedMenu`); "Full Build Menu"
  (local, on) turns it on for terrain tools.
- The ramp profile cycles on the Brush shape key (N); the snap-while-held key is Z (not while Ctrl is down); the
  admin limit override is Right Alt; the seed grid key is I (only with a seed selected).
- Ramps and roads are the only "path tools" (`ToolAction.IsPathTool`): points, a width from the brush radius, no
  shape or style keys. Every other special entry (clear, uproot, groundbreaker, custom) uses the whole brush.
- Tool levels gate entries, shapes and styles: one sender guard (Gear `LevelGate`), the click refusal
  (`BrushCaps.EntryRefusal` in `PlacementHook`), and the Brush key cycles skip locked choices.
- A reset (or any edit) touching a ward the player has no access to is refused as a whole, not trimmed.
- Resets (keys, `ew reset`) and restores are free and never held back by the cooldown; reset strokes and privileged
  edits pay no volume stone.
- Custom entries that run `ew forestry` / `ew debris` charge the entry and then the command (known, documented).
- Undo records a generous square and prunes unchanged values once the edit is applied (at once on owned ground,
  after 3 s otherwise), so it never reverts neighbours' later edits outside that window.

## In-game test checklist

Single player first, then a dedicated server with an admin (A) and a player (B). Nothing below has been run in game.

1. Load: `Loading [EarthWright 0.3.0]`, no failed patches, no exceptions; `ew help` lists the subcommands.
2. Hoe menu: the game's four entries, then Lower, Smooth, Paint, Reset, Ramp, Road, Groundbreaker (Clear only with
   Clearing Enabled; Terraform only for admins); icons; search finds "lower"; cultivator shows Till and Uproot.
3. Brush: Alt+wheel and `[`/`]` resize without zooming; B cycles values; N shapes; arrows rotate; I grid; O edge;
   L style; P paint; HUD and hint show; outline, changed points and volume follow the ground.
4. Target: feet by default, Shift aims, K lock, PageUp/PageDown, End, Y continue-flat, middle mouse floor height.
5. Level Ease/Step/Instant, F9 plateau (layout unchanged), raise/lower amounts, smooth, paint each paint, reset
   (entry, U, Shift+U, `ew reset`), across a 64 m zone border with no seam.
6. Clicking a rock next to flat ground levels the ground behind it; hold-to-repeat applies and charges per repeat.
7. Undo/redo (Ctrl+Z/Y while walking), a dragged stroke is one step, ramp points first, snapshots.
8. Ramp (3 clicks, N profiles, Ctrl one side, Alt blend ends), J quick ramp, road (waypoints, H, Shift+H), colours.
9. Costs at defaults equal the game's; each cost setting; free build F7 for admins; cooldown without message spam.
10. Limits: raise stops at the Raise Limit; Dig Limit 12 lets the pickaxe dig to 12 m; per-biome YAML; `ew limits`;
    admin Right Alt past the limits; gentle slopes; strict dig exceptions near copper and in tar.
11. Protection: wards over the brush edge (sender and owner), no-build, dungeon, tools switch, lock (pickaxe too),
    zones via `ew zone`, combat lock, admin-only Terraform.
12. Tools: hoe and cultivator upgrade to 6; reach 20 m; tool light seen by the other player;
    torch in the left hand; faster running; road sprint bonus; seed grid; cultivate anywhere; uproot keeps crops.
13. Panel F6 and the Esc button: typed values stick, presets, sections; Esc closes it; no hotkeys while typing.
14. Dedicated server: B's edits on ground A owns are applied by A and seen by both; a privileged edit goes through
    the server; the server log stays clean.
