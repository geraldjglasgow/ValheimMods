# Palette

The colours the game paints with, sampled from its own textures: per material family (the families of `paint.md`) and
per biome. Written from `measure/palette.py` (data in `data/palette.json`), which reads the family samples measured by
`measure/paint.py` and measures the biome samples of `measure/palette_biomes.py` the same way.

- **Tones.** A texture's 10, 50 and 90 % tones are the mean linear colour of the texels within 5 percentiles of its
  10th, 50th and 90th luminance percentile, over the texels its UVs cover, times the material's tint. A family's tone
  is the per-channel median of its samples' tones. Hex values are sRGB; the linear triples are what Blender's Base
  Color and the paint recipes (`blender/workshop/paint_specs.py`) take.
- **Hue** in degrees (0 red, 30 orange, 60 yellow, 120 green, 180 cyan, 240 blue, 300 magenta): the circular mean of
  the samples' mean hues over texels saturated above 0.12, and the median of their 10th and 90th percentile hues.
  Families under 0.05 saturation are grey and have no hue.
- **S** and **V** are HSV saturation and value of the sRGB colour, 10 / 50 / 90 % (medians over samples).
- A family median mixes its samples: where they differ in hue (dyed cloth, creature skins) the median tone is a
  compromise no single texture has, and the per-texture tones under the table are the ones to pick from. The recipes
  take their default tones from named samples (`tones_from`) and offer the others as presets.

## Families

