# Rocks, cliffs, ore deposits and debris

The world's stone: boulders, big half-buried rocks, cliffs and pillars, the ore deposits the pickaxe opens (copper,
tin, silver, iron in mud piles and giants' remains, obsidian), scattered debris (statues, frozen bodies, ice sheets,
pebbles), and the terrain they sit in. Trees, logs, bushes, grass and pickables are in `vegetation.md`, locations and
dungeon rooms in `locations.md`, each biome's palette and light in `../biomes.md`.

Measured on 2026-09-29 by `codex/measure/environment.py` (`codex/data/environment.json`) from every prefab the zone
system places (`Systems/_ZoneSystem.prefab` and `Systems/LocationLists/*`) and what breaking them leaves. Looks were
read from Blender renders and texture sheets in `codex/out/environment/` (`render_env_*.png`, `textures_env_*.png`,
`terrain_slices.png`, `render_extra.png` for the fractured copies), made by `environment_render.py` (Blender, point
filtered, Meadows clear-day light, a 1.8 m figure for scale) and `environment_sheets.py`. The renders show the textures
only: the shader's moss, snow and wetness are described below from the material values. Paths are under the reference
export (`%USERPROFILE%\ValheimReference\ExportedProject\Assets`).

## The numbers

Triangles are what is drawn at the closest LOD. Texel density is texture pixels per metre of surface at scale 1, the
material's tiling included; a world-projected (triplanar) material counts as texture size over its repeat length.
Longest is the longest side at scale 1; the zone system then scales most rocks by 0.5 to 8 (placement column).

| Category | n | Triangles median (range) | Texture px | px per metre median (IQR) | Longest m median (range) | Above ground m | References |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `env.rock_small` | 8 | 202 (196 to 684) | 256 (128 to 1,024) | 35.8 (27.8 to 57.2) | 2.3 (2.1 to 4.2) | 1.7 (0.7 to 3.4) | Rock_4, Rock_3, Rock_7_deepnorth, UnstableLavaRock |
| `env.rock_large` | 11 | 272 (260 to 616) | 256 (256 to 1,024) | 36.7 (33.0 to 46.0) | 27.8 (5.5 to 29.1) | 4.0 (0.7 to 16.7) | rock4_forest, rock1_mountain, rock2_heath, rock_mistlands1, Ashlands_rock2 |
| `env.cliff` | 6 | 917 (244 to 1,960) | 640 (256 to 1,024) | 22.4 (18.5 to 24.6) | 47.3 (32.7 to 73.0) | 28.7 (10.1 to 44.4) | cliff_mistlands1, cliff_ashlands4, HeathRockPillar, cliff_ashlands3_Arch_1 |
| `env.mineable` | 10 | 910 (182 to 3,264) | 256 (64 to 512) | 26.9 (18.4 to 37.4) | 13.1 (2.2 to 27.8) | 5.4 (0.9 to 15.1) | rock4_copper, MineRock_Tin, silvervein, mudpile_beacon, MineRock_Obsidian, giant_helmet1 |
| `env.debris` | 7 | 1,736 (36 to 6,186) | 128 (32 to 256) | 51.4 (31.8 to 57.6) | 3.7 (0.1 to 50.6) | 2.2 (0.4 to 4.7) | StatueEvil, FrozenGD, ice1, instanced_small_rock1 |

Reference paths: `world/Props/Rock_4.prefab`, `world/Props/Rock_3.prefab`, `world/Props/Rock_7_deepnorth.prefab`,
`world/Props/Ashlands/Rocks/model/UnstableLavaRock.prefab`, `world/Props/Rocks/rock4_forest.prefab`,
`world/Props/Rocks/rock1_mountain.prefab`, `world/Props/Rocks/rock2_heath.prefab`,
`world/Props/Mistlands/rock_mistlands1.prefab`, `world/Props/Rocks/Ashlands_rock2.prefab`,
`world/Props/Mistlands/cliff_mistlands1.prefab`, `world/Props/Ashlands/Rocks/model/cliff_ashlands4.prefab`,
`world/Props/HeathRockPillar/HeathRockPillar.prefab`, `world/Props/Mistlands/cliff_ashlands3_Arch_1.prefab`,
`world/Props/Rocks/rock4_copper.prefab`, `world/Props/MineRock/MineRock_Tin.prefab`, `world/Props/Rocks/silvervein.prefab`,
`world/Props/MudPile/mudpile_beacon.prefab`, `world/Props/MineRock/MineRock_Obsidian.prefab`,
`world/Props/Mistlands/giant_helmet1.prefab`, `world/Props/Statues/StatueEvil.prefab`,
`world/Props/DeepNorthEnv/Frozen/FrozenGD.prefab`, `world/Props/Ice/ice1.prefab`,
`world/Props/ground_clutter/instanced_small_rock1.prefab`.

