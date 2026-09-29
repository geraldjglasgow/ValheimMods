"""Which of the game's textures stand for each biome in the palette, grouped by role: ground cover, rock, wood and
trees, foliage, creatures, and what its people build with. Chosen from the biome's own props, creatures and
locations (their folders and names in the export); a sample is a paint_families sample dict."""
from paint_families import s

R = "world/Props/"
C = "Characters/"
G = "world/Props/ground_clutter/models/textures/"
P = "GameElements/Pieces/_res/"
NATURE = "3rd party/A_piece_of_nature/Textures/"
MALBERS = "3rd party/Malbers Animations/Animals Packs/01 Forest Pack/"

BIOMES = {
    "meadows": {
        "ground": [s(G + "grass_terrain_color.png")],      # the grass texture is white, coloured by this map
        "rock": [s("GameElements/Items/_res/stone/rock_256.png"), s(NATURE + "stonemoss.png")],
        "wood": [s(R + "Beech/materials/beech_bark.png"), s(R + "oak/materials/oak_bark.png")],
        "foliage": [s(R + "Beech/materials/beech_leaf.png"), s(R + "oak/materials/oak_leaf.png"),
                    s(R + "Bush01/textures/Bush01_d.png")],
        "creatures": [s(MALBERS + "Boar/Textures/Boar_valheim_d.png"), s(MALBERS + "Deer/Textures/Deer Pixel.png"),
                      s(C + "Neck/material/neck_d.png")]},
    "black_forest": {
        "ground": [s(G + "forest_groundcover.png"), s(G + "forest_groundcover_brown.png")],
        "rock": [s("world/terrain/old/gouacherock_big.png", material=R + "Rocks/materials/highrock.mat"),
                 s(R + "RuneStones/model/runestone_d.png")],
        "wood": [s(R + "PineTree/Textures/PineTree_log_d.png"), s(R + "Birch/materials/birch_bark.png")],
        "foliage": [s(R + "PineTree/Textures/PineTree_01.png", crop=(0.0, 0.0, 0.5, 1.0)),
                    s(NATURE + "Pine_tree_texture_small.png")],
        "creatures": [s(C + "GreyDwarf/Materials/greydrawrf_diffuse.png"),
                      s(C + "Troll/model/material/troll_diffuse.png", material=C + "Troll/model/material/troll.mat"),
                      s(C + "Skeleton/model/Texture/Skeleton_d.tga")]},
    "swamp": {
        "ground": [s(G + "grass_toon1_yellow.png"), s(NATURE + "stonemoss_swamp.png")],
        "rock": [s("world/terrain/old/stone_256_d.jpg"), s("world/terrain/old/stone_sunken.jpg")],
        "wood": [s(R + "SwampTree/model/olivetree_trunk01.png", material=R + "SwampTree/model/swamptree1_bark.mat"),
                 s(R + "SwampTree/model/deadbranch.png")],
        "foliage": [s(R + "SwampPlant/model/swampplant2_d.png")],
        "creatures": [s(C + "Draugr/newmodel/Draugr_d.png"), s(C + "Blob/model/materials/blob_d.png"),
                      s(C + "Leech/material/swampfish_d.png"), s(C + "Abomination/model/Rotvalta_d.png"),
                      s(C + "Bonemass/material/Bonemass_D.png")]},
    "mountains": {
        "ground": [s(G + "forest_groundcover_snow.png")],
        "rock": [s(R + "Caverocks/model/curvedrock_d.png"), s(R + "Caverocks/materials/icewall_d.png")],
        "wood": [s(R + "DeepNorthEnv/Trees/model/Pine_tree_snow_d.png")],
        "foliage": [s(R + "DeepNorthEnv/Trees/model/Pine_tree_snow_small_d.png")],
        "creatures": [s(MALBERS + "Wolf/Textures/Wolf Pixel.png"), s(C + "Fenring/Model/Fenring_d.png"),
                      s(C + "Hatchling/Model/Hatchling_D.png"), s(C + "StoneGolem/Materials/Golem_d.png"),
                      s(C + "Ulv/Model/Ulv_d.png")]},
    "plains": {
        "ground": [s(G + "grass_heath.png"), s(G + "grass_heath_green.png")],
        "rock": [s(R + "HeathRockPillar/model/heathrock_d.png"), s(NATURE + "stonemoss_heath.png")],
        "wood": [s(R + "vegetation/models/acacia_trunk01.png")],
        "foliage": [s(R + "Shrub02/model/shrub_3_heath.png"), s(R + "Bush01/textures/Bush01_heath_d.png"),
                    s(P + "barley/materials/barley.png")],
        "creatures": [s(C + "Goblin/material/goblin_d.png"), s(C + "GoblinBrute/model/GoblinBrute_d.png"),
                      s(C + "Lox/Model/material/Halstein_d.png"), s(C + "Deathsquito/model/Deathsquito_d.png")],
        "building": [s("world/dungeon/goblicamp/GoblinVillage/model/GoblinVillage_d.png")]},
    "mistlands": {
        "ground": [s(R + "Dvergr/model/Creep/creep_d.png")],     # its grass is white, coloured by the terrain
        "rock": [s(R + "Mistlands/Cliff1/mistlands_cliff_d.png"), s(P + "Marble/material/marble_d.png")],
        "wood": [s(R + "Shoots/materials/ShootTrunk_d.png")],
        "foliage": [s(R + "Shoots/materials/ShootLeaf_d.png"),
                    s(R + "MistlandsHangingFoliage/model/MistlandsVegetation_d.png")],
        "creatures": [s(C + "Seeker/model/seeker_d.png"), s(C + "SeekerBrute/model/seekerBrute_d.png"),
                      s(C + "Gjall/model/Gjall_d.png"), s(C + "Tick/Model/material/Feasting_d.png"),
                      s(C + "Dverger/model/material/DvergrBody.png")],
        "building": [s(R + "Dvergr/model/Beams_Stake/materials/DvergrTownPieces_d.png")]},
    "ashlands": {
        "ground": [s("world/Props/Ashlands/Vegetation/Ashlandsvegetation_d.png")],
        "rock": [s(R + "Ashlands/Rocks/model/AshlandsRock_d.png"), s(P + "Grausten/model/Grausten_d.png")],
        "wood": [s(R + "Ashlands/Trees/AshlandsTrees_d.png")],
        "foliage": [s(R + "Vines_Ashlands/model/vineberrysapling_d.png")],
        "creatures": [s(C + "TheCharred/model/Charred_d.png"), s(C + "Asksvin/model/asksvin_d.png"),
                      s(C + "Morgen/model/Morgen_d.png"), s(C + "Volture/model/Volture_d.png")],
        "building": [s(P + "Ashlandsfortress/model/FortressWall1_d.png")]},
    "deep_north": {
        "rock": [s(R + "IceShelf/IceShelves_d.png"), s(R + "DeepNorth/BlackIce/model/blackice_d.png")],
        "wood": [s(R + "DeepNorthEnv/Trees/model/Pine_tree_plantable_trunk_d.png"),
                 s(R + "DeepNorthEnv/Trees/model/frostwood_d.png")],
        "foliage": [s(R + "DeepNorthEnv/Trees/model/Pine_tree_plantable_d.png"),
                    s(R + "DeepNorthEnv/Lingon/model/lingon_d.png")],
        "creatures": [s(C + "Jotnar/model/Jotnar_d.png"), s(C + "Barka/model/Barka_d.png"),
                      s(C + "seal/model/seal_d.png")],
        "building": [s(R + "Morkhalla/model/morkhallawall_d.png"), s(R + "Morkhalla/Statues/JotunStatues_d.png")]},
    "ocean": {
        "creatures": [s(C + "Serpent/model/SeaSerpent_d.png"), s(C + "BonemawSerpent/model/BonemawSerpent_d.png")],
        "rock": [s(C + "Leviathan/model/leviathan_d.png"), s(C + "Leviathan/model/barnacle_d.png")]},
}
