# Stations

Crafting stations (workbench, stonecutter, artisan table, galdr table, prep table), forges, cauldrons, the extensions
placed beside them, production stations (smelter, blast furnace, charcoal and frost kilns, eitr refinery, fermenter,
windmill, spinning wheel, beehive, bird nest, sap collector, cooking stations, oven, Frost Foundry, obliterator) and
the other working pieces (portals, the ward, cartography table, wisp lure, eternal pyre, barber, shield generator).
Stations are building pieces and follow `pieces.md` for anatomy, the Custom/Piece shader, damage and placement; this
page gives their budgets, parts, paint and the script objects that make them come alive.

Measured on 2026-09-29 from the hammer's piece table by `codex/measure/pieces.py` (`codex/data/pieces.json`,
categories `station.*`, 59 pieces). Looks read from `codex/out/pieces/renders/station.*.png`,
`renders/states_5.png`, `renders/close_piece_workbench.png`, `textures/workbench.png`, `textures/forge.png`,
`textures/smelter.png` and `icons/station.*.png`. Reference prefabs are `GameElements/Pieces/<name>.prefab`.

## The numbers

| Category | n | Triangles median (range) | Texture px | px per metre median (IQR) | Longest m median | References |
| --- | --- | --- | --- | --- | --- | --- |
| `station.workbench` | 5 | 2,512 (1,252 to 7,589) | 256 (128 to 256) | 39.5 (36.2 to 43.4) | 3.05 | piece_workbench, piece_stonecutter, piece_artisanstation, piece_magetable, piece_preptable |
| `station.forge` | 2 | 1,748 and 4,203 | 256 and 128 | 50.3 and 36.4 | 1.87 and 3.66 | forge, blackforge |
| `station.cauldron` | 2 | 2,517 and 1,148 | 256 and 128 | 79.8 and 73.0 | 2.46 and 1.34 | piece_cauldron, piece_MeadCauldron |
| `station.extension` | 26 | 1,574 (292 to 6,751) | 128 (64 to 256) | 49.2 (41.4 to 63.1) | 1.53 | piece_workbench_ext1, piece_workbench_ext2, piece_workbench_ext3, forge_ext1, forge_ext2, cauldron_ext3_butchertable |
| `station.production` | 16 | 3,277 (384 to 13,812) | 256 (128 to 512) | 38.9 (31.4 to 53.3) | 4.49 | smelter, charcoal_kiln, fermenter, piece_spinningwheel, windmill, piece_cookingstation |
| `station.other` | 8 | 2,590 (408 to 4,452) | 256 (128 to 256) | 28.9 (23.8 to 46.5) | 4.43 | portal_wood, guard_stone, piece_cartographytable, piece_wisplure |

- **The first tier is cheap.** The workbench is 1,252 triangles on one 256 px texture, the forge 1,748, the smelter
  609, the charcoal kiln 384, the fermenter 662, the wooden portal 408. Later stations climb (artisan table 7,589, blast
  furnace 13,812, Frost Foundry 11,360 on the only 512 px texture), but a new early or mid station belongs at 1,000 to
  2,500 triangles on 256 px.
- **One unique atlas each** (`WorkBench_d.png`, `Forge_d.png`, `smelter.png`, `fermenter_d.png` ...), 256 px, with a
  metal mask where there is iron (`Forge_m.png`). Glowing parts are a second material (see Glow).
- **Sizes:** crafting tables 2.5 to 3.6 m long and 1.2 to 2.7 m tall (workbench 3.05 x 1.85 x 1.33 m, stonecutter
  3.28 x 2.08 x 1.63 m); the forge 1.87 x 1.25 x 1.17 m; smelter 3.0 x 4.2 x 2.6 m with its chimney; charcoal kiln
  4.7 x 3.7 x 4.9 m; fermenter 1.7 x 2.2 x 2.0 m; windmill 5.3 x 8.4 x 5.8 m; extensions 0.9 to 2.5 m.
- **No snap points** (except the oven's 8 and the Frost Foundry's 4); pivot on the ground at the centre.
- **Health 200** for crafting tables and forges, 50 to 1,000 for extensions, up to 2,000 for furnaces.

## Anatomy of a crafting station

`piece_workbench` and `forge` show the full set of children:

```
forge                           Piece, ZNetView, CraftingStation, WearNTear
  floor_2x2_snow (off)          snow cap scaled to the top
  Collider                      leaky box on piece: 1.8 x 0.91 x 1.0 m, centre 0.49 m up
  connectionEffectPoint         where extension beams end (0.42, 0.99, 0.03)
  roof_check_piont              the point tested for a roof (1.4 m up; workbench 1.51 m)
  PlayerBase                    capsule + EffectArea on character_trigger: the base area
  _enabled (off)                CraftingStation.m_inUseObject: shown while the station is used
    SmokeSpawner, Point light, FireWarmth, flames, smoke, SFX, flare, sparcs (1)
  AreaMarker                    CircleProjector: the build-range ring, radius = m_rangeBuild
    Particle System             2.55 m up
  New                           LODGroup: High (1,748 triangles, Forge_mat + ForgeCoals_mat), Low (406)
  Worn (off), Broken (off)      Worn: ForgeWorn material; Broken: a broken mesh in it
  ForgeDestruction (off)        28 pre-cut chunks, the fragment roots
```

- **`CraftingStation`** fields that decide what the player sees: `m_rangeBuild` 20 m (the artisan table 40) and
  `m_extraRangePerLevel` 3 to 4 m per extension, drawn by `AreaMarker`'s `CircleProjector` (workbench: radius 20, 80
  segments turning at 0.1); `m_useDistance` 1.7 to 2 m; `m_craftRequireRoof` 1 on tables and forges (tested at
  `m_roofCheckPoint`), 0 on cauldrons; `m_craftRequireFire` 1 on cauldrons; `m_useAnimation` (the player's crafting
  pose) 1 on tables, 2 on forges, 3 on cauldrons.
- **`m_inUseObject`** is shown for 1 s after each use, so it stays on while crafting: the forge's `_enabled` fire (a
  point light (1.0, 0.621, 0.482) intensity 3, range 3 m, soft shadows, flicker 0.1 at 10 with movement 0.05; flames,
  smoke, sparks and flare particles; a sound; a `SmokeSpawner`; a warmth sphere). Tables have none.
- **`m_haveFireObject`** is shown while a fire burns under the station (`EffectArea` burning area at the pivot plus
  0.25 m): the cauldron's `HaveFire` holds bubbles, steam and a small light (1.0, 0.673, 0.324), intensity 1, range
  2 m.
- **Sounds** (`m_craftItemEffects`, `m_craftItemDoneEffects`, `m_repairItemDoneEffects`): `sfx_gui_craftitem_workbench`,
  `_end`, `sfx_gui_repairitem_workbench` for tables; the `_forge` set for forges; `sfx_gui_craftitem_cauldron` for
  cauldrons. A new station reuses the set of its kind.
- **Placing:** `vfx_Place_workbench` (3 x 1 x 1 m box), `vfx_Place_forge` (1.76 x 1 x 1.79 m), `vfx_Place_cauldron`
  (0.6 x 1.5 x 0.6 m), with `sfx_build_hammer_wood` or `_stone`.

## Extensions (`station.extension`)

- **`StationExtension`:** `m_craftingStation` names the station prefab, `m_maxStationDistance` 5 m (19 extensions),
  4 m (6), 2 m (1); `m_stack` 0 (one of each).
- **The connection effect:** while the station is used the game spawns `m_connectionPrefab` at the extension's
  `m_connectionOffset` (0, 1.1, 0) on 9 extensions, 0.4 to 1.8 m up on the rest, turns it towards the station's
  `connectionEffectPoint` and stretches it along Z to the distance (`StationExtension.StartConnectionEffect`).
  `vfx_ExtensionConnection` (22 extensions) is a 0.1 x 0.1 x 1 m particle box with a glow and a puff;
  `vfx_ExtensionConnection_mage` (4, galdr table) the same in its colour. A new extension reuses one and puts its
  offset on the part that should appear to link (1 m up for a waist-high prop).
- **They are props with a job, built from existing things:** piece_workbench_ext1 is a chopping block (78 triangles,
  scaled 1.6) with the game's flint axe item mesh (214 triangles, `weapons1.mat`) stuck in it; piece_workbench_ext3 a
  log on two stumps with an adze and the wood-pile texture; the forge's six (bellows on a frame, a grinding wheel, an
  anvil on a stump, a quench barrel, a tool rack) mostly share `fi_village_forge_hd.png` (128 px). Reuse item meshes and
  textures where the game would.
- One object for all three looks (24 of 26); pre-cut destruction chunks on 16, none on 10; LOD 1 at a median 412
  triangles.