| Family | 10 % | 50 % | 90 % | 50 % linear | hue (10-90 %) | S 10/50/90 | V 10/50/90 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| wood.planks | #584028 | #715031 | #86693f | (0.167, 0.080, 0.031) | 31 (28 to 33) | 0.51 / 0.55 / 0.59 | 0.35 / 0.44 / 0.52 |
| wood.dark | #221e1e | #564028 | #634f2d | (0.092, 0.051, 0.021) | 27 (22 to 33) | 0.40 / 0.45 / 0.54 | 0.13 / 0.34 / 0.39 |
| wood.logs | #573e2d | #755e44 | #8f754d | (0.177, 0.111, 0.058) | 35 (28 to 41) | 0.31 / 0.48 / 0.60 | 0.34 / 0.45 / 0.57 |
| wood.bark | #43362a | #664f40 | #8a7357 | (0.134, 0.079, 0.052) | 21 | 0.18 / 0.27 / 0.40 | 0.26 / 0.40 / 0.54 |
| wood.fine | #5e4831 | #7f6344 | #9c836d | (0.211, 0.126, 0.058) | 29 (24 to 33) | 0.36 / 0.48 / 0.51 | 0.36 / 0.47 / 0.59 |
| metal.iron | #575557 | #666d64 | #9ba69b | (0.132, 0.153, 0.129) | grey | 0.01 / 0.02 / 0.10 | 0.34 / 0.43 / 0.65 |
| metal.bronze | #724834 | #997a57 | #d8a56b | (0.317, 0.196, 0.096) | 27 (23 to 30) | 0.38 / 0.54 / 0.58 | 0.44 / 0.59 / 0.85 |
| metal.copper | #574b3d | #5d5b4b | #6d6d66 | (0.110, 0.104, 0.071) | 53 (29 to 106) | 0.27 / 0.42 / 0.48 | 0.34 / 0.37 / 0.45 |
| metal.tin | #3d3c3d | #8d8c8c | #b8b8b7 | (0.266, 0.264, 0.261) | grey | 0.01 / 0.02 / 0.06 | 0.25 / 0.56 / 0.71 |
| metal.silver | #735249 | #89898b | #aab5c4 | (0.250, 0.252, 0.259) | grey | 0.01 / 0.02 / 0.05 | 0.44 / 0.56 / 0.77 |
| metal.blackmetal | #141814 | #202020 | #424742 | (0.015, 0.015, 0.015) | grey | 0.00 / 0.03 / 0.11 | 0.10 / 0.13 / 0.28 |
| metal.gold | #716c35 | #d6a64b | #e7b969 | (0.672, 0.382, 0.071) | 45 (42 to 48) | 0.54 / 0.59 / 0.67 | 0.42 / 0.84 / 0.91 |
| metal.flametal | #3c4049 | #5b6162 | #687185 | (0.105, 0.119, 0.121) | 219 (197 to 267) | 0.12 / 0.25 / 0.34 | 0.29 / 0.38 / 0.55 |
| bone.bone | #503830 | #8b6e5d | #b49d7b | (0.257, 0.156, 0.109) | 45 (41 to 48) | 0.30 / 0.38 / 0.43 | 0.31 / 0.54 / 0.71 |
| bone.antler | #885c3e | #aa795f | #ccbb94 | (0.401, 0.190, 0.114) | 31 (16 to 46) | 0.26 / 0.41 / 0.57 | 0.51 / 0.66 / 0.80 |
| bone.horn | #9e6b4f | #b1826d | #c0b4a1 | (0.441, 0.225, 0.153) | 30 (26 to 33) | 0.01 / 0.34 / 0.58 | 0.51 / 0.67 / 0.79 |
| bone.teeth | #6e5a37 | #926d4c | #afa186 | (0.287, 0.152, 0.072) | 47 (22 to 76) | 0.10 / 0.38 / 0.50 | 0.38 / 0.57 / 0.66 |
| leather.leather | #41241a | #482e21 | #6b4928 | (0.065, 0.028, 0.015) | 26 (21 to 30) | 0.49 / 0.64 / 0.71 | 0.26 / 0.29 / 0.42 |
| leather.hide | #493b36 | #714947 | #855c5e | (0.165, 0.067, 0.064) | 33 (25 to 41) | 0.34 / 0.44 / 0.52 | 0.35 / 0.46 / 0.53 |
| leather.fur | #44372e | #635953 | #847c77 | (0.125, 0.101, 0.087) | 30 (14 to 42) | 0.10 / 0.17 / 0.31 | 0.27 / 0.39 / 0.51 |
| cloth.linen | #522c1f | #6d3d35 | #a97853 | (0.152, 0.047, 0.035) | 16 (355 to 35) | 0.40 / 0.55 / 0.77 | 0.33 / 0.43 / 0.67 |
| cloth.rope | #7e7061 | #99968e | #ada9a2 | (0.318, 0.304, 0.272) | 34 (30 to 36) | 0.08 / 0.11 / 0.16 | 0.48 / 0.60 / 0.68 |
| stone.stone | #474544 | #5e5c58 | #797672 | (0.111, 0.107, 0.099) | grey | 0.02 / 0.04 / 0.06 | 0.28 / 0.37 / 0.47 |
| stone.marble | #232323 | #2d2d2c | #4d4942 | (0.026, 0.026, 0.026) | grey | 0.02 / 0.03 / 0.13 | 0.14 / 0.18 / 0.29 |
| stone.grausten | #3b3b3b | #4d4a4a | #534e4d | (0.075, 0.069, 0.068) | 12 (358 to 20) | 0.08 / 0.09 / 0.10 | 0.23 / 0.30 / 0.32 |
| thatch.straw | #5c5037 | #8b7850 | #a08d68 | (0.259, 0.187, 0.081) | 37 (32 to 41) | 0.34 / 0.40 / 0.46 | 0.36 / 0.53 / 0.61 |
| crystal.crystal | #57272a | #7f413f | #d0766a | (0.210, 0.054, 0.050) | 335 | 0.42 / 0.74 / 0.96 | 0.34 / 0.49 / 0.84 |
| crystal.ice | #3b697c | #5b95aa | #85b4c3 | (0.104, 0.298, 0.403) | 186 | 0.35 / 0.36 / 0.37 | 0.48 / 0.66 / 0.76 |
| crystal.obsidian | #1a191a | #393939 | #4e4d4d | (0.042, 0.041, 0.041) | grey | 0.00 / 0.03 / 0.18 | 0.09 / 0.22 / 0.29 |
| chitin.chitin | #2f2622 | #543f39 | #916852 | (0.089, 0.050, 0.041) | 43 (12 to 126) | 0.16 / 0.39 / 0.69 | 0.21 / 0.32 / 0.57 |
| skin.skin | #3e3323 | #555835 | #787f5b | (0.091, 0.098, 0.035) | 64 (40 to 75) | 0.27 / 0.42 / 0.54 | 0.27 / 0.41 / 0.56 |
| skin.flesh | #75453b | #92544d | #af795f | (0.288, 0.088, 0.074) | 10 (0 to 21) | 0.45 / 0.53 / 0.66 | 0.46 / 0.57 / 0.69 |
| veg.mushroom | #912a12 | #b69096 | #ddd8c9 | (0.465, 0.278, 0.304) | 20 (340 to 41) | 0.09 / 0.26 / 1.00 | 0.57 / 0.84 / 0.93 |
| veg.leaves | #464d2e | #5e733c | #739c5a | (0.111, 0.172, 0.045) | 78 (47 to 93) | 0.33 / 0.47 / 0.55 | 0.33 / 0.45 / 0.61 |
| veg.moss | #515c2f | #546232 | #5a6b3a | (0.089, 0.123, 0.032) | 66 (57 to 73) | 0.45 / 0.50 / 0.56 | 0.36 / 0.39 / 0.42 |

### Per texture

The 10 / 50 / 90 % tones of every sample (sRGB hex), to pick a single texture's colours where the family mixes several.

- **wood.planks**: Planks5c_low #543f27 #5b4529 #644b2e; Planks1c #926f41 #977344 #9f7b49; woodchest_d #000000 #634a2b
  #735733; Bench_d #71522d #7f5f37 #896b3f; Chair_d #78562e #7e5f36 #84683f; WorkBench_d #593b24 #674a2e #735538;
  Table_d #75532c #7d5e35 #856a40; spiralstair_d #7a5337 #906243 #9b6b4a; barrel_d #543a23 #7b5533 #8e8f8e; Cart_d
  #473224 #5d4530 #785a3c; boat_d #2e2013 #61452b #865f3a; wood #835336 #8e6040 #986b49; drawbridge_d #391f09 #56412b
  #634d33; wood_roof_d #574128 #634b2e #9e8c70.
- **wood.dark**: DarkWoodBeams_d #221c14 #564028 #614a2d; BlackWood_d #211e1e #22201e #423829; DarkWoodGate_d #1e1912
  #3f3121 #634f38; frostwood_d #4f2f25 #724b3f #8a6353; DarkWoodChair_d #4e392f #745541 #975a28.