What these say:

- **Rock is almost no geometry.** A 2.3 m boulder is 196 triangles; a 28 m rock 272; a 73 m cliff 1,100. The mean
  triangle edge (square root of twice the mean triangle area) is 0.33 to 0.49 m on the boulders, 2.2 to 3.2 m on the
  big rocks and 2.4 to 3.9 m on cliffs. Shape comes from a few dozen large facets; everything smaller is texture.
- **Texel density is the same as a building piece.** 28 to 57 px per metre on boulders, 33 to 46 on big rocks (a
  256 px texture tiled 8 to 10 times), 13 to 30 on cliffs. Do not give a rock a unique high-resolution atlas.
- **One LOD, short draw distance for small rocks.** Every rock has one LOD level culled at a screen height of 0.065
  to 0.30: at the game's default LOD bias of 2 and 65 degree field of view a Rock_4 disappears beyond 24 m at scale 1
  (24 to 71 m as placed at scale 1 to 3), rock4_forest beyond 175 m, rock1_mountain 315 m. Ashlands cliffs add a
  200 to 220 triangle LOD1 drawn out to 1,735 m.
- **Mostly buried.** The pivot is the ground line. rock4_forest is 16.8 m tall with 12.9 m below the pivot: 4 m shows
  above a 28 by 24 m footprint. rock1_mountain shows 9.6 of 29.1 m. Boulders show 0.7 to 1.7 m of 1.4 to 2.1 m.

## How rocks are shaped

Seen in `render_env_rock_small.png`, `render_env_rock_large.png` and `render_env_cliff.png` (views and wire views):

- **Boulders** (Rock_3, Rock_4, Rock_7): one closed lump, 10 to 14 segments round the silhouette, no bevels, smooth
  shading across facets; a flatter base sunk 0.4 to 0.7 m into the ground; slightly asymmetric, with a broad rounded
  top. Rock_7 is a standing stone 4.2 m tall and 1.3 m across. Facets are roughly equal-sized triangles with no
  long slivers.
- **Big rocks** (rock1 to rock4): low domes and lumpy mounds. rock4 (forest, coast, heath, copper, Ashlands) is a flat
  28 by 24 m cake with a gently undulating top; rock1 and rock2_mountain are 6 to 10 m high mounds with two or three
  bulges; rock3_mountain rises 16.7 m. Triangles 2 to 3 m across, fanned without pattern.
- **Cliffs**: Mistlands spires stack uneven ledges up one side (cliff_mistlands1, 43 m above ground, 1,100 triangles);
  Ashlands cliffs are ringed horns and arches built of stacked bands (cliff_ashlands4, 42 m; cliff_ashlands3_Arch_1,
  a 33 m arch at 1,960 triangles with facets under 1 m, the finest of the cliffs); HeathRockPillar is a 44 m
  pale column of 244 triangles.
- **Ashlands rocks** carry the most geometry per metre (UnstableLavaRock 684 triangles for 3.3 m: bulging lobes with
  glowing orange cracks) and 1,024 px textures.

## How rocks are painted