## Production stations (`station.production`)

What each script needs on the model (the prefab's own objects, `data/pieces.json` `production`):

- **Smelters and kilns (`Smelter`):** `_enabled` (the fire inside: a point light (1.0, 0.642, 0) intensity 5, range 4 m,
  soft shadows, plus a second small one; flames and smoke), two `Switch` children at the hatches the player presses (the
  smelter's `add_ore` 1.6 m up on one side, `add_wood` 1.39 m up on the opposite side), `m_outputPoint` where bars drop,
  a `SmokeSpawner` at the chimney top. The spinning wheel and windmill use `Smelter` too, with no fuel. Effects:
  `vfx_smelter_addore` / `_addfuel` / `_produce` with `sfx_smelter_add` and `_produce`; the kilns' own `vfx_kiln_*`.
- **Windmill (`Windmill`):** transforms the code spins: `m_propeller` (-600 degrees per second at full wind),
  `m_grindstone` (300), `m_bom` (the head that turns to the wind, 100); two sound loops pitched 0.3 to 1.5 with wind.
  Model the sails, the grindstone and the head as separate objects with their pivots on the axes.
- **Fermenter (`Fermenter`):** three objects the game switches, `_fermenting`, `_ready` and `_top` (the lid); 2,400 s
  per batch.
- **Cooking stations and the oven (`CookingStation`):** slots where food is shown, `HaveFuel` and `Working` objects on
  the oven and Frost Foundry (the oven's working light (1.0, 0.504, 0.324), intensity 2, range 8 m).
- **Beehive, bird nest, sap collector:** the product is shown or spawned; `vfx_pickable_pick` with `sfx_pickable_pick`.
- **Animated parts:** the spinning wheel, blast furnace and eitr refinery carry an `Animator` and skinned meshes; the
  artisan table too. Everything else is static with switched objects.

## Other stations (`station.other`)

- **Portals (`TeleportWorld`):** the wooden portal is an arch of two carved branches, 4.2 x 3.3 x 1.2 m, 408
  triangles, 256 px (`portal_small_d.png`, tint 0.80, 0.75, 0.68); the stone portal 8.6 x 7.0 m. Target-found lights:
  red (1.0, 0.39, 0) and (1.0, 0.06, 0), intensity 2, range 10 m, soft shadows; `vfx_Place_portal`.
- **The ward (`guard_stone`, `PrivateArea`):** a 1.6 m carved figure, 1,324 triangles, a glowing second material
  (`Guardstone_OdenGlow_mat`), a warm light (0.99, 0.87, 0.76) intensity 2.77, range 3 m, and a `CircleProjector` ring.
- Cartography table 4.2 x 1.6 x 2.5 m (4,452 triangles), wisp lure 5.0 m tall (1,829), the eternal pyre, the barber
  (with a water material), the shield generator 6.5 m tall.

## Paint

Measured inside the UV islands with the tint applied (`data/pieces.json` `paint`):

| Station | Texture (px) | Tint | Median colour new / worn | Luminance p10 to p90 |
| --- | --- | --- | --- | --- |
| workbench | `GameElements/Pieces/_res/WorkBench/Model/WorkBench_d.png` (256) | 0.62 | #89633f / #6f6450 | 0.35 to 0.46 |
| forge | `GameElements/Pieces/_res/Forge new/Model/Forge_d.png` (256) | 0.62 | #6e5033 / #6a6150 | 0.11 to 0.47 |
| stonecutter | `GameElements/Pieces/_res/StonecutterBench/model/StoneCutterBench_d.png` (256) | 1.0 | #a37c49 | 0.37 to 0.69 |
| smelter | `GameElements/Pieces/_res/smelter/model/smelter.png` (256) | 1.0 | #4a4944 | 0.0 to 0.49 |
| charcoal kiln | `GameElements/Pieces/_res/charcoalkiln/model/newcharcoalkiln_d.png` (256) | 1.0 | #59524c | 0.05 to 0.43 |
| fermenter | `GameElements/Pieces/_res/fermenter/fermenter_d.png` (256) | 0.77 | #73503f | 0.25 to 0.57 |
| spinning wheel | `GameElements/Pieces/_res/SpinningWheel/model/SpinningWheel_d.png` (256) | 0.60 | #825c45 | 0.25 to 0.52 |
| windmill | `GameElements/Pieces/_res/windmill/model/windmill_d.png` (256) | 1.0 | #8c7c62 | 0.34 to 0.61 |
| portal (wood) | `GameElements/Pieces/_res/portal/model/portal_small_d.png` (256) | 0.80, 0.75, 0.68 | #79553d | 0.28 to 0.43 |

How they are painted (`codex/out/pieces/textures/workbench.png`, `forge.png`, `smelter.png`, close renders):

- **Workbench:** big soft-edged islands on black: the table top a single wide island of pale orange-tan planed wood
  with long grain, scattered dark knots and nail holes; smaller islands for legs, uprights and boards; a round grey
  stone, a pale hide with a dark centre, a flat orange-brown leather patch. The 0.62 tint pulls it to mid brown
  (#89633f). Islands have a dark painted rim where they meet the black background. Worn: the same painting desaturated
  to grey-brown (#6f6450).
- **Forge:** the same wood as the workbench beside grey mottled stones, a round coal-bed island (black with orange
  glowing specks), soot-black blotches across the wood around the fire. The metal mask marks only the tongs and
  fittings.
- **Smelter and kiln:** lumpy grey stones (smelter #4a4944) blotched with soot, dark mortar, a black firebox; painted
  as big soft blobs, no crisp masonry lines.
- **Planks and timbers** are 0.1 to 0.2 m thick with a chamfer on every edge, joined by modelled leather straps and
  iron pins (`renders/close_piece_workbench.png`), the same chunky proportions as the building pieces (`pieces.md`).

### Glow

Hot and magic parts are a **second material on Unity's Standard shader with an emission map**, separate from the
Custom/Piece body: the forge's coals `ForgeCoals_mat` (`Forge_e.png`, emission 0.89), the hearth's burning ash
`HearthAshesBurn_mat` (its albedo as emission, HDR 3.45), the black forge's lava channel on FlowOpaque (colour 1.0,
0.59, 0; emission 0.57, 0.05, 0), the ward's `Guardstone_OdenGlow_mat`. Keep glowing parts in their own material so
they can be switched with the fire object and do not darken in rain.

## Damage

Stations follow `pieces.md`: modelled stations swap to a worn material below 75 % and to a broken mesh (planks fallen,
parts missing, in the worn material) below 25 %, and break into 12 to 30 pre-cut chunks (the workbench 28, the forge
28). Seen in `renders/states_5.png`: the broken workbench has its uprights and boards lying on the ground beside the
frame; the broken forge has its table tilted with the stones scattered. Health: 200 (workbench, forge, stonecutter,
artisan table, galdr table), 50 to 1,000 (extensions), 750 median for production stations.

## Icons

64 px, transparent, the high three-quarter view from the front-left used for all volumes (`pieces.md`), the station
filling 85 to 95 % of the square; lit fires and glowing parts show their glow (the blast furnace, charcoal kiln and
smelter icons have orange firebox light).

## Checklist for a new station

1. Category key, and the station kind's reference (a crafting table beside piece_workbench, a furnace beside
   smelter).
2. 1,000 to 2,500 triangles for an early or mid station, one 256 px atlas of its own at 38 to 50 px per metre, a
   metal mask for iron, glowing parts in a second Standard material with an emission map.
3. Pivot on the ground at the centre; a `leaky` box collider on `piece` round the body.
4. Children: `connectionEffectPoint` about 1 m up where extension beams should land, a roof check point above the work
   surface, `PlayerBase`, `AreaMarker` with a `CircleProjector` of radius `m_rangeBuild`, the in-use object (fire,
   light, particles, sound) or the have-fire object, the three looks with LODs, and a `Destruction` of 12 to 30 chunks.
5. Extensions: `StationExtension` naming the station, 4 to 5 m reach, a connection offset on the linking part, the
   game's `vfx_ExtensionConnection`; built from props and item meshes the game already has where it can.
6. Production: the objects its script switches, named and pivoted as the script expects (`_enabled`, switches at the
   hatches, an output point, spinning parts on their axes).
7. Effects and sounds reused by name: `vfx_Place_workbench` or `_forge`, the station kind's craft sounds, the
   material's hit and destroy pair.

## Not measured

- Animated stations (spinning wheel, blast furnace, eitr refinery, artisan table) were rendered in their rest pose; the
  clips are not measured here (`rigs/`).
- The in-use and fire objects were read from the prefabs and the decompiled `CraftingStation`, not watched in game.