- **wood.logs**: Pine_tree_log_wall #5d3a24 #735034 #89633f; LogBench_d #72492e #956945 #b08555; drawbridge_log_d
  #774d32 #946b4d #a68e78; Pine_tree_log_texture #222013 #292617 #2f2a1b; woodpile_diffuse #473b2c #5b5244 #735c44;
  stakewall_d #52412f #776855 #958773.
- **wood.bark**: beech_bark #70685b #766e64 #837a70; birch_bark #434343 #9c9c9c #b0b0b0; oak_bark #3c3329 #584f46
  #9f9d9c; PineTree_log_d #44332b #59453a #735948; olivetree_trunk01 #241e14 #453c29 #6b6148; acacia_trunk01 #65533e
  #786449 #907959; ShootTrunk_d #573927 #724f3a #976d55; AshlandsTrees_d #020102 #111111 #201f20.
- **wood.fine**: finewood_d #685139 #987958 #b79674; Table_oak_d #3c2c25 #563f30 #664a36; runed_furniture_d #523d27
  #5b452a #796c65; Finewoodbow_d #93653a #af8153 #f2cf98.
- **metal.iron**: IronArmorChest_d #7d756f #989695 #b7b7b8; helmet_iron_d #606060 #787878 #a2a2a2; iron_buckler_d
  #696969 #969696 #adadad; ironmace_d #223e17 #427332 #69b352; atgeir_iron_d #4f4f4f #676767 #949494; battleaxe_d
  #424242 #515050 #7b7470; Ironbeam_d #30302e #53524e #a8aaa4; Shield_Designs_d #5f5a5e #645e62 #676163.
- **metal.bronze**: BronzeArmor_Chest_meshes_chest_d #000000 #482c18 #e18f50; helmet_bronze_d #543133 #745a4f #cf9976;
  bronzebuckler_d #786548 #af9872 #c8b18e; macebronze_d #6c3716 #d29360 #fabb88; BronzeSpear_d #785636 #7d5a3a #8f694b;
  atgeir_bronze_d #efae5d #f1af5f #f0b05e.
- **metal.copper**: copper #7c8461 #94875a #6d9a66; copper_ore_big_d #4c4b3d #585b4b #706d66; anvil #57412e #5d4431
  #634735.
- **metal.tin**: Rocks_4_tin_d #3d3c3d #8d8c8c #b8b8b7.
- **metal.silver**: SilverArmourChest_d #7c7977 #cecece #ffffff; Silver_shield_d #8e898c #9d9a9c #aaa8aa; silverknife_d
  #73523d #898989 #c3c3c3; SilverWarhammer_d #4b4c49 #68808b #99b5c6; silver_ore_d #4b4b48 #5e5e5b #797976.
- **metal.blackmetal**: blackmetal_d #0c4707 #10570a #166b0f; blackmetalsword_d #1b1b1b #201f20 #4a4a4a; blackmetalaxe_d
  #161516 #222122 #393839; BlackMetalShields_d #192b19 #7a6a61 #9e9d9d; blackmetalatgeir_d #121212 #191a19 #444344;
  blackmetalknife_d #101010 #212021 #414041.
- **metal.gold**: GoldVein_d #363635 #58544b #deaa26; Valheim_Crown_d #db9953 #dfa64a #e7b969; coin_pile_decal #716c2b
  #d6ce58 #fff46b.
- **metal.flametal**: Flametal_d #303842 #3c4651 #88938c; Flametalore_d #303944 #38414b #555f62; FlametalArmor_d #4e424b
  #747477 #9a9ca2; Flametal_Shield_d #5b4d56 #5b616e #657185; Spear_Flametal_d #5b595b #626162 #686768; flametalgate_d
  #2f3844 #39424c #47505b; flametalbeam_d #3c4049 #5f6575 #919aaa.
- **bone.bone**: Skeleton_d #5c3f2b #866950 #b39e82; Boneshield_d #94785a #a48966 #b59b75; SpineSnap_d #98805c #b1976b
  #d1b67b; bonefragments_d #be9e6d #c1a372 #c6ac7b; bonemass_bonebone_d #422918 #8f6b4c #d6aa80; TrollSkeleton_d #192122
  #212b2f #3c4345; huge_skull_d #000100 #767166 #939289; BoneThrone_d #2c3033 #4f5053 #5a5a5a.
- **bone.antler**: eikthyrnir_d #85533e #a87763 #bbab8c; weapons #8a653e #ab7a5b #dbca9c.
- **bone.horn**: betahorn_d #62564c #868282 #bfa87f; anniversaryHorn_d #c67b52 #d28251 #c1c0bc.
- **bone.teeth**: fangspear_d #765a42 #926d4c #afa186; DraugrFangBow_d #6e5b37 #aeaf78 #cde5e5; AshFang_d #2c292c
  #34363c #858179.
- **leather.leather**: helmet_leather_d #412b1e #482e22 #6b493a; leatherscraps_d #513b27 #674b2f #795836; LoxChest_D
  #2c1f09 #462e09 #744b0b; LoxLegs_D #2d1b04 #362205 #613f0d; belt1_d #48241a #602f21 #6b3828; SpineSnap_d #281817
  #2b1b1a #322120; asksvinsaddle_d #58463c #57514b #947b6d.
