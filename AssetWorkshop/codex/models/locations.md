# Locations and dungeon rooms

The places the zone system drops into the world whole: boss altars, rune stones, stone circles and henges, ruins,
abandoned houses and camps, Dvergr towers, natural sites (tar pits, nests, giants' remains), dungeon entrances, and
the rooms dungeons and generated camps are strung together from. Single props (rocks, trees) are in `environment.md`
and `vegetation.md`; the building pieces locations are made of are in `pieces.md`.

Measured on 2026-09-29 by `codex/measure/environment_locations.py` (through `environment.py`, data in
`codex/data/environment.json`, keys `location.*` and `location_list`): the 168 enabled `m_locations` entries of
`Systems/_ZoneSystem.prefab` and `Systems/LocationLists/*` (158 distinct location prefabs under `world/Locations/`
found in the export), and the 360 enabled `Room` prefabs under `world/Rooms/`. Looks were read from Blender renders
(`codex/out/environment/render_location_*.png`, a high three-quarter view on a terrain-green ground with a 1.8 m
figure). The export flattens
each location into one hierarchy, so every part below is counted by the child GameObject's name matching a prefab in
the export (`reused_parts`). Paths are under the reference export.

## The numbers

Triangles are everything a location draws up close with every random part switched on (the game switches about half
of them off, below); texel density is area-weighted over all its materials.

| Category | n | Triangles median (range) | px per metre median (IQR) | Longest m median (range) | Materials | Random parts | References |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `location.altar` | 33 | 3,422 (154 to 154,836) | 32.0 (25.3 to 33.4) | 20.3 (4.0 to 76.6) | 3 | 0 (0 to 108) | Eikthyrnir, GDKing, Runestone_Meadows, StoneHenge1, Dolmen01 |
| `location.ruin` | 43 | 74,014 (1,610 to 2,329,606) | 23.8 (16.3 to 27.3) | 18.9 (5.2 to 324, the charred fortress's beam) | 6 | 7 (0 to 1,106) | Ruin1, StoneTowerRuins04, SwampRuin1, CharredRuins2, Mistlands_GuardTower1_ruined_new |
| `location.camp` | 33 | 17,306 (7,124 to 111,382) | 41.3 (33.6 to 43.7) | 11.1 (6.3 to 61.2) | 10 | 24 (0 to 368) | WoodHouse1, SwampHut1, AbandonedLogCabin02, Greydwarf_camp1, Hildir_camp |
| `location.structure` | 14 | 53,567 (448 to 379,670) | 30.5 (27.7 to 47.4) | 26.2 (2.4 to 50.2) | 5 | 43 (0 to 462) | Waymarker01, Mistlands_Lighthouse1_new, Mistlands_Viaduct2, Mistlands_Statue1 |
| `location.dungeon_entrance` | 18 | 50,662 (3,702 to 524,604) | 35.1 (30.4 to 39.1) | 38.3 (12.0 to 74.0) | 10 | 43 (0 to 136) | Crypt2, SunkenCrypt4, TrollCave02, MountainCave02, Mistlands_DvergrTownEntrance1 |
| `location.natural` | 17 | 8,924 (2,074 to 78,200) | 26.5 (22.1 to 32.0) | 29.9 (7.5 to 75.5) | 3 | 5 (0 to 38) | TarPit1, DrakeNest01, InfestedTree01, Mistlands_Swords1, SulfurArch |
| `location.dungeon_room` | 360 | 13,977 (220 to 1,491,952) | 37.8 (30.3 to 51.3) | 15.9 (2.3 to 77.0) | 5 | 22 (0 to 768) | cave_new_sloperoom02, dvergr_roombig_open02, plainsfortress_Hildir_Start, hole_corridors01_alt2, dvergr_new_bossroom_ENTRANCE02 |

Reference paths: `world/Locations/Meadows/Eikthyrnir.prefab`, `world/Locations/BlackForest/GDKing.prefab`,
`world/Locations/Meadows/Runestone_Meadows.prefab`, `world/Locations/Heath/StoneHenge1.prefab`,
`world/Locations/Meadows/Dolmen01.prefab`, `world/Locations/BlackForest/Ruin1.prefab`,
`world/Locations/Mountains/StoneTowerRuins04.prefab`, `world/Locations/Swamp/SwampRuin1.prefab`,
`world/Locations/Ashlands/CharredRuins2.prefab`, `world/Locations/Mistlands/Mistlands_GuardTower1_ruined_new.prefab`,
`world/Locations/Meadows/WoodHouse1.prefab`, `world/Locations/Swamp/SwampHut1.prefab`,
`world/Locations/Mountains/AbandonedLogCabin02.prefab`, `world/Locations/BlackForest/Greydwarf_camp1.prefab`,
`world/Locations/Meadows/Hildir_camp.prefab`, `world/Locations/Mountains/Waymarker01.prefab`,
`world/Locations/Mistlands/Mistlands_Lighthouse1_new.prefab`, `world/Locations/Mistlands/Mistlands_Viaduct2.prefab`,
`world/Locations/Mistlands/Mistlands_Statue1.prefab`, `world/Locations/BlackForest/Crypt2.prefab`,
`world/Locations/Swamp/SunkenCrypt4.prefab`, `world/Locations/BlackForest/TrollCave02.prefab`,
`world/Locations/Mountains/MountainCave02.prefab`, `world/Locations/Mistlands/Mistlands_DvergrTownEntrance1.prefab`,
`world/Locations/Plains/TarPit1.prefab`, `world/Locations/Mountains/DrakeNest01.prefab`,
`world/Locations/Swamp/InfestedTree01.prefab`, `world/Locations/Mistlands/Mistlands_Swords1.prefab`,
`world/Locations/Ashlands/SulfurArch.prefab`; rooms under `world/Rooms/cave/`, `world/Rooms/mistlands/`,
`world/Rooms/rooms/` and `world/Rooms/hole/`.

What these say:

- **A location is an assembly, not a model.** No location is one mesh. The most reused parts across all 158 are the
  player's own building pieces and the game's props: `stone_wall_2x1` 3,015 times, `vines` 2,269, `Ashlands_Wall_2x2`
  1,768, `blackmarble_2x2x2` 1,590, `wood_floor` 556, `stone_wall_1x1` 347, `stave_wall_2x2` 290, the terrain helper
  `LevelTerrain` 270, crypt `Stone` 172, `caverock_curvedrock` 167, `woodwall` 139.
- **Texel density matches the pieces and props it is built from**: 24 to 41 px per metre, lower on the Ashlands
  ruins (12 to 17) whose `Ashlands_*` pieces use 128 px textures over 2 to 6 m.
- **Triangle totals follow from the piece count**, not from a budget: a stone ruin of 200 to 400 wall pieces is
  75,000 to 250,000 triangles; a rune stone 154.

## How a location is composed

A location prefab's root carries `Location` (and usually `ZNetView`), and its children are everything in it:

- **`Location`**: `m_exteriorRadius` (the area it owns: 3 m for a waymarker, 8 m for a house or rune stone, 10 to 12 m
  for crypts and ruins, 20 to 32 m for towers, camps, henges and boss altars), `m_clearArea` (true on 123 of 158:
  every tree, rock and bush the zone system would place inside the exterior radius is left out; houses, dolmens and
  swamp ruins keep theirs), `m_noBuild` (the player cannot build inside; set on only 7), `m_applyRandomDamage`
  (true on 91: every building piece in it starts at 10 to 60 % of its health, so `WearNTear` shows its worn or broken
  model), `m_hasInterior` with `m_interiorRadius` and `m_interiorEnvironment` (the dungeon, below).
- **The placement entry** in the location list: biomes, a count for the whole world (`m_quantity`: 1 start temple, 3
  to 5 boss altars, 20 of each wood house, 200 to 300 Greydwarf nests and burial chambers, 700 infested trees),
  `m_minDistanceFromSimilar`, the largest terrain height difference it accepts (`m_maxTerrainDelta`, 2 to 3 m for most),
  altitude, random rotation, whether it snaps to water. The exterior radius here usually matches the prefab's.
- **Ground work**: `TerrainModifier` children level, smooth and paint the terrain when the location is placed.
  Most use one on the root (smooth, paint cleared); bigger sites drop the `world/LevelTerrain.prefab` helper several
  times (level a 4 m square, smooth 5 m, paint "cleared" dirt): StoneHenge1 has 20, the Dvergr towers 8, MountainCave02
  10. Some sites paint nothing and sit on the natural ground (SulfurArch, the viaducts).
- **Parts**: the building pieces, props, rocks and trees that make it, placed by hand. Stone ruins are stone walls
  (`stone_wall_2x1`, `stone_wall_1x1`, `stone_arch`, `stone_stair`, `stone_floor_2x2`) with wood floors and roofs;
  Meadows houses are `woodwall`, `wood_roof_45`, `wood_pole`, `wood_beam_45` and `wood_floor` draped in `vines`;
  mountain cabins `wood_wall_log`; Mistlands towers `blackmarble_*` blocks, Dvergr wood props and `vines`; Ashlands
  ruins `Ashlands_Wall_2x2`, `Ashlands_Pillar4`, `Ashlands_Floor` and pre-broken `Ashlands_Ruins_Wall_*_broken*`
  walls; henges and dolmens `highstone`, `widestone`, `Rock_3`, `Rock_7`; altars add a rune stone
  (`RuneStone_*`), an offering altar (`offeraltar_*`) and statues.
- **Life and loot**: creature spawners (`Spawner_Greydwarf`, `Spawner_Draugr`, `Spawner_GoblinBrute`...), treasure
  chests (`TreasureChest_*`), pickables (`Pickable_Stone`, `Pickable_Mushroom`, `Pickable_DolmenTreasure`), crows,
  torches (`CastleKit_groundtorch`), guide points for the raven.
- **Variation**: `RandomSpawn` on parts switches each on with a chance when the location is created (median 50 %,
  range 10 to 100 %; `m_OffObject` can show something else instead). Ruins, towers and houses carry dozens to
  hundreds (WoodHouse1 49 at a median 80 %, StoneTowerRuins04 15 at 70 %, Mistlands_GuardTower1_ruined_new 546 at
  50 %, CharredRuins1 1,106 at 60 %); boss altars and rune stones none. So two copies of a ruin differ.

## The ruin look

From `render_location_ruin.png`, `render_location_camp.png` and the numbers above, a ruin in this game is:

- **Whole building pieces with pieces missing**, not broken sculpted walls: stone walls stepped down unevenly from a
  full-height corner (Ruin1, StoneTowerRuins04), floors half gone, roofs with gaps. The random switches take out
  individual 2 by 1 m blocks, so the broken edges are stepped in the piece grid.
- **Worn and broken piece models**: `m_applyRandomDamage` puts every piece between 10 and 60 % health, which shows the
  piece's own worn (below 75 %) or broken (below 25 %) mesh.
- **Scattered fallen blocks** on the ground round the base (SwampRuin1: single wall pieces lying at angles).
- **Plants over it**: `vines` hung on walls (29 on WoodHouse1, 258 on a Mistlands tower), grass and moss on roofs
  (the Meadows houses' roofs are green with vines), ferns in the Ashlands ruins (`FernAshlands` 150 times).
- **Crows** (`Crow` parts) circling over ruins and crypts (Ruin1, SwampRuin1, Crypt2).

## Categories

- **Altars** (`location.altar`): the boss altars are a ring or square of standing stones and statues around an
  offering altar and a rune stone (Eikthyrnir: 4 `Stone1_huge`, a deer statue, 3,422 triangles, radius 10 m; GDKing:
  a 27 m stone-block platform with four pillars, 12,678 triangles, radius 25 m). Rune stones are one 154-triangle
  standing stone (3.2 by 4 m) with its `RuneStone` script. Henges (`StoneHenge1-6`, Plains) are rings of 13 to 28
  `highstone` and `widestone` with rocks, 20 to 43 m across, randomly damaged. Dolmens are a table of `Rock_7` stones
  12 m across.
- **Ruins** (`location.ruin`): stone houses and towers (Black Forest, Mountains, Plains, Swamp), wells, graves,
  ship wrecks (`shipwreck_karve_*` parts, 1,600 to 3,100 triangles), and the Ashlands charred ruins and fortresses
  (up to 2.3 million triangles, 1,768 wall pieces on CharredFortress).
- **Camps and houses** (`location.camp`): Meadows wood houses (7,000 to 28,000 triangles, 4 to 15 m, radius 6 to
  10 m, no clearing), swamp huts on stilts, mountain log cabins, the Greydwarf nest (16,682 triangles of roots round a
  nest), Hildir's camp (30 materials: tents, rugs, props), the Dvergr excavations, and the
  generated camps (`GoblinCamp2`, `WoodFarm1`, `WoodVillage1`, `NorthVillage`, which hold only a `DungeonGenerator` and
  are assembled from rooms at runtime, below).
- **Structures** (`location.structure`): Dvergr guard towers, lighthouse, harbour and viaducts (black marble blocks,
  iron floors, Dvergr wood props, vines; 80,000 to 380,000 triangles), statues and waymarkers (a 448-triangle cairn
  of stacked flat stones).
- **Dungeon entrances** (`location.dungeon_entrance`): a small outside (Crypt2: a mound of rock slabs 13 by 16 m;
  SunkenCrypt4: a stepped block of sunken-crypt walls; TrollCave02: a rock arch; MountainCave02: a rock dome with an
  ice mouth) and an interior 5,000 m above it. The interior is a `DungeonGenerator` (below) or hand-placed rooms.
- **Natural sites** (`location.natural`): tar pits (tar pickables, `lox_ribs` bones, blob spawners), the drake nest
  (8 `NestRock` round an egg), the infested tree (a SwampTree2 with 9 guck sacks), giants' sword and helmet
  groups, the sulfur arch (cliff_ashlands3_Arch_1 with 18 small cliff rocks and 20 sulfur pickables), Mistlands rock
  spires, Deep North frozen troll remains.

## Dungeons

An entrance's `Location` has `m_hasInterior`; its interior sits exactly 5,000 m above the entrance (the gizmo in
`Location.cs` draws it at `position + (0, 5000, 0)`), lit by its own environment (`m_interiorEnvironment`: `Crypt`,
`SunkenCrypt`, `Caves`, `InfectedMine`). The export's bounds and renders above leave the interior out.

The interior is a `DungeonGenerator` (prefabs `world/dungeon/DG_*.prefab`) that picks rooms of its theme and joins
them door to door. Settings measured: `DG_ForestCrypt` theme ForestCrypt, 20 to 40 rooms, 8 m tile, grid 4;
SunkenCrypt 20 to 30 rooms; MountainCave02 3 to 64 rooms; the Dvergr town 16 to 96 rooms. Generated camps
(`DG_GoblinCamp`, `DG_MeadowsFarm`, `DG_MeadowsVillage`, `DG_NorthVillage`, `DG_AshlandRuins`, `DG_FortressRuins`)
use the camp algorithm (`m_algorithm` 2): rooms placed round a ring with perimeter sections, on 10 m tiles
(`DG_GoblinCamp` 15 to 25 rooms on a 15 to 30 m ring; the Meadows farm and village, the north village and the
Ashlands ruins 15 to 30 rooms on rings of up to 32 m).

A room is a prefab with `Room` on the root: `m_size` (the cell it fills, metres), `m_theme` (a bit mask), flags for
entrance, end cap and divider, a weight, and `RoomConnection` children (the doorways, each with a type string that must
match the room it joins, and an `m_entrance` flag). Connection types seen: blank (any), `shrine`, `ice`, `loot`,
`dome`, `deeproom` (caves), `dvergr`, `dvergropen`, `stone` (Dvergr town), `wall`, `balcony`, `window`,
`middlewall` (Hildir's fortress), `stair`, `stair2` (Mörk halls), `boss_*` (Dvergr boss).

The half-buried crypt's theme is a bit (262144) that `Room.Theme` does not name; the codex calls it HalfBurriedCrypt.

| Theme | Rooms | Typical size m | Connections | What rooms are built from | Main shader | px/m median |
| --- | --- | --- | --- | --- | --- | --- |
| Cave (+CaveHildir) | 82 + 12 + 1 | 12 x 12 x 12, domes 48 | blank, shrine, ice, loot, dome | `caverock_curvedrock`, `caverock_pillar`, `MountainKit_int_wall_4x4`, ice | StaticRock 98 % | 35 |
| DvergerTown | 48 | 8 x 8 x 8, 16 x 12 x 16 | dvergr, stone, dvergropen | `dvergrrock_curvedrock`, `dvergrtown_4x2x1`, arches, seeker eggs | Bonemass (the infestation) 66 % | 68 |
| PlainsFortHildir | 33 | 4 x 4, 19 x 8 x 19 | wall, balcony, window | `SunkenKit_int_wall_2x4`, corner stairs, rusty iron walls | StaticRock 74 % | 29 |
| Hole | 32 | 15 x 8 x 15, 30 x 18 x 15 | blank | `HoleRock_curved2`, `HoleRock_small1`, glow worms | StaticRock 97 % | 44 |
| DvergerBoss | 18 | 16 x 13 | boss_* | `blackmarble_creep_*`, Dvergr town arches and floors | Bonemass | - |
| GoblinCamp | 17 | 10 x 8 x 7, 12 x 8 x 12 | none (camp) | `goblin_woodwall_1m`, `goblin_pole`, `goblin_roof_45d`, spawners | Piece 65 % | 37 |
| MorkHalla | 16 | 60 x 20 x 60 | stair, stair2 | `Morkhalla_Wall_20`, rubble, bounding walls | StaticRock 75 % | 32 |
| SunkenCrypt | 16 | 12 x 10 x 8, 12 x 10 x 12 | blank, water | `SunkenKit_int_wall_4x4` / `_2x4`, `_int_floor_4x4`, arches, green torches | StaticRock 94 % | 28 |
| MeadowsFarm / MeadowsVillage | 13 / 10 | 15 x 8 x 15, 16 x 8 x 20 | none | `wood_roof_45`, `woodwall`, `wood_floor`, `wood_fence`, `LevelTerrain` | Piece 93 to 99 % | 45 to 51 |
| ForestCrypt (+Hildir) | 6 + 12 + 3 | 6 x 5 x 6, 16 x 6 x 8 | blank | `stonewall`, `StoneKit_int_floor_2x2`, dirt decals, yellow mushrooms | StaticRock 78 to 92 % | 53 |
| NorthVillage | 9 | 11 x 8 x 3 to 15 x 15 x 15 | none | `stave_wall_2x2`, `wood_floor`, `stave_beam_*`, `darkwood_roof_67` | Piece 80 % | 39 |
| HalfBurriedCrypt | 14 | 6 x 5 x 6, 6 x 5 x 10 | blank | `stonewall_2`, `highstone_2`, `widestone`, `crypt_rock`, yellow mushrooms | StaticRock 100 % | 67 |
| AshlandRuins, FortressRuins | 5 + 3 + 10 | 10 x 4 x 10, 20 x 4 x 20 | none | `Ashlands_Floor`, `Ashlands_WallBlock`, broken pillars, ferns | Piece 97 % | 12 to 17 |

Rooms are kits the same way locations are: dozens of placed pieces per room (a sunken crypt room holds 32
`SunkenKit_int_wall_2x4` alone), the crypts on the `SunkenKit_*` and `StoneKit_*` interior walls and floors, the caves on a dozen reusable rock shapes (`caverock_*`, `HoleRock_*`), the
villages on the player's building pieces. The room's texture density is the kit's (28 to 53 px per metre).

## Building a new location or room

1. Pick the category and the reference nearest in size; copy its exterior radius, clearing, random damage and
   quantity.
2. Build it from the game's own parts where the game would: its building pieces (`pieces.md`), rocks
   (`environment.md`), trees and bushes (`vegetation.md`), spawners, chests, torches. Only the parts the game does not
   have are new models, each made to its own category's budget.
3. Put a `TerrainModifier` on the root (smooth and paint cleared) and `LevelTerrain` helpers under floors.
4. Put `RandomSpawn` on everything that can be missing (walls, floors, roof pieces, props, spawners) at 25 to 80 %;
   set `m_applyRandomDamage` for a ruin so its pieces show their worn and broken models.
5. Dress it: vines on walls, fallen pieces at the base, ferns or grass on top, crows over ruins, a rune stone or
   treasure chest as the reward.
6. A dungeon: a small outside with `m_hasInterior`, and an interior 5,000 m up made of rooms on the theme's grid
   (8 m tiles for crypts), each room a `Room` with `m_size`, its `RoomConnection` doorways typed to match, end caps
   and an entrance.
7. Render it high three-quarter with every random part on and with half of them off, beside a reference of its
   category, under its biome's light (`../biomes.md`).

Do not: model a location as one sculpted mesh; give it new textures for walls, floors and rocks the game already
has; build a ruin with smoothly broken wall edges (the game's break along piece seams); leave the ground unlevelled
under floors or trees growing through floors (`m_clearArea`).
