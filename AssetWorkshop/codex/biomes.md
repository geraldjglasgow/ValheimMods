# Biomes

What each of the game's nine biomes is made of, as numbers: the terrain texture it is painted with, the props the zone
system scatters over it and how many, the grass and ground cover, the textures its props are painted with (sampled
10, 50 and 90 % tones), the weather it cycles through and the light and fog of that weather. A new asset for a biome
uses that biome's palette and must read right under that biome's light: check it under the numbers below, not under a
neutral studio light.

Everything here comes from `measure/environment.py` (data in `data/environment.json`, keys `terrain`, `biomes`,
`environments`, `vegetation`, `clutter`). Per-material colour families are in `palette.md`; how rocks, trees, grass and
locations are built is in `models/environment.md`, `models/vegetation.md` and `models/locations.md`.

## How a biome is put together

- **Terrain.** One heightmap per 64 m zone, a vertex every 1 m (`Systems/_Zone.prefab`: `m_width` 64, `m_scale` 1),
  drawn by the `Heightmap` shader from a 16-slice texture array (`world/terrain/terrain_d_array.png`, 256 px a slice,
  normals in `terrain_n_array.png`). The biome is written into the mesh's vertex colours (`Heightmap.GetBiomeColor`:
  Swamp red, Mountain and Deep North green, Black Forest blue, Plains alpha, Ashlands red + alpha, Mistlands blue +
  alpha, Meadows none) and the shader picks and blends slices from it; steep ground turns to the cliff slice, and the
  snow slot (slice 6) is an orange placeholder the shader replaces with its own snow. Material values: `_UVScale` 0.5,
  `_Glossiness` 0.1, `_RockGloss` 0.7, `_SnowGloss` 1.0, tessellation 4 with 0.05 m displacement.
- **Vegetation list.** `Systems/_ZoneSystem.prefab` `m_vegetation` (and each `Systems/LocationLists/_LocationList_*`
  for Ashlands, Deep North and Mistlands): 149 enabled entries, each a prefab, the biomes it may go in, a random count
  of placement attempts per 64 m zone (`m_min`..`m_max`; below 1 it is a chance), a group size and radius, a scale
  range and a random tilt. The "per zone" figures below are that `m_max`.
- **Clutter.** `ClutterSystem` in `Systems/_GameMain.prefab` (plus the location lists): 21 instanced grass and ground
  cover entries, drawn in 10 m patches (`m_grassPatchSize`) out to 45 m from the camera (`m_distance`), `m_amount`
  instances per patch, with an overall `m_amountScale` of 1.5.
- **Locations.** `m_locations` in the same lists: 168 entries (`models/locations.md`).
- **Weather.** `EnvMan` in `Systems/_Environment.prefab` (and the location lists) gives each biome weighted
  environments; each environment (`EnvSetup`) holds sun, ambient and fog colours for night, morning, day and evening,
  fog densities, the light intensity by day and by night, wind and flags. A day is 1800 s (`m_dayLengthSec`); an
  environment lasts 666 s and blends over 10 s.

## Terrain slices

Sampled from `world/terrain/terrain_d_array.png` (tones are the mean colour around the 10th, 50th and 90th luminance
percentile). The slice-to-biome mapping is compiled into the `Heightmap` shader, which the export does not keep; the
"looks" column is what each slice shows, matched against the older named terrain textures in `world/terrain/old/`
(`grass.png` = 0, `forest_d.png` = 1, `dirt_d.png` = 2, `cleared_d.png` = 3, `gouacherock.png` = 5, `snow.png` = 6,
`sand.png` = 9). Contact sheet: `out/environment/terrain_slices.png`. The biome sections below name the slices whose
colour and subject match that biome's ground; that match is by eye, not read from the shader.