- **leather.hide**: hide01 #572808 #734014 #86521e; hide02 #4a2206 #78471c #855a2e; bearhide_d #42332f #50413c #64524c;
  troll_hide #0a3758 #164977 #1f5185; moose_rug_d #6c615b #83746b #918379; Sealrug_d #423f3c #6f6a66 #9c9792;
  asksvinrug_d #495840 #808651 #8fa26c; Tanningrack_d #5c3e27 #6b4a30 #795e46.
- **leather.fur**: WolfCape_d #000000 #67645f #cecdbf; LoxCape_D #402f22 #553f30 #7b695d; rug_fur #4d3624 #59402c
  #6d5a4a; rug_wolf #504237 #716c68 #8d8c8c; wolfhide_d #393839 #999999 #bdbdbd; BjornRug_d #483b35 #5f4d45 #725e57.
- **cloth.linen**: ClothesCasual1_d #3a3030 #4a3d3c #9c784e; ClothesCasual4_d #32221f #493429 #a07953; jutecarpet_d
  #53251f #6d251f #872620; jutecarpet_blue_d #03415b #064b67 #0b5472; Banner_1_borderRedWhite_d #94180b #a81104 #b47570;
  sail_white #a39f99 #aeaaa5 #b6b3af; ShipTent_d #521c0a #6d2510 #99371e; NordCape_d #432c12 #856649 #c0a587;
  morkhalla_rug1_d #48251c #542c24 #ce7601; TraderTent_d #63493a #6e5b4a #a98562; HildirFabrics1_d #62333b #6b4b35
  #ad9442.
- **cloth.rope**: rope_d #6b6250 #89816f #9e9684; nornthread_d #8d7b6f #a7a7a7 #bababa.
- **stone.stone**: stone #585651 #67635d #7c7872; stone_256_d #2f2a26 #4d4b48 #7a7977; stonewall #4a4848 #636060
  #777373; stonefloor_d #595855 #787778 #909190; rock_256 #5a574e #706c63 #85857e; gouacherock_big #0a0a09 #0f100f
  #171716; stonepillar_d #3f3f3f #535353 #5f5f5f; runestone_d #565656 #656565 #848584; MemorialStone_large_d #2b2a2b
  #353435 #5e5d5e; stonechest_d #2e2925 #544c45 #685f56; curvedrock_d #434340 #575754 #71716d; stoneslab_d #505050
  #747474 #8b8b8b.
- **stone.marble**: marble_d #232323 #2c2c2c #4a4945; marblebench_d #232323 #2e2d2d #594a3f; marbletable_d #232323
  #2e2d2d #504941; marble_item_d #232323 #2c2c2c #474742.
- **stone.grausten**: Grausten_d #4e4d4e #535253 #545554; Grausten_cracked_d #3b3b3b #4d4c4d #545354;
  Grausten_Roof_Slab_d #38231c #452d25 #4b2f27; Ashlands_Stone_Ashen_d #474241 #504a4a #534e4d; FortressWall1_d #1b343b
  #213842 #28424b.
- **thatch.straw**: straw_roof #5b4c36 #6d5e48 #7e705c; straw_roof_worn #5d5447 #70685c #857d71; straw_roof_corner
  #5b4c36 #6c5d48 #7e705b; strawfloor_d #91784a #b39760 #ccb076; birdnest_d #533322 #b18e55 #cfb079; strawhat_d #7a6538
  #a1854b #b79a5f.
- **crystal.crystal**: battleaxe_crystal_d #8f8c8f #cdb2f3 #d0e1fe; Gemstones_d #532727 #6b413f #5f766a; Proustite_d
  #57202a #7f2837 #d71f4f.
- **crystal.ice**: icewall_d #77a7b9 #7fb1c5 #85bbcf; icefloor_d #354d54 #3e5862 #496773; IceShelves_d #415e68 #65868c
  #92adb7; ice_frozenship_d #78a8bb #7fb2c5 #85bacf; blackice_d #101110 #2b2b29 #545453; frostcore_d #32728d #4ea1c2
  #87c1d7.
- **crystal.obsidian**: ObsidanRock_d #070707 #434342 #5d5d5d; iron #242424 #2d2d2d #383838.
- **chitin.chitin**: chitin #c4bdae #dbd4c3 #ede4d3; carapacearmor_d #1e2222 #3a2c26 #4f463f; Shield_carapaceround_d
  #022417 #2c3b39 #6f6856; seeker_d #2e2615 #543b1c #914e26; seekerBrute_d #422d31 #5f3f3d #a46852; Carapace_d #1a2425
  #233031 #4c5147; SeekerQueen_d #362935 #904d4c #bdab98; Deathsquito_d #2f3521 #414526 #69512b; Feasting_d #421715
  #b15f4f #c26b57.
- **skin.skin**: troll_diffuse #192029 #536170 #7f8a8d; Draugr_d #4b3618 #43471b #75704a; greydrawrf_diffuse #362a1b
  #5a4e30 #94795d; goblin_d #453720 #574723 #705b26; GoblinBrute_d #3c491e #53672f #809150; Jotnar_d #413428 #546b74
  #73868d; neck_d #6a553b #627950 #75925e; PlayerCharacter_01 #8e5b45 #ab775d #c79376; frosttroll_d #303234 #66675e
  #a8a6a1; Rotvalta_d #2e281d #423a29 #645f40; Morgen_d #5e241c #77402d #7b725a; Fenring_d #262626 #393939 #595652.
