# Trees, logs, bushes, grass, flowers and pickables

Everything that grows: the felled trees of every biome and what felling leaves (stumps, logs, half logs), saplings and
small trees, bushes and shrubs, the instanced grass and ground cover, flowers, and pickables (stones, flint, branches,
mushrooms, wild seeds, berry bushes). Rocks and the terrain are in `environment.md`, each biome's palette and light in
`../biomes.md`.

Measured on 2026-09-29 by `codex/measure/environment.py` (`codex/data/environment.json`) from every prefab the zone
and clutter systems place and every stub, log and half log they leave. Looks were read from Blender renders and
texture sheets in `codex/out/environment/` (`render_env_tree_*.png`, `render_env_bush.png`, `render_env_grass.png`,
`render_env_pickable.png`, `render_env_log_stump.png`, `textures_env_*.png`), made by `environment_render.py` (point
filtered, Meadows clear-day light, a 1.8 m figure, a wire view showing every card) and `environment_sheets.py`. Grass
was planted as a 3 m patch of its instanced mesh and tinted by its terrain colour map's mean colour. Paths are under
the reference export.

## The numbers

Triangles at the closest LOD, texel density at scale 1 (tiling included), longest side and height above the pivot at
scale 1. The zone system scales trees by 0.3 to 3 (table below).

| Category | n | Triangles median (range) | Texture px | px per metre median (IQR) | Longest m | Above ground m | References |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `env.tree_meadows` | 4 | 443 (344 to 578) | 256 leaves, 512 bark | 32.0 (26.2 to 38.7) | 27.8 (23.5 to 35.9) | 24.1 | Beech1, Oak1, Birch1 |
| `env.tree_plains` | 2 | 399 (344 to 454) | 128 leaves, 256 bark | 25.6 | 24.3 | 23.6 | Birch1_aut, Birch2_aut |
| `env.tree_blackforest` | 2 | 596 and 2,071 | 512 and 256 (one atlas) | 95.8 and 39.1 | 10.9 and 24.4 | 10.6 and 23.8 | FirTree, Pinetree_01 |
| `env.tree_swamp` | 2 | 272 and 616 | 512 bark, 256 branches | 57.0 and 23.7 | 21.7 and 47.9 | 20.0 and 43.7 | SwampTree1, SwampTree2 |
| `env.tree_mistlands` | 4 | 2,344 (536 to 2,488) | 512 trunk, 128 leaves | 18.0 (17.5 to 23.0) | 40.7 | 27.6 | YggaShoot1, YggdrasilRoot |
| `env.tree_ashlands` | 3 | 2,010 (1,826 to 2,523) | 512 (one atlas) | 38.7 (14.4 to 53.8) | 28.8 | 23.4 | AshlandsTree1_0, AshlandsTree6 |
| `env.tree_deepnorth` | 4 | 2,071 (2,044 to 2,428) | 256 | 31.8 (21.8 to 39.1) | 33.3 (24.4 to 42.2) | 32.3 | SnowFirTree, Pinetree_Snow |
| `env.tree_small` | 6 | 543 (46 to 596) | 64 to 512 | 50.0 (40.1 to 52.5) | 3.9 (3.5 to 10.9) | 3.8 | Beech_small1, FirTree_small, YggaShoot_small1, SnowFirTree_small |
| `env.log_stump` | 42 | 64 (23 to 576) | 256 (64 to 512) | 31.6 (22.1 to 44.4) | 7.0 (1.2 to 48.3) | 2.7 | Beech_Stub, beech_log, beech_log_half, stubbe, FirTree_oldLog, OakStub |
| `env.bush` | 10 | 158 (48 to 649) | 64 (64 to 512) | 33.2 (29.7 to 40.3) | 3.6 (2.9 to 12.7) | 1.8 | Bush01, shrub_2, Bush02_en, AshlandsBush1 |
| `env.grass` | 16 | 36 (4 to 1,528) | 64 (32 to 128) | 74 (41 to 112) | 1.4 (0.9 to 2.6) | - | instanced_meadows_grass, instanced_forest_groundcover, instanced_heathgrass, instanced_swamp_grass, instanced_ormbunke, instanced_vass |
| `env.flower` | 3 | 135 (4 to 136) | 32 to 128 | 81 (49 to 91) | 1.1 | 1.0 | Pickable_Dandelion, Pickable_Thistle, instanced_heathflowers |
| `env.pickable` | 20 | 299 (45 to 4,584) | 64 (32 to 512) | 41.3 (30.4 to 54.9) | 1.0 (0.4 to 3.9) | 0.9 | Pickable_Flint, Pickable_Stone, Pickable_Mushroom, RaspberryBush, Pickable_Branch, Pickable_SeedCarrot |