| Slice | Looks | 10 % | 50 % | 90 % |
| --- | --- | --- | --- | --- |
| 0 | green grass (Meadows) | #4c693c | #50703f | #567643 |
| 1 | moss-green patches on brown (forest floor) | #55512a | #606131 | #6e733a |
| 2 | olive dirt, alpha-masked | #5e572a | #675e2e | #706733 |
| 3 | brown dirt (cleared ground, paths) | #4a4127 | #584c2e | #655835 |
| 4 | grey mottled stone | #353533 | #484845 | #5e5e5c |
| 5 | grey faceted rock, height in alpha (cliff) | #42423f | #555552 | #696966 |
| 6 | orange placeholder: snow, drawn by the shader | - | - | - |
| 7 | near-black ash | #171b1d | #1b1f21 | #1f2325 |
| 8 | yellow-olive grass (Plains heath) | #7a7043 | #817849 | #887f4d |
| 9 | sand (beaches, sea floor) | #ba9166 | #bf9a70 | #c0a177 |
| 10 | dark brown leaf litter, alpha-masked (Black Forest floor) | #2c240e | #382f16 | #453a1d |
| 11 | yellow-green grass with flowers | #718739 | #7c923a | #83a046 |
| 12 | grey cobbles (paved) | #463c2e | #5c5a56 | #747473 |
| 13 | dark grey-green stone | #363a3a | #3a3d3e | #3d4040 |
| 14 | dark cracked rock | #161717 | #242525 | #373939 |
| 15 | black marble with white veins | #111111 | #393939 | #a7a7a7 |