- **skin.flesh**: bearmeat_uncooked_d #601715 #722b29 #91514e; UncookedDeerMeat_d #b25a55 #bc706b #c28784; raw_meat_d
  #943a32 #9a463e #af655f; entrails_d #75463c #906652 #b68a79; softtissue_d #ab593d #b56f4d #b78d60; heart #6d453b
  #8c545f #7e7940; Moosemeat_d #704b45 #847870 #a79e97; UncookedWolfMeat_d #7d312d #934844 #a55855; bloodbag_d #751b13
  #922920 #b83933.
- **veg.mushroom**: Boletus_edulis_d #9b2a12 #8f7e73 #ac9881; bzerkermushroom_d #910000 #e5dbc1 #f0ebdb;
  MistlandsShrooms_d #555d8f #b69096 #ddd8c9.
- **veg.leaves**: beech_leaf #464d2e #5e703c #8b9b5b; birch_leaf #334c24 #4b7433 #73a65a; oak_leaf #3e4628 #465c29
  #6e8d4e; Bush01_d #52643b #67804e #7b9d60; shrub_2 #5b5c48 #5f6b4d #687f4c; PineTree_01 #303f29 #3e4f2e #4a6332;
  Pine_tree_texture_small #2f3e1f #41522b #5b693f; ShootLeaf_d #4f4b1b #50730f #689c10; kale_d #386b43 #87ae63 #f9e863;
  turnip #946389 #b6b29e #b6b3a0; swampplant2_d #6e714f #8c936b #99a371.
- **veg.moss**: stonemoss #505a2d #516230 #566b30; stonemoss_heath #7a6f42 #817649 #877e4b; stonemoss_swamp #525e31
  #566334 #5c6a38; stonekit_moss #485326 #4a5c29 #51662b; creep_d #9d7a22 #a49628 #b0ac3b; Planks5c_worn #4b4b38 #525140
  #595545.

## What the recipes paint with

The default dark, mid and light tones each recipe of `blender/workshop/paint.py` resolves to (sRGB; the recipes use
them in linear), and where they come from; `neutral` families are turned into greys of the same luminance, and skin
and fur tones are raised to keep their mean under the light from above. `preset=` (`dye=` for cloth) takes a single
texture's tones instead.

| Recipe | Family | Dark | Mid | Light | Taken from |
| --- | --- | --- | --- | --- | --- |
| `wood_planks` | wood.planks | #584028 | #715031 | #86693f | every sample |
| `wood_dark` | wood.dark | #221c14 | #564028 | #634f2d | DarkWoodBeams_d, DarkWoodGate_d, DarkWoodChair_d |
| `wood_logs` | wood.logs | #573e2d | #755e44 | #8f754d | every sample |
| `wood_bark` | wood.bark | #40332a | #594a40 | #7b6e5f | beech_bark, oak_bark, PineTree_log_d, olivetree_trunk01 |
| `wood_fine` | wood.fine | #5e4831 | #7f6344 | #9c836d | every sample |
| `iron` | metal.iron | #555555 | #6b6b6b | #a3a3a3 | every sample (as greys) |
| `bronze` | metal.bronze | #724835 | #997a57 | #d8a56b | every sample |
| `copper` | metal.copper | #57412e | #5d4431 | #634735 | anvil |
| `tin` | metal.tin | #3c3c3c | #8c8c8c | #b8b8b8 | every sample (as greys) |
| `silver` | metal.silver | #7a7a7a | #9b9b9b | #a9a9a9 | Silver_shield_d, SilverArmourChest_d, silver_ore_d (as greys) |
| `blackmetal` | metal.blackmetal | #171717 | #202020 | #454545 | every sample (as greys) |
| `gold` | metal.gold | #b08543 | #dbbb51 | #f3d96a | Valheim_Crown_d, coin_pile_decal |
| `flametal` | metal.flametal | #3c4049 | #5b6162 | #687185 | every sample |
| `bone` | bone.bone | #7c6147 | #9a7b5c | #c4a47e | Skeleton_d, SpineSnap_d, Boneshield_d, bonemass_bonebone_d |
| `antler` | bone.antler | #885c3e | #aa795f | #ccbb94 | every sample |
| `horn` | bone.horn | #9e6b4f | #b1826d | #c0b4a1 | every sample |
| `teeth` | bone.teeth | #6e5a37 | #926d4c | #afa186 | every sample |
| `leather` | leather.leather | #41241a | #482e21 | #6b4928 | every sample |
| `hide` | leather.hide | #512e1c | #6f4427 | #7f563b | hide01, hide02, bearhide_d, Tanningrack_d |
| `fur` | leather.fur | #46392f | #665c56 | #88807b | every sample |
| `linen` | cloth.linen | #63493a | #85664a | #b6a587 | sail_white, TraderTent_d, NordCape_d |
| `rope` | cloth.rope | #7e7061 | #99968e | #ada9a2 | every sample |
| `stone` | stone.stone | #474544 | #5e5c58 | #797672 | every sample |
| `marble` | stone.marble | #232323 | #2d2d2d | #494949 | every sample (as greys) |
| `grausten` | stone.grausten | #3b3b3b | #4d4a4a | #534e4d | every sample |
| `thatch` | thatch.straw | #5c5037 | #8b7850 | #a08d68 | every sample |
| `crystal` | crystal.crystal | #8f8c8f | #cdb2f3 | #d0e1fe | battleaxe_crystal_d |
| `ice` | crystal.ice | #3b697c | #5b95aa | #85b4c3 | every sample |
| `obsidian` | crystal.obsidian | #191919 | #393939 | #4d4d4d | every sample (as greys) |
| `chitin` | chitin.chitin | #272524 | #48362c | #765043 | seeker_d, seekerBrute_d, carapacearmor_d, Carapace_d |
| `skin` | skin.skin | #48381f | #5b4a2b | #7a744d | Draugr_d, greydrawrf_diffuse, goblin_d, neck_d, Rotvalta_d |
| `flesh` | skin.flesh | #75453b | #92544d | #af795f | every sample |
| `mushroom` | veg.mushroom | #9b2a12 | #8f7e73 | #ac9881 | Boletus_edulis_d |
| `leaves` | veg.leaves | #464d2e | #5e733c | #739c5a | every sample |
| `moss` | veg.moss | #515c2f | #546232 | #5a6b3a | every sample |