- **Big rocks and Mistlands cliffs share the terrain's cliff texture.** rock1..4 and cliff_mistlands1/2 draw
  `world/terrain/old/gouacherock_big.png` (256 px, tones #434341 / #575754 / #71706b: a soft grey with lighter
  blotches, like gouache) tiled 8 by 8 or 10 by 10. The Mistlands cliffs darken it with a material colour of 0.255
  (#414141). The painting is not the rock's own: a new big rock should draw a tiling rock texture at the same repeat
  (32 to 40 px per metre), not a baked unique atlas.
- **Boulders have their own small atlas.** `3rd party/A_piece_of_nature/Textures/Rocks_4_texture.png` (128 px) and
  `Rocks_3_4_texture.png` (256 px): four or five UV islands of pale grey-green stone (#848673 / #868773 / #8c8e7b),
  very low contrast, soft darker crevices, the alpha channel a grey gloss map (material names end in `_roughness`).
- **Ashlands rock** (`world/Props/Ashlands/Rocks/model/AshlandsRock_d.png`, 1,024 px) is a blue-black
  (#1b242f / #252d38 / #2e3742) with faint wavy strata lines.
- **Top cover comes from the shader, not the texture.** Every rock material names a `_MossTex` that the `StaticRock`
  shader lays over its up-facing surfaces, and that texture is the biome's ground: `stonemoss.png` (Meadows),
  `world/terrain/old/forest_d.png` (Black Forest: the terrain's forest texture itself), `stonemoss_heath.png` (Plains),
  `stonemoss_swamp.png` (Swamp), `stonekit_moss.png` (Mountain rocks, bark), `stonemoss_bw.png` (Mistlands),
  `AshOnRocks_d.png` (Ashlands), `DeepNorthEnv/.../pillar_snow_d2.png` (Deep North: snow as the moss layer). A rock
  made for two biomes is the same mesh and texture with two materials that differ only in `_MossTex`: Rock_4 /
  Rock_4_plains / Rock_4_deepnorth, Rock_3 / Rock_3_deepnorth, rock4_forest / rock4_coast / rock4_heath.
- **Moss values** (the shader's labels: "Moss alpha", "Texture blend" 0 to 10, "Transition Size") come in two
  recurring settings: `_MossAlpha` 1 with `_MossBlend` 0 (Rock_3, Rock_4, rock4_coast, old logs, stumps) and
  `_MossAlpha` 0 with `_MossBlend` 5 to 10 (rock4_forest, rock_heath, the Mistlands cliffs, beech and oak bark);
  `_MossTransition` is 0.1 to 0.33. A few mix both (HeathRockPillar alpha 1 and blend 7.7, Ashlands rock 1 and 7.4)
  and rock1_mountain uses neither (0 and 0). How each blends is inside the compiled shader: copy the values of the
  nearest reference.
- **Snow and rain.** `_AddSnow` is 1 on nearly every world material (off on Ashlands rock, the silver vein, lava rock,
  the swamp statue and a few plants and pickables) and `_AddRain` 1 makes surfaces wet in rain; no game script feeds a snow amount, so how much snow
  the shader lays where is decided inside it. The Deep North relies on its snow-textured moss variants.
- **Gloss and normals.** `_Glossiness` 0.19 to 0.37 (0.18 Ashlands, 1.0 on rock_mistlands with a gloss map); normal
  strength `_BumpScale` 1 to 2.4. No metal map on plain rock; ore adds `_MetalTex` (below).

`StaticRock` properties a new rock sets (display names from `Shaders/StaticRock.shader`): `_MainTex` and tiling,
`_Color` (tint), `_BumpMap` and `_BumpScale`, `_Glossiness` or `_GlossMap` with `_UseGlossMap`, `_MetalTex` /
`_Metallic` / `_MetalGloss` for ore, `_MossTex` / `_MossAlpha` / `_MossBlend` / `_MossColor` / `_MossGloss` /
`_MossNormal` / `_MossTransition`, `_TriplanarMap` and `_TriplanarScale` (world-projected mapping, used on fracture
insides), `_AddSnow`, `_AddRain`, `_EmissiveTex` / `_EmissionColor` (lava), `_FRESNEL` edge glow. A mod does not ship
this shader: borrow the game's material at runtime (`GameMaterials.Borrow`) or copy a game rock material and swap its
textures.

## How rocks break

Every rock except the one-hit boulders is two prefabs: the whole rock the zone system places, and a pre-fractured copy
that replaces it on the first hit.

1. The whole rock carries `Destructible` with health 1 and `m_spawnWhenDestroyed` naming the copy (`rock4_forest` to
   `rock4_forest_frac`). It has no hit or destroy effects of its own.
2. The copy carries `MineRock5`: every collider under it is one hit area with its own mesh, health 50 (30 on Rock_3,
   70 on cliffs, 100 on Ashlands cliff4 and giant ribs, 150 on giant metal, 5 on mud piles) and its own drop. It looks
   identical from outside (`render_extra.png`); the broken faces inside use a world-projected material:
   `rock_internal` (`Trilinearmap`, `gouacherock.png` 32 px, one repeat per 1.33 m) or the Mistlands' triplanar
   `mistlands_cliff_internal` (512 px, one repeat per 10 m).

| Rock | Whole triangles | Copy triangles | Hit areas | Median area size m | Health per area |
| --- | --- | --- | --- | --- | --- |
| Rock_3 | 202 | 572 | 5 | 1.7 | 30 |
| rock_mistlands1 | 616 | 2,276 | 21 | 2.1 | 50 |
| rock4_forest (and coast, heath, copper) | 272 | 7,490 | 130 | 5.0 | 50 |
| rock2_heath / rock2_mountain | 260 | 7,111 | 122 | 3.3 / 4.4 | 50 |
| rock1_mountain | 358 | 9,808 | 165 | 4.6 | 50 |
| cliff_mistlands1 | 1,100 | 11,969 | 191 | 7.3 | 70 |
| HeathRockPillar | 244 | 11,836 | 194 | 4.7 | 60 |
| silvervein | 196 | 4,692 | 100 | 1.6 | 50 |

Hit areas are 1.5 to 7 m chunks; the copy has 3 to 48 times the whole rock's triangles (25 to 30 times on the big
rocks). Small boulders (Rock_4, Rock_7)
skip the copy: `Destructible` with health 30 (200 on Rock_7) breaks them in one go.

Effects to reuse (names are prefabs in the export): hit `vfx_RockHit` + `sfx_rock_hit`; a boulder destroyed
`vfx_RockDestroyed` + `sfx_rock_destroyed`; a hit area destroyed `vfx_RockDestroyed_large` (dust, 3D stone chunks with
`GameElements/Items/_res/stone/rock_low.png`, pebbles) + `sfx_rock_destroyed`. Ashlands stone uses
`vfx_GraustenDestroyed` / `_large`; metal remains `vfx_HitSparks` + `vfx_GiantMetal_destroyed`; marble
`vfx_RockHit_Marble` / `vfx_RockDestroyed_marble`; obsidian `vfx_RockHit_Obsidian` / `vfx_RockDestroyed_Obsidian`; mud
`sfx_MudHit` / `sfx_MudDestroyed`; ice `vfx_ice_hit` + `sfx_ice_hit`, `vfx_ice_destroyed` + `sfx_ice_destroyed`. Drops
are stone (and the ore) per area, 2 to 8 per area on big rocks.

## Ore deposits and mineable remains

| Deposit | Biome | What it is | Triangles | Texture | Health, tool tier | Drops |
| --- | --- | --- | --- | --- | --- | --- |
| rock4_copper | Black Forest | the rock4 cake painted with ore, `copper_ore_big_d.png` tiled 8 (grey stone with blue-green copper streaks and gold-orange veins) | 272 (copy 7,490) | 256 px | 50 per area, 0 | Stone, CopperOre (weight 0.5), 2-4 per area |
| MineRock_Tin | Black Forest shores (-0.6 to 1.5 m altitude) | the Rock_4 boulder on an opaque Standard material with `Rocks_4_tin_d.png` (128 px: near-black stone, pale grey-white tin veins) and a metallic-gloss map | 196 | 128 px | 30, 0 | TinOre 3-4 |
| silvervein | Mountain, above 120 m | a flat Y-shaped vein 17 by 11 m, 2 m proud of the snow, `silver_ore_d.png` (grey with white veins) tiled 2.5 | 196 (copy 4,692) | 128 px | 50 per area, 2 | Stone, SilverOre (0.5) |
| mudpile_beacon | Swamp | a 3 m mound of mud (`mudpile_d.png` 64 px tiled 6) studded with skulls, shields and scrap from an atlas | 2,660 | 64-256 px | 5 per area, 0 | IronScrap, WitheredBone (0.01) |
| MineRock_Obsidian | Mountain, above 100 m | a 2 m faceted black glassy lump (#040404 / #393837 / #5a5a5a) with a metal map | 182 | 256 px | 30, 2 | Obsidian 5-8 |
| giant_helmet1/2, giant_sword1/2 | Mistlands | half-buried rusted giant arms, `GiantRust_d.png` (orange-red rust with blue-green patina) | 690 to 3,264 | 512 px | 150 per area, 3 | IronScrap, CopperScrap |
| giant_ribs | Mistlands | a giant's rib cage, a 64 px bark texture tiled 20 times and darkened to 0.37 | 2,218 | 64 px | 100 per area, 3 | BlackMarble |

How ore reads: the ore is painted into the rock texture as streaks and veins in the metal's colour (copper blue-green
with orange, silver white, tin white-grey on black), and the material adds a `_MetalTex` so the veins catch the light.
The deposit's silhouette is the biome's ordinary rock (rock4, Rock_4) or a distinct low shape (the silver vein, the
mud pile), never a crystal cluster; only obsidian is faceted glass. Tin is the one deposit on the plain Standard
shader, so it takes no moss or snow.

## Debris

- **StatueEvil** (`world/Props/Statues/StatueEvil.prefab`, Swamp, 2 per zone): a 5.2 m bent monolith, 6,186 triangles,
  sculpted smooth (0.1 m facets), `statue1` with swamp moss. The exception to low-poly rock: a sculpted statue.
- **Frozen bodies** (`FrozenGD`, `FrozenSkeleton_Pose1/2`, Deep North): posed creature meshes (1,736 to 4,317
  triangles) in one pale ice-blue texture (#a5b2b7 / #afc4cb / #c0d2d7), `Destructible` health 30 that leaves an Ice
  item, `vfx_frozengd_destroyed`.
- **Ice sheets** (`ice1`, 8 m, 36 triangles; `IceShore_1`, 50 m, 148 triangles): flat untextured Standard material
  #d6f8ff, a 2x2 tiled normal map.
- **Pebbles** (`instanced_small_rock1`, every biome, 15 a patch): a 48 triangle cluster 0.1 m across on
  `rock_low.png` 32 px, tinted #cdbda9.

## The terrain an asset sits in

The ground is a 64 m heightmap zone with a vertex every 1 m, drawn from a 16-slice texture array (256 px a slice) by
biome vertex colour; slices and tones are in `../biomes.md`. The heightmap material tiles its textures with
`_UVScale` 0.5; with 256 px slices that is 128 px per metre if the scale is applied to world metres (the shader is not
readable, so treat this as an estimate). Steep slopes switch to the cliff slice with its own normal map
(`gouacherock_big_n.png`, `mistlands_rock_normal.png`), which is why big rocks drawing `gouacherock_big.png` blend
into cliffs.

Placement (from the zone system's `m_vegetation`): rocks get a random yaw, a random tilt (`m_randTilt`: 0 on the
boulders that follow the ground, 5 to 20 degrees on big and coast rocks), a scale range (Rock_4 1 to 3, Rock_3 2 to 8, rock4 0.6 to 1.2, rock1_mountain 1 to 1.5), and a count of
placement attempts per 64 m zone (Rock_4 up to 40 in the Meadows, Rock_3 up to 15, big rocks 1 to 10, the Ashlands'
small rock up to 196). Boulders, tin and obsidian take the ground's slope (`m_chanceToUseGroundTilt` 1); big rocks
and cliffs stand upright or tilt at random and are limited to slopes: rock1 and rock2_mountain only go on 30 to 80
degree ground (they are the mountainside's rock faces), rock4 on 5 to 45 degrees above 4 m altitude, cliff_mistlands1
on 25 to 50 degrees.

## Building a new rock, cliff or deposit

1. Pick the category and nearest reference above; copy its size at scale 1 and decide its placement scale range.
2. Model one closed lump (or a stack of lumps for a cliff) with facets the size of the reference's (0.3 to 0.5 m on a
   2 m boulder, 2 to 3 m on a 20 to 30 m rock). No bevels, no chips; smooth normals across the whole rock.
3. Put the pivot on the ground line and sink the rock: a third of a boulder, half to three quarters of a big rock.
4. Boulders: one small UV atlas (128 to 256 px) at 28 to 57 px per metre, painted soft, low-contrast grey with darker
   crevices. Big rocks and cliffs: UVs laid for a tiling texture, 256 px repeating every 6 to 8 m (32 to 40 px per
   metre); use the game's own `gouacherock_big.png` through a borrowed material where possible.
5. Leave the top clean: the biome's moss, ash or snow comes from `_MossTex`. Make one material per biome by swapping
   `_MossTex` (and `_MossAlpha` / `_MossBlend` as the reference has them), like Rock_4 / Rock_4_plains / Rock_4_deepnorth.
6. One LODGroup level, culled at a screen height of 0.07 (boulder) to 0.25 (big rock); add `LodFadeInOut`.
7. Breaking: a boulder gets `Destructible` (health 30 to 50, `vfx_RockDestroyed`, `sfx_rock_destroyed`). A big rock
   gets `Destructible` health 1 spawning a `_frac` copy with `MineRock5`: cut the same mesh into 1.5 to 7 m chunks,
   one collider and mesh per chunk, insides on a triplanar rock material, health 50 per chunk,
   `vfx_RockHit` / `vfx_RockDestroyed_large`. Ore: the same, with the ore streaked into the texture, a `_MetalTex`,
   and the ore in the drop table at weight 0.5.
8. Check it on the terrain slice of its biome under that biome's light (`../biomes.md`), beside two references.

Do not: give rocks 1,000-triangle detail, bevelled edges or sharp chipped corners; paint moss or snow into the albedo
(it doubles with the shader's); use a unique 1,024 px atlas on a boulder; float the rock on the ground with its full
underside showing.