Reference paths: `world/Props/Beech/Beech1.prefab`, `world/Props/oak/Oak1.prefab`, `world/Props/Birch/Birch1.prefab`,
`world/Props/Birch/Birch1_aut.prefab`, `world/Props/FirTree/FirTree.prefab`, `world/Props/PineTree/Pinetree_01.prefab`,
`world/Props/SwampTree/SwampTree1.prefab`, `world/Props/SwampTree/SwampTree2.prefab`,
`world/Props/Shoots/YggaShoot1.prefab`, `world/Props/Mistlands/YggdrasilRoot.prefab`,
`world/Props/Ashlands/Trees/AshlandsTree1_0.prefab`, `world/Props/Ashlands/Trees/AshlandsTree6.prefab`,
`world/Props/DeepNorthEnv/Trees/SnowFirTree.prefab`, `world/Props/PineTree/Pinetree_Snow.prefab`,
`world/Props/Beech/Beech_small1.prefab`, `world/Props/FirTree/FirTree_small.prefab`,
`world/Props/Shoots/YggaShoot_small1.prefab`, `world/Props/DeepNorthEnv/Trees/SnowFirTree_small.prefab`,
`world/Props/Beech/Beech_Stub.prefab`, `world/Props/Beech/logs/beech_log.prefab`,
`world/Props/Beech/logs/beech_log_half.prefab`, `world/Props/stubbe/stubbe.prefab`, `world/Props/FirTree/FirTree_oldLog.prefab`,
`world/Props/oak/OakStub.prefab`, `world/Props/Bush01/Bush01.prefab`, `world/Props/Shrub02/shrub_2.prefab`,
`world/Props/Bush01/Bush02_en.prefab`, `world/Props/Ashlands/Bushes/AshlandsBush1.prefab`, the clutter prefabs in
`world/Props/ground_clutter/`, and the pickables in `GameElements/Items/pickables/` (berry bushes in
`world/Props/Bush01/`, `world/Props/CloudberryBush/`, `world/Props/DeepNorthEnv/Lingon/`).

What these say:

- **A whole tree is a few hundred triangles.** Beech1 is 432 triangles for a 30 m tree, Oak1 578, Birch1 344. The
  conifers, shoots and charred trees spend 2,000 to 2,500. There is no detailed trunk and no individual leaves.
- **Leaves are a few huge cards.** Beech1's canopy is 100 cards averaging 5.8 m square (200 triangles over 3,360 m² of
  card), Oak1 125 cards of 6.8 m, Birch 88 to 92 cards of 4.7 to 5.2 m, SwampTree2 16 branch cards of 10 m. Each card
  carries a whole painted branch (below). Only the Deep North snow firs (1,128 cards of 1.6 m) and the Mistlands shoots
  (648 cards of 2.7 m) use many small cards.
- **Texel density is low and even.** 24 to 44 px per metre on leaves and bark alike: a 256 px leaf card over 5.8 m is
  44 px per metre; a 512 px bark atlas wrapped on a 30 m trunk is 20. Resolution is never spent on the trunk.
- **Two or three LODs, faded.** Every big tree has a LODGroup with fade mode 1 and `LodFadeInOut`: Beech1 432 / 114 /
  22 triangles, switching at 79, 143 and culled at 238 m (default LOD bias 2, 65 degree field of view, scale 1);
  Birch 344 / 98 to 78 and 238 m; Oak1 keeps one mesh to 280 m; Pinetree_01 2,071 / 461 to 38 and 191 m; conifers
  and snow firs one LOD to 175 m. No billboards or impostors anywhere (`m_LastLODIsBillboard` is 0 on every tree): the
  last LOD is a simplified mesh of the same kind of cards (22 to 578 triangles).