Presets (dark, mid, light as the recipe paints them):

- cloth.linen `red` (jutecarpet_d): #53251f #6d251f #872620
- cloth.linen `blue` (jutecarpet_blue_d): #03415b #064b67 #0b5472
- cloth.linen `undyed` (sail_white): #a39f99 #aeaaa5 #b6b3af
- cloth.linen `brown` (ClothesCasual4_d): #32221f #493429 #a07953
- cloth.linen `tent` (TraderTent_d): #63493a #6e5b4a #a98562
- cloth.linen `ship` (ShipTent_d): #521c0a #6d2510 #99371e
- skin.skin `troll` (troll_diffuse): #1a222b #566574 #848f92
- skin.skin `draugr` (Draugr_d): #4e3819 #464a1c #7a744d
- skin.skin `greydwarf` (greydrawrf_diffuse): #382c1c #5e5132 #9a7e61
- skin.skin `goblin` (GoblinBrute_d): #3f4c20 #566b31 #859753
- skin.skin `human` (PlayerCharacter_01): #945f48 #b27c61 #cf997b
- skin.skin `jotun` (Jotnar_d): #44362a #576f79 #788b92
- skin.skin `neck` (neck_d): #6e593e #667e53 #7a9862
- skin.skin `frost` (frosttroll_d): #323436 #6a6b62 #aeaca7
- skin.skin `fenring` (Fenring_d): #282828 #3c3c3c #5d5a55
- crystal.crystal `lilac` (battleaxe_crystal_d): #8f8c8f #cdb2f3 #d0e1fe
- crystal.crystal `red` (Proustite_d): #57202a #7f2837 #d71f4f
- crystal.crystal `gem` (Gemstones_d): #532727 #6b413f #5f766a
- veg.mushroom `boletus` (Boletus_edulis_d): #9b2a12 #8f7e73 #ac9881
- veg.mushroom `toadstool` (bzerkermushroom_d): #910000 #e5dbc1 #f0ebdb
- veg.mushroom `mistlands` (MistlandsShrooms_d): #555d8f #b69096 #ddd8c9
- leather.hide `deer` (hide02): #4a2206 #78471c #855a2e
- leather.hide `bear` (bearhide_d): #42332f #50413c #64524c
- leather.hide `troll` (troll_hide): #0a3758 #164977 #1f5185
- leather.hide `seal` (Sealrug_d): #423f3c #6f6a66 #9c9792
- leather.hide `moose` (moose_rug_d): #6c615b #83746b #918379
- leather.fur `wolf` (rug_wolf): #524439 #746f6b #919090
- leather.fur `lox` (LoxCape_D): #423123 #584132 #7f6c60
- leather.fur `bear` (BjornRug_d): #4a3d37 #624f47 #75615a
- metal.gold `crown` (Valheim_Crown_d): #db9953 #dfa64a #e7b969
- metal.gold `coins` (coin_pile_decal): #716c2b #d6ce58 #fff46b

## Metals by tier (the items agent)

Median sRGB of the metal texels of weapons and armour, `data/items.json`: bronze (206, 146, 115) to (240, 175, 94);
iron (104, 104, 104) to (181, 181, 181); silver 157 to 186 grey; black metal 24 to 41 grey; flametal 74 to 95 blue
grey; Deep North gold (156, 117, 71). Wood hafts (98 to 148, 73 to 113, 49 to 82); shield planks (132, 89, 51) to
(148, 93, 57); saturated deep red wraps (74 to 104, 0 to 22, 5 to 8). The untextured ingots are colours only
(`paint.md`).

## Building materials, new and worn (the pieces agent)

Median sRGB of the texels a piece's UVs cover, times its tint, new and worn (`data/pieces.json` key `paint`). Worn
textures are greyer and a little lighter: the planks go from #5d4529 to #52513e.

