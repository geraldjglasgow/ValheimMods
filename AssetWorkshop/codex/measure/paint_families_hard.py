"""Material families of hard things: wood, metal, bone, stone, crystal (see paint_families)."""
from paint_families_base import A, C, I, NATURE, P, PLANKS, R, W, s  # noqa: F401

FAMILIES = {
    "wood.planks": {
        "label": "sawn and split planks: floors, walls, chests, furniture, ships",
        "region": "wood",
        "samples": [
            s(PLANKS + "Planks5/textures/Planks5c_low.png"),
            s(PLANKS + "Planks1/textures/Planks1c.png"),
            s(R + "Chests/materials/woodchest_d.png"),
            s(P + "Bench/Model/Bench_d.png"),
            s(P + "Chair/Model/Chair_d.png"),
            s(P + "WorkBench/Model/WorkBench_d.png"),
            s(P + "Table/Model/Table_d.png"),
            s(P + "spiralStair/model/spiralstair_d.png"),
            s(P + "barrel/model/barrel_d.png"),
            s("GameElements/Cart/_res/CartNew/model/Cart_d.png"),
            s("GameElements/Ships/_res/karve/boat_d.png"),
            s("world/textures/wood.png"),
            s(P + "drawbridge/model/drawbridge_d.png"),
            s(P + "roof/model/wood_roof_d.png"),
        ]},
    "wood.dark": {
        "label": "dark-stained and charred wood: darkwood, ashwood, Deep North frostwood",
        "region": "wood",
        "samples": [
            s(P + "DarkWood/Beams/model/DarkWoodBeams_d.png"),
            s(P + "BlackWood/walls/BlackWood_d.png"),
            s(P + "BigGate/model/DarkWoodGate_d.png"),
            s(R + "DeepNorthEnv/Trees/model/frostwood_d.png"),
            s(P + "DarkWooodChair/model/DarkWoodChair_d.png"),
        ]},
    "wood.logs": {
        "label": "debarked logs and poles: log walls, beams, log furniture, firewood",
        "region": "wood",
        "samples": [
            s(P + "logwall/Pine_tree_log_wall.png"),
            s(P + "LogBench/model/LogBench_d.png"),
            s(P + "Drawbridge_log/model/drawbridge_log_d.png"),
            s(NATURE + "Pine_tree_log_texture.png"),
            s(R + "wood/woodpile_diffuse.png"),
            s(P + "stakewall/stakewall_d.png"),
        ]},
    "wood.bark": {
        "label": "bark of living and felled trees",
        "region": "wood",
        "samples": [
            s(R + "Beech/materials/beech_bark.png", material=R + "Beech/materials/beech_bark.mat"),
            s(R + "Birch/materials/birch_bark.png"),
            s(R + "oak/materials/oak_bark.png", material=R + "oak/materials/oak_bark.mat"),
            s(R + "PineTree/Textures/PineTree_log_d.png"),
            s(R + "SwampTree/model/olivetree_trunk01.png", material=R + "SwampTree/model/swamptree1_bark.mat"),
            s(R + "vegetation/models/acacia_trunk01.png"),
            s(R + "Shoots/materials/ShootTrunk_d.png"),
            s(R + "Ashlands/Trees/AshlandsTrees_d.png"),
        ]},
    "wood.fine": {
        "label": "fine wood: smooth, pale, close-grained; bows, shields, fine furniture",
        "region": "wood",
        "samples": [
            s(R + "wood/finewood_d.png"),
            s(P + "Table_oak/Table_oak_d.png"),
            s(P + "Runed_furniture/model/runed_furniture_d.png"),
            s(I + "shields/_res/shield_wood/shieldwood_d.png", mask="nonmetal"),
            s(W + "bow/FineWoodbow/Finewoodbow_d.png"),
        ]},
    "metal.iron": {
        "label": "iron: armour, weapons, bands, beams",
        "region": "metal",
        "samples": [
            s(A + "IronArmor/model/IronArmorChest_d.png", mask="metal"),
            s(I + "helmets/_res/Bronze/helmet_iron_d.png", mask="metal"),
            s(I + "shields/_res/shield_buckler_iron/model/iron_buckler_d.png", mask="metal"),
            s(W + "maceiron/model/ironmace_d.png", mask="metal"),
            s(W + "atgier/atgeir_iron_d.png", mask="metal"),
            s(W + "battleaxe/model/battleaxe_d.png", mask="metal"),
            s(P + "IronBeam/model/Ironbeam_d.png", mask="metal"),
            s(I + "shields/_res/NewShields/model/IronShields/Shield_Designs_d.png", mask="metal"),
        ]},
    "metal.bronze": {
        "label": "bronze",
        "region": "metal",
        "samples": [
            s(A + "Bronzearmor/BronzeArmor_Chest_meshes_chest_d.png", mask="metal"),
            s(I + "helmets/_res/Bronze/helmet_bronze_d.png", mask="metal"),
            s(I + "shields/_res/shield_buckler/model/bronzebuckler_d.png", mask="metal"),
            s(W + "macebronze/macebronze_d.png", mask="metal"),
            s(W + "BronzeSpear/model/BronzeSpear_d.png", mask="metal"),
            s(W + "atgier/atgeir_bronze_d.png", mask="metal"),
        ]},
    "metal.copper": {
        "label": "copper: ore and Dvergr copper fittings",
        "region": "metal",
        "samples": [
            s(I + "_res/copper/copper.png"),
            s(R + "Rocks/materials/copper/copper_ore_big_d.png"),
            s(R + "Anvil/anvil.png", material=R + "Dvergr/materials/dvergr_copper.mat"),
        ]},
    "metal.tin": {
        "label": "tin ore",
        "region": "metal",
        "samples": [
            s(NATURE + "Rocks_4_tin_d.png"),
        ]},
    "metal.silver": {
        "label": "silver",
        "region": "metal",
        "samples": [
            s(A + "SilverArmour/SilverArmourChest_d.png", mask="metal"),
            s(I + "shields/_res/shield_silver/Silver_shield_d.png", mask="metal"),
            s(W + "silverknife/model/silverknife_d.png", mask="metal"),
            s(W + "SilverWarhammer/model/SilverWarhammer_d.png", mask="metal"),
            s(I + "_res/silver/silver_ore_d.png"),
        ]},
    "metal.blackmetal": {
        "label": "black metal",
        "region": "metal",
        "samples": [
            s(I + "_res/blackmetal/blackmetal_d.png"),
            s(W + "blackmetalsword/model/blackmetalsword_d.png", mask="metal"),
            s(W + "blackmetalaxe/model/blackmetalaxe_d.png", mask="metal"),
            s(I + "shields/_res/shield_blackmetal/BlackMetalShields_d.png", mask="metal"),
            s(W + "blackmetalatgeir/model/blackmetalatgeir_d.png", mask="metal"),
            s(W + "blackmetalknife/model/blackmetalknife_d.png", mask="metal"),
        ]},
    "metal.gold": {
        "label": "gold: veins, crowns, coin piles",
        "region": "metal",
        "samples": [
            s(R + "goldvein/model/GoldVein_d.png"),
            s(I + "helmets/_res/ValheimCrown/model/Valheim_Crown_d.png"),
            s(P + "Treasure/model/coin_pile_decal.png"),
        ]},
    "metal.flametal": {
        "label": "flametal",
        "region": "metal",
        "samples": [
            s(R + "Flametal/model/Flametal_d.png"),
            s(R + "Flametal/model/Flametalore_d.png"),
            s(A + "FlametalArmor/model/FlametalArmor_d.png", mask="metal"),
            s(I + "shields/_res/Flametal_shields/model/Flametal_Shield_d.png", mask="metal"),
            s(W + "Splitner/model/Spear_Flametal_d.png", mask="metal"),
            s(P + "FlametalGate/model/flametalgate_d.png"),
            s(P + "Flametal_pillar_Beam/model/flametalbeam_d.png"),
        ]},
    "bone.bone": {
        "label": "bone",
        "region": "bone",
        "samples": [
            s(C + "Skeleton/model/Texture/Skeleton_d.tga"),
            s(I + "shields/_res/shield_bone_tower/model/Boneshield_d.png"),
            s(W + "bow/SpineSnap/model/SpineSnap_d.png", crop=(0.0, 0.0, 0.55, 0.42)),
            s(I + "materials/_res/bonefragments/bonefragments_d.png"),
            s(C + "Bonemass/material/bonemass_bonebone_d.png"),
            s(C + "TrollSkeleton/model/TrollSkeleton_d.png"),
            s(R + "hugeskull/model/huge_skull_d.png"),
            s(P + "BoneThrone/model/BoneThrone_d.png"),
        ]},
    "bone.antler": {
        "label": "antler",
        "region": "bone",
        "samples": [
            s(C + "Eikthyr/model/old/eikthyrnir_d.png", prefab="HardAntler"),
            s(W + "weapons1/weapons.png", prefab="PickaxeAntler"),
        ]},
    "bone.horn": {
        "label": "horn",
        "region": "bone",
        "samples": [
            s(I + "_res/betahorn/model/betahorn_d.png"),
            s(I + "_res/AnniversaryHorn/model/anniversaryHorn_d.png"),
        ]},
    "bone.teeth": {
        "label": "teeth, fangs and tusks",
        "region": "bone",
        "samples": [
            s(W + "wolffang/fangspear_d.png", prefab="SpearWolfFang", mask="nonmetal"),
            s(W + "bow/DraugrFangBow/DraugrFangBow_d.png"),
            s(W + "AshFang/model/AshFang_d.png"),
        ]},
    "stone.stone": {
        "label": "worked and natural grey stone: walls, floors, rocks, runestones",
        "region": "stone",
        "samples": [
            s(P + "stone/stone.png"),
            s("world/terrain/old/stone_256_d.jpg"),
            s(R + "stonewall/model/stonewall.png"),
            s(R + "stonefloor/materials/stonefloor_d.png"),
            s(I + "_res/stone/rock_256.png"),
            s("world/terrain/old/gouacherock_big.png"),
            s(R + "stonepillar/model/stonepillar_d.png"),
            s(R + "RuneStones/model/runestone_d.png"),
            s(R + "MemorialStones/models/MemorialStone_large_d.png"),
            s(R + "Chests/materials/stonechest_d.png"),
            s(R + "Caverocks/model/curvedrock_d.png"),
            s(R + "stoneslab/model/stoneslab_d.png"),
        ]},
    "stone.marble": {
        "label": "black marble (Mistlands)",
        "region": "stone",
        "samples": [
            s(P + "Marble/material/marble_d.png"),
            s(P + "MarbleBench/model/marblebench_d.png"),
            s(P + "MarbleTable/model/marbletable_d.png"),
            s(I + "materials/_res/BlackMarble/marble_item_d.png"),
        ]},
    "stone.grausten": {
        "label": "grausten and Ashlands stone",
        "region": "stone",
        "samples": [
            s(P + "Grausten/model/Grausten_d.png"),
            s(P + "Grausten/model/Grausten_cracked_d.png"),
            s(P + "Grausten/model/Grausten_Roof_Slab_d.png"),
            s(P + "Ashlands_Build/model/Ashlands_Stone_Ashen_d.png"),
            s(P + "Ashlandsfortress/model/FortressWall1_d.png"),
        ]},
    "crystal.crystal": {
        "label": "crystal and gems",
        "region": "gem",
        "samples": [
            s(W + "battleaxe_crystal/model/battleaxe_crystal_d.png", mask="metal"),
            s(I + "_res/Gemstones/model/Gemstones_d.png"),
            s(I + "_res/Proustite/model/Proustite_d.png"),
        ]},
    "crystal.ice": {
        "label": "ice",
        "region": "gem",
        "samples": [
            s(R + "Caverocks/materials/icewall_d.png"),
            s(R + "Caverocks/materials/icefloor_d.png"),
            s(R + "IceShelf/IceShelves_d.png"),
            s(R + "FrozenShips/model/ice_frozenship_d.png"),
            s(R + "DeepNorth/BlackIce/model/blackice_d.png"),
            s(I + "_res/FrostCore/model/frostcore_d.png"),
        ]},
    "crystal.obsidian": {
        "label": "obsidian",
        "region": "stone",
        "samples": [
            s(R + "Rocks/models/ObsidianRock/model/ObsidanRock_d.png"),
            s(I + "_res/iron/iron.png", material=I + "materials/_res/obsidian/obsidian_nosnow.mat"),
        ]},
}