The terrain is low in saturation and mid to dark in value everywhere: the brightest ground (sand, #bf9a70) is the
exception, and every grass slice sits between #4c693c and #83a046. An asset that stands on the ground should have its
base within one or two steps of the slice under it; a prop whose lower edge is much brighter or more saturated than
the ground floats.

The player's hoe and pickaxe repaint the ground with `TerrainModifier` paint types Dirt, Cultivate, Paved, Reset,
ClearVegetation and DeepSnow; locations flatten and paint it with the `world/LevelTerrain.prefab` helper (level radius
4 m square, smooth radius 5 m, power 3, paints "cleared").

## Weather and light

The main environment of each biome, day values first. Colours are the stored `EnvSetup` values as hex; intensity is
the directional light's. Sun colour and intensity light the asset; ambient fills the shadows; fog colour and density
decide how fast it fades into the distance. The scene's fog is exponential (`Scenes/main.unity`, `m_FogMode` 2):
fog covers half of an asset's colour at ln 2 / density, 69 m at 0.01, 35 m at 0.02, 231 m at 0.003, 17 m at 0.04.

| Environment (biome, weight) | Sun day | Intensity day / night | Ambient day / night | Fog day / night | Fog density day / night | Wind |
| --- | --- | --- | --- | --- | --- | --- |
| Clear (Meadows 5, Ocean 1) | #ffc57b | 1.7 / 1.0 | #7692b4 / #5b5e7c | #4d94be / #25262b | 0.003 / 0.01 | 0.1-0.6 |
| DeepForest Mist (Black Forest 2) | #ffdea3 | 1.5 / 1.2 | #4e8bae / #325c74 | #3d7ea3 / #1d2e38 | 0.01 / 0.02 | 0.1-0.6 |
| SwampRain (Swamp, only) | #efffc3 | 0.6 / 0.5 | #4f5648 / #4b5145 | #545454 / #173625 | 0.02 / 0.02 | 0.1-0.3 |
| Snow (Mountain 5), SnowStorm 1 | #f2cd8b | 1.2 / 0.4 | #7692b4 / #595c7c | #7b8da5 / #272727 | 0.004 / 0.004 | 0.1-0.6 |
| Heath clear (Plains 2) | #ffa163 | 1.5 / 1.0 | #aeaeae / #5b5e7c | #7580c7 / #212124 | 0.006 / 0.02 | 0.4-0.8 |
| Mistlands_clear (Mistlands 1.5) | #ffc294 | 1.2 / 0.5 | #55616a / #55616a | #37434c / #30516a | 0.02 / 0.04 | 0.05-0.2 |
| Ashlands_ashrain (Ashlands 1.5) | #84624e | 2.2 / 0.4 | #ab746d / #345557 | #802f21 / #2c4146 | 0.02 / 0.03 | 0.1-0.5 |
| Twilight_Clear (Deep North 1) | #a6c8da | 0.9 / 0.3 | #5c6180 / #34323a | #6f91b2 / #213b46 | 0.01 / 0.02 | 0.2-0.6 |
| Twilight_Snow (Deep North 1) | #f2cd8b | 0.5 / 0.3 | #707d8c / #4f526b | #535f6f / #141414 | 0.01 / 0.01 | 0.3-0.6 |
| Misty (Meadows, Black Forest, Plains, Ocean) | #f8d69b | 1.0 / 1.0 | #839ebf / #5b5c67 | #a5b3c5 / #28292f | 0.02 / 0.15 | 0.1-0.3 |
| Rain, ThunderStorm (most biomes, 0.1-0.2) | #ececec | 0.5 / 0.77 | #969696 / #626262 | #545454 / #2d2e35 | 0.03 / 0.03 | 0.5-1.0 |

Morning and evening sun: Clear #ffab6b and #ff3c00, DeepForest Mist #ffb881 and #ff3f04, Mistlands #7d4141 and
#90494b, Ashlands #b07e48 and #684dc0, Twilight_Clear #f86d5d and #ff1818. The sun stands at 45 degrees in most
environments, 60 in the Mistlands, 35 in the Ashlands and 20 in Twilight_Snow. Every environment shares the ambient
occlusion colour #01003a (a deep blue), so contact shadows in the game go blue-black, never brown.

What that means for an asset: the game's day light is warm (orange-yellow sun) against a cool blue sky fill in the
Meadows, Black Forest, Mountains and Plains; so textures are painted warm in the lit tones and the shadows are left to
the blue ambient. In the Swamp the sun is pale green-yellow at 0.6 and the fill is olive grey: a Swamp asset painted
at Meadows brightness glows. In the Mistlands everything sits in dark blue-grey fog (#37434c at 0.02): contrast within
a Mistlands asset has to be strong to survive 40 m. The Ashlands light is strong (2.2) and brown-red with red fog: its
props are near-black so the light does the colouring. Deep North light is weak (0.5 to 0.9) and cold blue.

Every environment's full values (morning and evening fog, sun fog, AO strength) are in `data/environment.json`
`environments`.

## Meadows

- **Weather:** Clear 5, Rain 0.2, Misty 0.2, ThunderStorm 0.2, LightRain 0.2. Nearly always clear, warm and bright
  (sun 1.7), thin fog (0.003).
- **Ground:** slice 0 (#50703f) with slice 11 (#7c923a) and dirt. Grass: `instanced_meadows_grass` (200 a patch, white
  blades tinted by `grass_terrain_color.png`, a 1024 px map of soft green, olive and brown blotches, mean #597742) and
  `instanced_meadows_grass_short` (250), ferns in forest, reeds by water, water lilies.
- **Placed (per zone):** Beech_small1/2 100 + 100 + 80 + 80, Bush01 80, Rock_4 40 (scale 1-2), Beech1 40, Pickable_Flint
  30, Pickable_Stone 30, Rock_3 15 (scale 2-4), Pickable_Branch 15, Pickable_Dandelion 10, Birch1 5, Oak1 up to 1,
  old fir logs, stumps (`stubbe`). Locations: Eikthyr's altar, start temple, stone circles, abandoned wood houses,
  farms and villages, rune stones, dolmens.
- **Materials and tones (share of the placed surface, weighted by count per zone):** beech_leaf_small 31 % (#3e5126 / #54663b / #718b56), meadows grass
  40 % (white, terrain-tinted), Bush01 7 % (#50643a / #67804e / #7c9d62), Rocks_4 (#848673 / #868773 / #8c8e7b),
  beech_leaf (#464d2c / #5c7136 / #8b9b59), fern (#4c6a32 / #57783f / #66864a).
- **Mood:** fresh yellow-green on mid green, soft grey rocks with moss on top, warm light, long views.

## Black Forest

- **Weather:** DeepForest Mist 2, Rain 0.1, Misty 0.1, ThunderStorm 0.1. Sun 1.5 but a blue fog of 0.01 (half gone at
  69 m) and a blue ambient (#4e8bae).
- **Ground:** dark leaf litter (slice 10, #382f16) and forest floor (slice 1, #606131). Ground cover:
  `instanced_forest_groundcover_brown` (80 a patch) and `instanced_forest_groundcover` (50), 32 px pixel-art sprigs
  (#5c5131, #46542b), ferns.
- **Placed (per zone):** shrub_2 100, Pinetree_01 60 (scale 1-2.5), FirTree_small 60 + 30, FirTree 40 + 5 (scale 2-2.5),
  MineRock_Tin 20, Rock_4 20 (scale 1-3), Rock_3 15 (scale 3-6: 6 to 12 m boulders), stumps 15, branches, rock4_forest
  up to 2, copper deposits up to 1, blueberry bushes. Locations: 300 Greydwarf nests, burial chambers, troll caves,
  tower and stone house ruins, the trader.
- **Materials and tones:** shrub 17 % (#5b5b48 / #686654 / #677d4d), Pine_tree_small 15 % (#2b3f1e / #43522d /
  #5e6942), forest ground cover 13 % (#584e31 / #5c5131 / #635136), PineTree_01 10 % (#392921 / #493b2e / #58523a),
  Pine_tree 7 % (#43472f / #505a3a / #5d7643). Rocks use the terrain's own `gouacherock_big.png` with `forest_d.png`
  as their moss.
- **Mood:** dark olive and brown under tall conifers, blue haze between trunks, grey boulders greened on top.

## Swamp

- **Weather:** SwampRain only: always wet and dark (sun 0.6, pale green-yellow #efffc3), ambient olive grey #4f5648,
  grey fog 0.02, green-black fog at night (#173625), little wind (0.1-0.3).
- **Ground:** dark mud; swamp grass `instanced_swamp_grass` (150 a patch, olive #676e44), yellow ferns, reeds.
- **Placed (per zone):** FirTree_small_dead 60, SwampTree1 40, SwampTree2 20 (scale 1.5-2, 70 to 95 m tall), Rock_4 20,
  shrub_2_heath 20, flies 20, swamp mist 10, old logs 6, mud piles (iron) 5, stumps 4, SwampTree2_log 3, StatueEvil 2,
  thistle 2. Locations: 700 infested trees, 200 grave sites, sunken crypts, huts, wells, rune stones.
- **Materials and tones:** swamp grass 40 % (#5d633e / #676e44 / #767351), dead pine 16 % (#3e3c24 / #535234 /
  #6b6a43), reeds 8 % (#5c3410 / #6b4015 / #734518), swamp tree branch cards (#786757). Moss texture on swamp props:
  `stonemoss_swamp.png`.
- **Mood:** brown-olive and grey, bare branches, low light; nothing bright except the green night fog.

## Mountains

- **Weather:** Snow 5, SnowStorm 1. Snow falls; sun 1.2 warm (#f2cd8b) on a blue ambient; SnowStorm drops to 0.7 with
  pale grey fog (#bababa) at 0.05. `m_isFreezing` is set.
- **Ground:** the snow slot (drawn white by the shader) and grey cliff rock (slice 5). Clutter: pebbles and reeds only.
- **Placed (per zone):** Rock_4 40, FirTree_small 30, FirTree 20 (scale 1.5-3), Rock_3 15, MineRock_Obsidian 15, rock1
  and rock2_mountain 8 each (24 to 29 m rocks), rock3_mountain 2, one silver vein. Locations: drake nests, dragon
  altar, log cabins, stone tower ruins, graves, wells, caves.
- **Materials:** Rocks_4 22 %, Pine_tree_small 16 %, reeds 16 %, Pine_tree 11 %, rock1_mountain
  (`gouacherock_big.png` tiled 10 times, #434341 / #575754 / #71706b), obsidian (#040404 / #393837 / #5a5a5a). The
  snow on Mountain rocks and trees is not in their textures: their materials have `_AddSnow` on and the shader lays it
  (no game script sets a snow amount, so the rule lives in the compiled shader).
- **Mood:** grey rock and dark conifers under white, warm sun, blue shade.

## Plains

- **Weather:** Heath clear 2, Misty 0.4, LightRain 0.4. Orange sun (#ffa163) at 1.5, a grey ambient (#aeaeae), windy
  (0.4-0.8).
- **Ground:** yellow-olive heath (slice 8, #817849). Grass `instanced_heathgrass` (200 a patch, #998f5a, 3.5 times
  taller than wide), red-flowered heath (100 a patch, red #bc5c4e).
- **Placed (per zone):** Bush01_heath 80, Rock_4_plains 30, Birch1_aut 30, shrub_2_heath 20, rock4_heath 10,
  Birch2_aut 10, Bush02_en 3, cloudberry bushes 3, rock2_heath 1, HeathRockPillar (44 m) one in ten zones. Locations:
  Fuling camps, stone henges, towers, tar pits, the Fuling king's altar.
- **Materials and tones:** heath grass 38 % (#7b7347 / #998f5a), heath flower 19 % (#7e3c34 / #bc5c4e / #a69b61),
  Bush01_heath 15 % (#6b7132 / #84913b / #9eb047), autumn birch leaves 7 % (#665d38 / #9b994e / #e5e67a, the brightest
  foliage in the game). Rock moss: `stonemoss_heath.png`.
- **Mood:** dry yellow and olive under an orange sun; the only biome where foliage goes near yellow-white.

## Ocean

- **Weather:** Clear 1, Rain, LightRain, Misty and ThunderStorm 0.1 each.
- **Placed:** rock4_coast groups of 3 in shallow water (depth 2-30 m), the Leviathan (1 in 100 zones). Clutter: reeds,
  pebbles. The coast rocks share the grey gouache rock texture.

## Mistlands

- **Weather:** Mistlands_clear 1.5, rain 0.1, thunder 0.1. Sun 1.2 at 60 degrees, but a dark blue-grey fog (#37434c)
  at 0.02 by day and 0.04 to 0.05 at night and morning, grey ambient (#55616a), almost no wind (0.05-0.2). The mist
  itself is a placed effect (`MistArea`). At 0.02 fog half of a colour is gone at 35 m.
- **Ground:** dark grey-green stone and black marble (slices 13 to 15). Clutter: `instanced_mistlands_grass_short`
  400 + 400 a patch (white moss blades tinted #fff08a), rock plants 40.
- **Placed (per zone):** cliff_mistlands2 50 (22 to 37 m spires), YggaShoot_small1 40 + 6, rock_mistlands1 20,
  cliff_mistlands1 8 (73 m), Yggdrasil roots 4, YggaShoot1-3 2 to 3 each, giant helmets, swords and ribs 1-2 each
  (rusted metal to mine). Locations: Dvergr towers, lighthouses, harbours, excavations, viaducts, statues, giant
  remains, the infested mines.
- **Materials and tones:** moss 79 % (white, tinted), mistlands_cliff (the grey gouache rock tinted to #414141 by its
  material colour 0.255), Shoot_Leaf (#4d4a19 / #51730f / #679b10: saturated yellow-green), rock plants
  (#4a5716 / #5a601d / #5c7523), rock_mistlands (#151616 / #222423 / #373938), Shoot_Trunk (#583a28 / #724f3a /
  #956b53), giant rust (orange-red with blue-green patina, `GiantRust_d.png`).
- **Mood:** near-black rock under grey-blue fog, lit by saturated yellow-green leaves and orange rust.

## Ashlands

- **Weather:** Ashlands_ashrain 1.5, CinderRain 0.2, misty 0.1, storm 0.05. The strongest light in the game (2.2) but
  brown (#84624e) at a 35-degree sun, a pink-brown ambient (#ab746d), dark red fog (#802f21) at 0.02. The storm turns
  the sun dark red (#7d322d).
- **Ground:** near-black ash (slice 7, #1b1f21) and dark cracked rock (slice 14). The shader's
  `_AshlandsVariationCol` is #697b8c. Grass: `instanced_ashlands_grass_short` and `_long` (50 each, white blades tinted
  #633b52 by the material colour 0.388 / 0.267 / 0.322).
- **Placed (per zone):** cliff_ashlands6 up to 196 at scale 0.2-0.9 (the scattered black rocks), charred skulls 40,
  ash stones 15, charred trees 8 to 10 each, bushes 10, branches, pots and grave stones, cliffs and arches 3 to 5.
  Locations: charred fortresses, ruins and towers, Morgen holes, the sulfur arch, lava leviathans.
- **Materials and tones:** AshlandsRock 56 % (#1b242f / #252d38 / #2e3742: a blue-black, not neutral), AshlandsTrees
  14 % (#000000 / #100f10 / #1f1e1f: black), ash as the rocks' moss (`AshOnRocks_d.png`), orange lava cracks
  (`Unstable_d.png`). Emissive orange-red is the only saturated colour.
- **Mood:** black rock and charred wood under red-brown light and red haze, with orange glow.

## Deep North

- **Weather:** Twilight_Snow 1, Twilight_Clear 1, Twilight_SnowStorm 0.5. Weak cold light: 0.9 on a clear day (sun
  #a6c8da, pale blue), 0.5 in snow, 0.4 in a storm, ambient #5c6180 to #707d8c, fog 0.01 by day. Freezing day and
  night.
- **Ground:** snow. Clutter: `instanced_forest_groundcover_snow` (30 + 50 a patch, white-grey twigs #b6bdbe).
- **Placed (per zone):** SnowFirTree_small 35 + 10, Pinetree_Snow_dead 30, SnowFirTree 2 25, ice sheets 20 + 13, pine
  trees 20 + 20, snow branches 15, shore ice 10 + 10, SnowFirTree 7 (42 m), Rock_4 at scale 4-6, Rock_3 at scale 2-8,
  frozen Greydwarfs and skeletons, stump huts, lingonberry bushes. Locations: north villages, lumber camps, frozen
  ships, memorial places, Mörk halls.
- **Materials and tones:** snow ground cover 24 % (#7d8282 / #b6bdbe / #d5d8d8), ice sheets 16 % (untextured
  Standard material, colour #d6f8ff; the ice rock's `ice2` is #aee4ff), snow fir branches 12 % (#6c524c / #767b59 /
  #b6c8c3: snow painted into the card), PineTree_01 and its dead variant, frozen bodies (#a5b2b7 / #afc4cb /
  #c0d2d7). Deep North variants of rocks, stumps, logs and bushes swap their
  moss texture for `pillar_snow_d2.png`, so snow lies on their tops.
- **Mood:** white and pale blue-grey under low, cold light; dark trunks and near-black water for contrast.

## Checking an asset against its biome

1. Render it under the biome's main environment above (sun colour and intensity, ambient colour, fog colour and density
   at 20, 40 and 80 m) beside the biome's references (`models/*.md`).
2. Its median tone should sit inside the 10 to 90 % tones of the textures it stands among (the biome's materials
   above); its brightest lit tone should not exceed the brightest of theirs.
3. Anything on the ground takes that biome's top cover the way the game's own props do: moss, snow or ash on top from
   the shader (`models/environment.md`), grass colour from the terrain tint (`models/vegetation.md`).