| Piece material | Texture | Tint | New (median sRGB) | Worn |
| --- | --- | --- | --- | --- |
| wood planks | Planks5c_low.png (128 px) | white | #5d4529 | #52513e |
| core wood logs | Pine_tree_log_wall.png (128 px) | (0.79, 0.79, 0.79) | #835c3c | #726750 |
| darkwood | DarkWoodBeams_d.png (128 px) | white | #564028 | #5d5347 |
| thatch | straw_roof.png (64 px) | (0.79, 0.79, 0.79) | #827257 | #7c7365 |
| darkwood shingles | DarkWoodBeams_d.png (128 px) | white | #564028 | #5d5347 |
| stone | stone.png (128 px) | (0.79, 0.79, 0.79) | #76736b | #6a6760 |
| iron cage | metalwall.png (32 px) | (0.63, 0.63, 0.63) | #65645c | #685948 |
| iron beam | Ironbeam_d.png (256 px) | white | #594632 | #5d5139 |
| black marble | marble_d.png (256 px) | white | #2b2c2b | #2b2c2b |
| grausten | Grausten_d.png (128 px) | (0.79, 0.79, 0.79) | #605f60 | #595759 |
| ashwood | BlackWood_d.png (128 px) | white | #21201e | - |
| stave scales | Scaledwall2x2_d.png (128 px) | white | #4f3b23 | - |
| stake wall | stakewall_d.png (128 px) | white | #736855 | - |
| banner cloth | Banner_1_borderBlackWhite_d.png (128 px) | white | #373737 | - |
| dvergr metal | anvil.png (32 px) | (1.00, 0.75, 0.56) | #5d4f46 | - |
| workbench | WorkBench_d.png (256 px) | (0.62, 0.62, 0.62) | #89633f | #6f6450 |
| forge | Forge_d.png (256 px) | (0.62, 0.62, 0.62) | #6e5033 | #6a6150 |
| grausten roof | Grausten_Roof_Slab_d.png (128 px) | (0.72, 0.50, 0.43) | #564541 | - |
| wood chest | woodchest_d.png (128 px) | white | #634a2b | - |
| reinforced chest | ironchest_d.png (128 px) | white | #705a39 | - |
| table | Table_d.png (256 px) | (0.71, 0.70, 0.61) | #997449 | - |
| bed | BedSimple_d.png (128 px) | (0.80, 0.78, 0.64) | #a27a4d | #7e755e |
| raven throne | RavenThrone_d.png (128 px) | (0.86, 0.83, 0.62) | #a2794e | - |
| torch | wood.png (32 px) | (0.76, 0.76, 0.76) | #a9764f | - |
| brazier | Brazier02_d.png (64 px) | white | #c09263 | - |
| hearth | HeartNew_d.png (256 px) | white | #52514c | #43423e |
| stonecutter | StoneCutterBench_d.png (256 px) | white | #a37c49 | - |
| smelter | smelter.png (256 px) | white | #4a4944 | - |
| charcoal kiln | newcharcoalkiln_d.png (256 px) | white | #59524c | - |
| fermenter | fermenter_d.png (256 px) | (0.77, 0.77, 0.77) | #73503f | - |
| portal | portal_small_d.png (256 px) | (0.80, 0.75, 0.68) | #79553d | - |
| spinning wheel | SpinningWheel_d.png (256 px) | (0.60, 0.60, 0.60) | #825c45 | - |
| windmill | windmill_d.png (256 px) | white | #8c7c62 | - |

## Biomes

Sampled from each biome's ground cover, rocks, trees, foliage, creatures and buildings (`measure/palette_biomes.py`);
"overall" pools the roles. `biomes.md` (the environment agent) has each biome's terrain slices, prop palettes, weather
light and fog from `data/environment.json` (keys `terrain` and `biomes.*.palette`); a new asset for a biome takes its
tones from there and its material family's here, and is judged under that biome's light.

| Biome | 10 % | 50 % | 90 % | hue | S 50 % | V 50 % | roles (50 % tone) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| meadows | #52553b | #606e3f | #77855b | 61 | 0.39 | 0.45 | ground #57793f, rock #62674e, wood #7c7266, foliage #5e703c, creatures #604536 |
| black_forest | #433f2b | #59533a | #73705d | 58 | 0.39 | 0.36 | ground #51522e, rock #5e5e5d, wood #807b78, foliage #40512d, creatures #5a6150 |
| swamp | #38311b | #444229 | #716644 | 45 | 0.39 | 0.29 | ground #5f693d, rock #3c3b38, wood #413922, foliage #8c936b, creatures #423a1b |
| mountains | #534b4a | #5c6f6e | #83a3a5 | 62 | 0.06 | 0.46 | ground #b7bdbe, rock #6d8e9a, wood #5b4640, foliage #545f5e, creatures #5d7d7c |
| plains | #4f5033 | #596433 | #7f7e50 | 54 | 0.48 | 0.40 | ground #7e844a, rock #6a6348, wood #786449, foliage #697333, creatures #504624, building #54372c |
| mistlands | #423221 | #5a5230 | #8e6c40 | 47 | 0.54 | 0.42 | ground #a49628, rock #282828, wood #724f3a, foliage #526c15, creatures #5f5244, building #7d5334 |
| ashlands | #1e2421 | #373c34 | #544d4f | 129 | 0.33 | 0.26 | ground #89575b, rock #414347, wood #111111, foliage #472217, creatures #374130, building #213842 |
| deep_north | #413428 | #656b70 | #737d79 | 157 | 0.18 | 0.44 | rock #4f666a, wood #65433d, foliage #667b72, creatures #708087, building #54595c |
| ocean | #5f2d2e | #895145 | #b29f8b | 50 | 0.55 | 0.52 | creatures #925142, rock #675850 |