## How a tree is built

Seen in `render_env_tree_meadows.png` (Beech1, Oak1, Birch1 with their cards in wire), `render_env_tree_blackforest.png`,
`render_env_tree_swamp.png`, `render_env_tree_mistlands.png`, `render_env_tree_ashlands.png`,
`render_env_tree_deepnorth.png` and the texture sheets.

| Tree | Height at 1 (placed scale) | Triangles by LOD (to m) | Leaf or branch cards | Card size | Leaf texture | Bark texture | px/m leaves / bark |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Beech1 | 30.5 m (0.8-1.5) | 432 / 114 / 22 (79 / 143 / 238) | 100 | 5.8 m | `beech_leaf.png` 256 | `beech_bark.png` 512 | 44 / 20 |
| Oak1 | 25.5 m, 36 m wide (0.8-1.0) | 578 / 578 (87 / 280) | 125 | 6.8 m | `oak_leaf.png` 256 | `oak_bark.png` 512 | 37 / 31 |
| Birch1 | 25.2 m (0.5-1.0) | 344 / 98 (78 / 238) | 88 | 4.7 m | `birch_leaf.png` 128 | `birch_bark.png` 256 | 27 / 19 |
| FirTree | 10.9 m (1.5-3) | 596 (175) | branch cards on one atlas | 0.8 m edge | `Pine_tree_texture_d.png` 512 | same atlas | 96 |
| Pinetree_01 | 24.4 m (1-2.5) | 2,071 / 461 (38 / 191) | frond cards on one atlas | 0.7 m edge | `PineTree_01.png` 256 | same atlas | 39 |
| SwampTree1 | 21.7 m (0.7-1.3) | 272 (374) | 12 | 3.6 m | `deadbranch.png` 256 | `olivetree_trunk01.png` 512 | 72 / 27 |
| SwampTree2 | 47.9 m (1.5-2) | 616 / 148 (50 / 451) | 16 | 10.3 m | `deadbranch.png` 256 | same, tiled 2 | 25 / 20 |
| YggaShoot1 | 31.9 m (0.8-1.2) | 2,248 / 739 (99 / 178) | 648 | 2.7 m | `ShootLeaf_d.png` 128 | `ShootTrunk_d.png` 512 | 18 / 17 |
| AshlandsTree1_0 | 23.1 m (0.7-1.4) | 2,010 / 428 (57 / far) | branch silhouettes on one atlas | 0.4 m edge | `AshlandsTrees_d.png` 512 | same atlas | 54 |
| SnowFirTree | 42.2 m (0.5-1.0) | 2,428 (175) | 1,128 | 1.6 m | `Pine_tree_snow_d.png` 256 | same atlas | 22 / 14 |

Parts, as the renders show them:

- **Trunk.** A tapered tube of few sides with a few forked branches as thinner tubes (Beech1's bark is 232
  triangles, Oak1's 328 with a hollow painted in); the base flares and sinks 0.6 to 1 m below the pivot (Beech1's
  mesh starts 0.98 m under it, Pinetree_01's 0.62 m). Conifer trunks are one thin tube the full height.
- **Deciduous crown.** Big square cards at random angles through the crown, overlapping, each showing a whole branch
  with its leaves; from any side the crown reads as clumps, never as planes, because the cards cross each other and
  are cut out. The cards hang off the trunk's branches but do not follow them.
- **Conifer crown.** Tiers of drooping frond cards round the trunk: FirTree stacks 6 to 8 tiers from the ground up
  (a full cone), Pinetree_01 puts sparse tiers on the top half of a bare trunk, SnowFirTree hangs 1,128 small snowy
  cards in a slim, leaning spire.
- **Dead and charred trees** (SwampTree, AshlandsTree) have no leaves: the Ashlands trees are bark tubes with twig
  cards cut out of the same 512 px atlas (its top right holds black branch silhouettes), the Swamp trees a bark trunk
  with 12 to 16 big cards painted with a bare branch silhouette (`deadbranch.png`).
- **Colliders.** The trunk alone collides: a MeshCollider on the bark mesh (Beech1: 232 triangles, 5.7 by 9.9 m) or a
  CapsuleCollider (Pinetree_01: radius 0.31 m, 24.4 m tall) on layer Default. A second MeshCollider of the whole LOD0 on
  layer `viewblock` (25) stops creatures seeing through the crown. A `log_spawnp` empty marks where the felled log
  appears, and Beech1 carries a `leaf_particles` system driven by `GlobalWind` (falling leaves).

## How leaves and bark are painted

From `textures_env_tree_*.png`:

- **A leaf card is a painted branch.** `beech_leaf.png` and `oak_leaf.png` (256 px) are one small tree each: a brown
  twig skeleton from the bottom centre, with leaf clusters painted as small blobs a few texels across, darker green
  inside the clump (#464d2c), yellow-green on top and at the edges (#8b9b59). The alpha is a hard cut-out of the whole
  silhouette, full of holes. `birch_leaf.png` is the same at 128 px with rounder clumps and a pale trunk.
  `ShootLeaf_d.png` (Mistlands, 128 px) is a branch of distinct saturated leaves (#51730f to #679b10).
- **Fir and pine cards are fronds.** `PineTree_01.png` (256 px): the left third holds three dark green fronds
  (#392921 / #493b2e / #58523a overall), the right two thirds the bark strip. `Pine_tree_texture_d.png` (512 px): three
  small whole-fir silhouettes, a mossy green bark strip and a sprig.
- **Bark is an atlas with the log ends in it.** `beech_bark.png` (512 px) is grey bark with green moss patches and a
  pale ring end (the cut face, a small disc) in a corner; the log and half log textures (`beech_log.png`, 256 px) hold
  the bark on the left two thirds, two ring ends top right and a torn, splintered end bottom right. Pine, fir, oak and
  birch follow the same layout.
- **Moss on bark is painted and projected.** Beech and oak bark have moss painted in the texture, and their material
  adds `stonekit_moss.png` through `_MossTex` with `_MossBlend` 10; the Mistlands shoot trunk and stump add the same
  projected moss (`_MossBlend` 6) over an unmossed bark.
- **Snow.** SnowFirTree paints the snow into its cards (`Pine_tree_snow_d.png`: a yellow-green and a snow-blue fir
  silhouette side by side, and a pale snow patch). Pinetree_Snow and Pinetree_Snow_dead reuse Pinetree_01's own
  material: their snow comes from the shader's `_AddSnow` (the render without the shader shows them green).

Material values on the `Vegetation` shader (`Shaders/Vegetation.shader`):

| Material | Cull | Cutoff | Sway speed / distance / height | Ripple speed / distance | Push | Moss | Two-sided normals |
| --- | --- | --- | --- | --- | --- | --- | --- |
| beech_leaf | off | 0.5 | 10 / 25 / 35 | 150 / 2 | 0 | - | no |
| beech_bark | back | 0.5 | 10 / 25 / 35 | 100 / 0 | 0 | stonekit_moss, blend 10 | - |
| oak_leaf | off | 0.4 | 15 / 15 / 50 | 100 / 2.5 | 0 | - | no |
| birch_leaf (and _aut) | off | 0.5 | 15 / 20 / 35 | 150 / 3 | 0 | - | no |
| PineTree_01 | off | 0.5 | 15 / 120 / 100 | 150 / 1.5 | 0 | - | yes |
| Pine_tree (FirTree) | off | 0.5 | 15 / 60 / 15 | 150 / 1 | 0 | - | no |
| Shoot_Leaf_mat | off | 0.7 | 15 / 25 / 35 | 100 / 2.5 | 0 | - | no |
| swamptree1_branch | off | 0.5 | 15 / 3,000 / 6 | 100 / 0.01 | 0 | - | yes |
| Fir_Snow_Branch_Mat | off | 0.5 | 3 / 4 / 0 | 20.8 / 5.5 | 0 | alpha 0.54 | yes |
| beech_leaf_small | off | 0.5 | 25 / 5 / 5 | 200 / 0.5 | 0 | - | no |
| Bush01 | off | 0.52 | 30 / 3 / 3 | 150 / 0.5 | 2 | - | no |
| shrub | off | 0.53 | 30 / 2 / 1 | 200 / 3 | 3 | - | no |

What they do, read from the property names and the values across 60 materials (the formula itself is compiled into
the shader): the whole mesh sways with the wind, and by the names more the higher above the pivot a vertex is, with
`_Height` the height scale and `_SwayDistance` the amount in the shader's own units (25 and 35 on a beech, 120 and 100
on a pine, 3 and 3 on a bush).
Bark and leaves of one tree share the same sway speed, distance and height so trunk and crown move together; only the
leaves add `_RippleSpeed` / `_RippleDistance`, a fast flutter (bark ripple distance is 0). `_PushDistance` bends
bushes and grass away from a passing player (2 to 3 on bushes, 0 on trees). Leaves are drawn two-sided (`_Cull` 0)
and cut out at 0.4 to 0.7 (`_Cutoff`, most 0.5); bark is back-face culled or two-sided. `_CamCull` hides geometry very
close to the camera ("Near camera cull"), mostly off on trees. `_AddSnow` and `_AddRain` are on for every temperate
tree, off on the snow fir branches (snow painted), the Mistlands leaves and the Ashlands trees. `_Glossiness` 0 to
0.25.

## Felling: tree, stub, log, halves

Every big tree (Beech1, Birch, Oak1, FirTree, Pinetree, SwampTree1, YggaShoot, Ashlands, snow trees) is a chain of
four prefabs, each placed by the one before:

1. **The tree**: `TreeBase` (health 80 beech, birch, fir; 120 pine; 100 shoot; 200 oak, Ashlands and snow fir; minimum
   tool tier 0, 2 for birch, oak and swamp trees, 4 for Yggdrasil shoots). Hit: `vfx_SawDust` + `sfx_tree_hit`.
   Felled: a per-tree cut effect (`vfx_beech_cut`, `vfx_oak_cut`, `vfx_birch1_cut`, `vfx_firetreecut`,
   `vfx_swamptree_cut`, `vfx_yggashoot_cut`, `vfx_ashlandstreecut`, `vfx_pinetreecut_snow`) and `sfx_tree_fall`; drops
   resin, feathers and the tree's seed or cone (1 to 3). The cut effect throws the tree's own leaf texture as mesh
   particles (`vfx_beech_cut/leafs` draws `beech_leaf.png`), twig meshes and a dust puff, so a new tree needs its own
   copy of a cut effect with its leaf material swapped in.
2. **The stub** stays where the tree stood: `Destructible` of type Tree (health 80 to 160), `DropOnDestroyed`, a
   low-sided stump of 23 to 78 triangles with the ring end painted on top (Beech_Stub 1.8 m wide, 1.7 m tall with
   1 m buried; OakStub 4.2 m wide).
3. **The log** falls from `log_spawnp`: `TreeLog` (health 60, 90 Ashlands, 100 shoot, 160 oak), a `Rigidbody`
   (100 kg on beech_log), `CapsuleCollider`, `Floating`, `ImpactEffect`, `ZSyncTransform`. It is a low-sided prism
   of 64 triangles with pointed ends painted with the ring texture (beech_log 16.8 m by 0.95 m; Oak_log 13.7 by 2.9 m; FirTree_log 7.7 by
   0.7 m). Destroyed, it spawns its half log at two `sublog` points and plays `vfx_beechlog_destroyed` /
   `vfx_firlogdestroyed` + `sfx_wood_break`.
4. **The half log**: the same 64-triangle prism at half length (beech_log_half 7.6 m), `TreeLog` with no sub log,
   dropping 10 to 25 of the tree's wood (Wood, FineWood, RoundLog, Frostwood, YggdrasilWood, Blackwood) and playing a
   `_half` destroy effect.

Old fallen logs (`FirTree_oldLog`, 64 triangles, 7.1 m, `StaticRock` with moss), the big root stumps (`stubbe`, 260
triangles, 7.2 by 4.9 m, roots spread flat on the ground) and SwampTree2's 48 m fallen trunk are placed directly as
scenery (`Destructible` Tree, health 40 to 100) or not destructible at all (SwampTree2, SwampTree2_log, YggdrasilRoot).

## Saplings, small trees and bushes

- **Saplings** (`env.tree_small`): `Destructible` of type Tree, health 20 to 80, break in one go with their own
  destroy effect (`vfx_beech_small1_destroy`) and `sfx_wood_break`. Beech_small1 is 46 triangles: 12 leaf cards of
  1.2 m on a 64 px leaf texture and a 22-triangle twig trunk, 3.8 m tall, placed at scale 1 to 2 up to 100 per zone
  twice over. FirTree_small is the FirTree's layout at half size on a 128 px atlas.
- **Bushes** (`env.bush`): `Destructible` of type Default, health 30 (Bush02_en and Ashlands bushes 80 to 100 as
  type Tree). Bush01 is 158 triangles: 48 cards of about 1.1 m on a 64 px leaf-clump texture (`Bush01_d.png`:
  pixel-art leaves #50643a / #67804e / #7c9d62) round a 62-triangle wooden core on a 64 px bark texture, 2.9 m wide,
  2.6 m tall. shrub_2 is 367 triangles of long-leaved fronds (64 px). Colliders: a thin capsule (radius 0.2 m) on
  layer `Default_small` and a `viewblock` sphere of 0.75 m. Hit `vfx_bush_leaf_puff` + `sfx_bush_hit`, destroyed
  `vfx_bush_destroyed` (leaf particles on `Effects/textures/leaf_low.png`).
- Biome variants swap the leaf texture, tint and moss: Bush01_heath (yellow-green `Bush01_heath_d.png`, tint 0.80),
  Bush01_deepnorth (`pillar_snow_d2.png` as moss, alpha 1, and almost no sway: speed 2, distance 0.5).

## Grass and ground cover

The clutter system draws grass, ferns, reeds, water lilies, pebbles and small flowers with `InstanceRenderer`: no
GameObject per blade, one mesh and one material drawn in batches over 10 m patches out to 45 m from the camera, no
shadows. Each clutter prefab (`world/Props/ground_clutter/instanced_*.prefab`) holds only the mesh, the material, a
scale and two distances.

| Clutter | Biome, instances a patch | Mesh | Scale x, y, z | Texture | Sway speed / distance / height, push | Colour |
| --- | --- | --- | --- | --- | --- | --- |
| instanced_meadows_grass | Meadows, 200 (scale 1-2.3) | 36 triangles, crossed blade cards | 1.5, 2, 1.5 | `grass_meadows.png` 128 px, white blades | 60 / 2.3 / 0.5, 2 | `_TerrainColorTex` `grass_terrain_color.png` (mean #597742) |
| instanced_meadows_grass_short | Meadows, 250 | 36 | 1.2 | `grass_meadows_short.png` 64, white | 60 / 1.0 / 0.5, 0.5 | same terrain colour |
| instanced_forest_groundcover | Black Forest, 50 | 36 | 2, 2.2, 2 | `forest_groundcover.png` 32 px pixel-art sprigs, tiled 2 by 1 | 50 / 0.3 / 0.2, 0.5 | painted (#46542b) |
| instanced_forest_groundcover_brown | Black Forest, 80 | 36 | 1.5 | `forest_groundcover_brown.png` 32 | 40 / 0.3 / 0.2, 0.5 | painted (#5c5131) |
| instanced_swamp_grass | Swamp, 150 | 36 | 1.5, 2, 1.5 | `grass_toon1_yellow.png` 64 | 60 / 0.5 / 0.5, 1 | painted (#676e44) |
| instanced_heathgrass | Plains, 200 | 36 | 1.3, 3.5, 1.3 | `grass_heath.png` 64 by 128 | 60 / 2.5 / 0.5, 1.5 | painted (#998f5a) |
| instanced_heathflowers | Plains, 100 | 4 (one crossed pair) | 0.5, 1.1, 0.5 | `grass_heath_redflower.png` 64 by 128 | 40 / 1.5 / 2, 1.5 | painted red (#bc5c4e) on olive stems |
| instanced_ormbunke (fern) | Meadows 30 in forest, Black Forest 10 | 144 | 1 | `autumn_ormbunke_green.png` 64 | 60 / 0.8 / 1, 1 | painted (#57783f) |
| instanced_vass (reeds) | most biomes by water, 30 | 1,528 | 1 | `vass_texture01.png` 128 | 40 / 2 / 2, 1.45 | painted reed green and brown bulrush heads |
| instanced_waterlilies | Meadows, Black Forest, 40 | 32 | 1 (placed 0.4-0.6) | `waterlilies.png` 32 | 20 / 3 / 1, 11.2 | painted (#94b661) |
| instanced_mistlands_grass_short | Mistlands, 400 + 400 | 36 | 1 | `mistlands_moss.png` 64, white | 60 / 0.6 / 0.5, 0.5 | material colour #fff08a |
| instanced_ashlands_grass_short / _long | Ashlands, 50 + 50 | 36 | 1, 1.5 (3 long), 1 | `mistlands_moss.png` 64, white | 60 / 2 / 0.5, 1.2 | material colour #633b52 |
| instanced_forest_groundcover_snow | Deep North, 30 + 50 | 36 | 2, 2.2, 2 | `forest_groundcover_snow.png` 64 | 50 / 0.3 / 0.2, 0.5 | painted white-grey twigs (#b6bdbe) |
| instanced_small_rock1 | every biome, 15 | 48 | 0.3, 0.3, 0.15 | `rock_low.png` 32 | - (Standard shader) | tint #cdbda9 |

How grass reads (`render_env_grass.png`): thin, sparse, vertical blades on crossed cards 0.3 to 1 m tall (before a
placement scale of up to 2.3), never a dense turf. Meadows grass is drawn pure white in its texture and takes its
colour from `_TerrainColorTex`, a 1,024 px map of soft green, olive, brown and light-green blotches repeated every
100 m (`_TerrainColorScale` 0.01), so the grass colour drifts across the land in patches tens of metres wide; the
Mistlands and Ashlands grasses are white blades tinted by `_Color`. The other grasses are painted in their own colour.
Every grass material fades out between `_FadeDistanceMin` 20 m and `_FadeDistanceMax` 35 m (ferns 3.8 to 40 m, reeds
35 to 40 m); cut-out at 0.46 (blades) or 1.0 (painted sprigs, where only fully opaque texels draw).

Placement (`ClutterSystem.m_clutter`): each entry sets its biome, an amount per 10 m patch (`m_amount`, times the
system's 1.5), a scale range, the steepest slope (18 degrees for ferns, 25 for grass, 30 for heath, 60 for Deep North
brush), an altitude band (grass from 0.3 to 1 m above the sea; reeds -1 to -0.1 m and water lilies -1.5 to -0.2 m,
so both stand in shallow water), `m_onUncleared` / `m_onCleared` (grass only on untouched ground; the pebbles only on
cleared ground), whether it follows the ground's tilt (grass yes, flowers and reeds no), a forest-only flag and a
fractal noise (`m_fractalScale` 5 on meadows grass, 2 to 5 elsewhere) that gathers it into patches.

## Flowers

Pickable flowers are tiny: `Pickable_Dandelion` is 136 triangles on a 32 px texture (a round yellow head #d9cd15, a
serrated green leaf strip, a stem strip), 0.5 m tall; `Pickable_Thistle` 135 triangles on a 64 px texture (olive leaves,
two purple heads), 1.5 m tall. Plains flowers are clutter (`instanced_heathflowers`, one crossed pair of cards with
red petals). Wild seed plants (`Pickable_SeedCarrot`, `_SeedTurnip`, `_SeedKale`) are 180 to 4,553 triangle clusters
of cards on 64 px atlases with a white or yellow flower umbel (`CarrotFlower_d.png`, `TurnipFlower_d.png`); carrot
and turnip use two-sided normals and a cut-out of 0.33.

## Pickables

A pickable is a prefab with `Pickable`, `ZNetView`, `StaticPhysics` and a small collider; picking plays
`sfx_pickable_pick` + `vfx_pickable_pick` and either hides one child or removes the object:

| Pickable | Triangles, texture | Item and amount | Hidden when picked | Respawn |
| --- | --- | --- | --- | --- |
| Pickable_Flint | 48, 64 px | Flint 1 | `model` | 240 min |
| Pickable_Stone | 48, 32 px (StaticRock) | Stone 1 | whole object | never |
| Pickable_Branch | 406, 256 px | Wood 1 | `model` | 240 min |
| Pickable_Mushroom | 45, 32 px | Mushroom 1 | `visual` | 240 min |
| Pickable_Dandelion / Thistle | 136 / 135, 32 / 64 px | 1 | `visual` | 240 min |
| RaspberryBush / BlueberryBush | 1,222 / 508, 64 + 32 px | berry 1 | `model/Berrys` (the berries only) | 300 min |
| CloudberryBush / LingonberryBush | 549 / 2,672, 32 px | berry 1 | `Berrys` / `Berries` | 300 min |
| Pickable_SeedCarrot / Turnip / Kale | 180 / 180 / 4,553, 64 px | seeds 3 | whole object | never |
| Mistlands mushrooms (Magecap, JotunPuffs) | 1,203 / 4,584, 64 px | 1 | whole object | never |

- **Berry bushes are a bush plus berries.** RaspberryBush is Bush01 (158 triangles, same 64 px leaves) with 7 berry
  spheres (`Berrys/Sphere` ... 1,064 triangles) on a 32 px texture of two flat colour blocks (red and blue); picking
  hides `Berrys` and leaves the bush. The berries switch off at LOD1 (1,222 to 158 triangles at 22 m).
- **Small pickables are sculpted but tiny**: a flint 0.65 m, a stone 0.5 m, a mushroom 0.37 m, 45 to 48 triangles on
  32 to 64 px textures. They draw at LOD0 only and vanish at 16 to 36 m.
- Most pickables use Unity's plain Standard shader (no moss, no snow); Pickable_Stone uses `StaticRock`, the
  plants `Vegetation` or `Grass`.

## Building a new tree, bush, grass or pickable

1. Choose the category and the nearest reference; copy its height at scale 1, its triangle count, texture sizes and
   placement scale.
2. **Tree:** a low-sided trunk tube with 3 to 6 branch tubes (150 to 350 triangles), then 80 to 130 crossed cards of
   4.5 to 7 m (deciduous) or tiers of frond cards (conifer), each card a painted whole branch with a hard cut-out.
   Leaves 256 px, bark 512 px atlas with the ring end and splinter end in it, 25 to 45 px per metre. Add two LODs
   (a quarter and a twentieth of the triangles) at screen heights of about 0.6, 0.33 and 0.2 with fade mode 1 and
   `LodFadeInOut`.
3. Paint leaf clumps as blobs a few texels wide, dark inside, yellow-green on top and at the edges; bark in two or
   three tones with painted moss patches; keep every tone inside the biome's range (`../biomes.md`).
4. Materials on the `Vegetation` shader (borrowed at runtime): leaves `_Cull` 0, `_Cutoff` 0.5, bark and leaves the
   same `_SwaySpeed` / `_SwayDistance` / `_Height` (copy the reference), leaves `_RippleSpeed` 100 to 150 and
   `_RippleDistance` 2 to 3, `_AddSnow` 1 unless the snow is painted.
5. Colliders: the trunk (capsule or the bark mesh) on Default, the whole LOD0 on `viewblock`; a `log_spawnp` empty.
6. Felling: `TreeBase` with a stub, a 64-triangle log prism with 2 `sublog` points, a half log that drops wood,
   and a cut effect copied from `vfx_beech_cut` with the new leaf texture. Hit `vfx_SawDust` + `sfx_tree_hit`, fall
   `sfx_tree_fall`, logs `sfx_wood_break`.
7. **Bush:** 40 to 100 cards of about 1 m on a 64 px clump texture round a small wooden core, `_PushDistance` 2,
   `Destructible` health 30 with `vfx_bush_leaf_puff` / `vfx_bush_destroyed`.
8. **Grass:** a 36-triangle crossed-card clump 1 to 1.5 m across on a 32 to 128 px texture, the Grass shader with
   sway 40 to 60, push 0.5 to 2, fade 20 to 35 m; white blades plus the terrain colour map if it should follow the
   Meadows ground, painted otherwise; one `InstanceRenderer` prefab and a `ClutterSystem` entry.
9. **Pickable:** under 200 triangles, 32 to 64 px, the part that is picked as its own child named in
   `m_hideWhenPicked`, respawn 240 to 300 minutes.

Do not: model individual leaves or needles; give a tree more than one texture per part or more than 512 px; add a
billboard LOD (the game has none); paint a white grass texture green if it is meant to take the terrain colour;
make a pickable larger than its item on the ground reads (flint 0.65 m, mushroom 0.37 m).