- **meadows**: ground: grass_terrain_color #57793f; rock: rock_256 #706c63, stonemoss #516230; wood: beech_bark #766e64,
  oak_bark #827569; foliage: beech_leaf #5e703c, oak_leaf #465c29, Bush01_d #67804e; creatures: Boar_valheim_d #564536,
  Deer Pixel #60452d, neck_d #627950.
- **black_forest**: ground: forest_groundcover #43532a, forest_groundcover_brown #5d5132; rock: gouacherock_big #565754,
  runestone_d #656565; wood: PineTree_log_d #59453a, birch_bark #9c9c9c; foliage: PineTree_01 #3e4f2e,
  Pine_tree_texture_small #41522b; creatures: greydrawrf_diffuse #5a4e30, troll_diffuse #536170, Skeleton_d #866950.
- **swamp**: ground: grass_toon1_yellow #686f45, stonemoss_swamp #566334; rock: stone_256_d #4d4b48, stone_sunken
  #22211f; wood: olivetree_trunk01 #453c29, deadbranch #3e371a; foliage: swampplant2_d #8c936b; creatures: Draugr_d
  #43471b, blob_d #5d543a, swampfish_d #2d1c1b, Rotvalta_d #423a29, Bonemass_D #312000.
- **mountains**: ground: forest_groundcover_snow #b7bdbe; rock: curvedrock_d #575754, icewall_d #7fb1c5; wood:
  Pine_tree_snow_d #5b4640; foliage: Pine_tree_snow_small_d #545f5e; creatures: Wolf Pixel #aba397, Fenring_d #393939,
  Hatchling_D #597d8a, Golem_d #8a897c, Ulv_d #5d5a57.
- **plains**: ground: grass_heath #998f5a, grass_heath_green #597834; rock: heathrock_d #4a4a48, stonemoss_heath
  #817649; wood: acacia_trunk01 #786449; foliage: shrub_3_heath #606433, Bush01_heath_d #69732e, barley #8e8749;
  creatures: goblin_d #574723, GoblinBrute_d #53672f, Halstein_d #4e3615, Deathsquito_d #414526; building:
  GoblinVillage_d #54372c.
- **mistlands**: ground: creep_d #a49628; rock: mistlands_cliff_d #232424, marble_d #2c2c2c; wood: ShootTrunk_d #724f3a;
  foliage: ShootLeaf_d #50730f, MistlandsVegetation_d #53651a; creatures: seeker_d #543b1c, seekerBrute_d #5f3f3d,
  Gjall_d #695244, Feasting_d #b15f4f, DvergrBody #306365; building: DvergrTownPieces_d #7d5334.
- **ashlands**: ground: Ashlandsvegetation_d #89575b; rock: AshlandsRock_d #252e38, Grausten_d #535253; wood:
  AshlandsTrees_d #111111; foliage: vineberrysapling_d #472217; creatures: Charred_d #212c30, asksvin_d #33422f,
  Morgen_d #77402d, Volture_d #3b4240; building: FortressWall1_d #213842.
- **deep_north**: rock: IceShelves_d #65868c, blackice_d #2b2b29; wood: Pine_tree_plantable_trunk_d #553a3b, frostwood_d
  #724b3f; foliage: Pine_tree_plantable_d #485828, lingon_d #7b9498; creatures: Jotnar_d #546b74, Barka_d #708087,
  seal_d #a7a7a7; building: morkhallawall_d #363a3f, JotunStatues_d #686e70.
- **ocean**: creatures: SeaSerpent_d #863d31, BonemawSerpent_d #9e5f4e; rock: leviathan_d #172e3b, barnacle_d #8c7260.

What the biomes show:

- **Meadows** and **Plains** are the warm, saturated greens and olives (saturation about 0.35 to 0.48), the lightest
  ground.
- **Black Forest** and **Swamp** are darker (value 0.29 to 0.36): olive and brown greens in the forest, murky olive and
  brown in the swamp, saturated creatures (Draugr olive, Leech red, Blob green).
- **Mountains** and **Deep North** are grey (saturation 0.06 to 0.18): cool blue greys, white snow, pale cyan ice.
- **Mistlands** is ochre and brown under its purple fog (the creep and the Seekers carry the colour); its black marble
  and cliffs near black.
- **Ashlands** is the darkest (value 0.26): charcoal greys with dark green lichens, red and orange only in lava,
  embers and flametal.

## Creatures (the creatures agent)

The middle tone of each creature's main albedo is under `colour` in `data/creatures.json` (dark, middle, light,
saturation, contrast). Examples, sRGB: Boar (104, 82, 64); Draugr (79, 81, 57); Leech (143, 48, 51); Wolf
(131, 131, 131) (grey, tinted per variant); Stone Golem (137, 136, 124); Goblin (119, 104, 51); Lox (65, 44, 10);
Seeker soldier (86, 57, 57); Charred (34, 49, 53); Morgen (90, 60, 49); Barka (112, 123, 124).
