# Catalogue: which game sound to reuse

Reusing the game's own sound is often the best answer: it is already in the game's mix and costs nothing. This page
lists the sound prefabs worth reusing, by purpose, with the numbers that decide between them. Every name was read
from the reference export (`measure/sfx_prefabs.py`) and is the prefab name `ZNetScene.instance.GetPrefab` takes.

## Three ways to use a game sound

1. **Play it as it is.** Add the prefab to an effect list the game already plays
   (`new EffectList.EffectData { m_prefab = ZNetScene.instance.GetPrefab("sfx_troll_alerted") }`), which spawns it the
   game's way (networked, every nearby player hears it), or play it on this machine only with
   `LocalEffects.LocalEffect.Sound(prefab, position)`.
2. **A variant: the same clips, other settings.** `BundlePrefabs.SfxPrefabs.Variant(scene, "sfx_greydwarf_idle",
   "MyMod_giant_idle", new SfxSettings { MinPitch = 0.6f, MaxPitch = 0.65f, Caption = "$enemy_mymod_giant" })`: how the
   game makes its Greyling, Elite and Brute (see [creatures.md](creatures.md)). No bundle, no new audio.
3. **New clips through a copy.** `SfxPrefabs.Copy(scene, "sfx_greydwarf_idle", "MyMod_rootling_idle", bundle, clips,
   settings)`: the copy keeps the game prefab's reach, roll-off, mixer group, reverb, concurrency and caption type;
   the clips come from the mod's bundle (`AssetWorkshop/sfx`). Choose the template from the tables below by the
   archetype, the reach and the group you want.

The **copy** column says how a prefab copies: `yes` is a sound-only prefab (one ZSFX, no particles); `2 layers` has a
second ZSFX child (an overlay, a slam, a slurp): `Copy` keeps the first layer and removes the second, `CopyLayers`
fills both; `particles` is an `fx_` prefab that also draws dust or splashes, which a copy keeps.

Columns: clips (variations), length s and played LUFS (medians of its clips; the level the game plays it at before
distance), ZSFX pitch, reach (AudioSource max distance, m) and roll-off, mixer group.

## Creature voices

| prefab | copy | clips | length s | played LUFS | pitch | reach | group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `sfx_greydwarf_idle` | yes | 8 | 1.75 | -21.3 | 1.2-1.3 | 50 custom | SFX |
| `sfx_greydwarf_alerted` | yes | 3 | 3.34 | -16.5 | 1.1-1.3 | 50 custom | SFX |
| `sfx_greydwarf_attack` | yes | 4 | 0.80 | -14.2 | 1.0-1.1 | 20 custom | SFX |
| `sfx_greydwarf_hit` | yes | 3 | 0.90 | -17.2 | 1.1-1.2 | 35 custom | SFX |
| `sfx_greydwarf_death` | yes | 1 | 1.62 | -8.8 | 1.1-1.2 | 35 custom | SFX |
| `sfx_skeleton_idle` | yes | 3 | 0.33 | -31.4 | 1.2-1.3 | 40 custom | SFX |
| `sfx_skeleton_alerted` | yes | 5 | 0.81 | -20.4 | 0.9-1.1 | 80 custom | SFX |
| `sfx_skeleton_hit` | yes | 4 | 0.72 | -18.2 | 0.9-1.1 | 35 custom | SFX |
| `sfx_skeleton_death` | yes | 3 | 0.87 | -10.3 | 0.9-1.1 | 20 custom | SFX |
| `sfx_draugr_idle` | yes | 7 | 1.43 | -23.3 | 1.2-1.3 | 40 custom | SFX |
| `sfx_draugr_alerted` | yes | 1 | 1.28 | -14.6 | 0.9-1.1 | 80 custom | SFX |
| `sfx_draugr_hit` | yes | 5 | 0.61 | -21.4 | 1.1-1.2 | 35 custom | SFX |
| `sfx_draugr_death` | yes | 5 | 2.03 | -14.7 | 1.0-1.1 | 20 custom | SFX |
| `sfx_wolf_alerted` | yes | 2 | 0.68 | -19.2 | 1.0-1.3 | 50 custom | SFX |
| `sfx_wolf_attack` | yes | 3 | 1.18 | -12.8 | 1.0-1.1 | 20 custom | SFX |
| `sfx_wolf_hit` | yes | 1 | 1.02 | -17.9 | 0.9-1.1 | 35 custom | SFX |
| `sfx_wolf_death` | yes | 2 | 0.93 | -10.4 | 0.9-1.0 | 40 custom | SFX |
| `sfx_boar_idle` | yes | 7 | 0.70 | -22.8 | 1.3-1.5 | 60 log | SFX |
| `sfx_boar_alerted` | yes | 2 | 1.62 | -13.7 | 0.9-1.1 | 80 log | SFX |
| `sfx_boar_hit` | yes | 3 | 0.90 | -12.4 | 1.1-1.2 | 30 log | SFX |
| `sfx_boar_death` | yes | 2 | 1.50 | -11.7 | 0.9-1.0 | 50 log | SFX |
| `sfx_bear_idle` | yes | 10 | 1.24 | -36.7 | 1.0 | 100 custom | SFX |
| `sfx_bear_hurt` | yes | 9 | 1.48 | -15.1 | 1.0 | 100 custom | SFX |
| `sfx_bear_death` | yes | 7 | 1.75 | -15.2 | 1.0 | 100 custom | SFX |
| `sfx_troll_idle` | yes | 8 | 1.82 | -12.1 | 0.9-1.1 | 60 custom | SFX |
| `sfx_troll_alerted` | yes | 3 | 1.75 | -13.1 | 0.9-1.0 | 50 custom | SFX |
| `sfx_troll_hit` | yes | 2 | 0.94 | -16.2 | 0.9-1.0 | 50 custom | SFX |
| `sfx_troll_death` | yes | 1 | 3.59 | -8.0 | 0.9-1.1 | 50 custom | SFX |
| `sfx_lox_alerted` | yes | 4 | 1.62 | -11.3 | 0.9-1.0 | 50 log | SFX |
| `sfx_ghost_idle` | yes | 3 | 1.84 | -21.9 | 0.9-1.0 | 30 custom | SFX |
| `sfx_ghost_alert` | yes | 1 | 1.64 | -21.4 | 0.9-1.0 | 40 custom | SFX |
| `sfx_wraith_idle` | yes | 3 | 2.34 | -22.0 | 1.2-1.3 | 50 log | SFX |
| `sfx_wraith_alerted` | yes | 1 | 1.98 | -17.3 | 0.9-1.1 | 40 custom | SFX |
| `sfx_wraith_death` | 2 layers | 1 | 1.78 | -12.9 | 1.0-1.1 | 25 custom | SFX |
| `sfx_goblin_idle` | yes | 4 | 0.81 | -19.3 | 1.2-1.3 | 60 custom | SFX |
| `sfx_goblin_alerted` | yes | 3 | 0.46 | -15.4 | 1.1-1.2 | 70 custom | SFX |
| `sfx_goblin_hit` | yes | 9 | 0.62 | -18.5 | 1.2-1.3 | 35 custom | SFX |
| `sfx_goblin_death` | yes | 4 | 1.05 | -10.5 | 1.0-1.1 | 35 custom | SFX |
| `sfx_blob_idle` | yes | 1 | 1.89 | -22.9 | 0.9-1.1 | 40 custom | SFX |
| `sfx_blob_alerted` | yes | 1 | 1.27 | -21.1 | 0.9-1.1 | 40 custom | SFX |
| `sfx_blob_hit` | yes | 3 | 1.14 | -14.9 | 0.9-1.1 | 30 custom | SFX |
| `sfx_blob_death` | yes | 3 | 2.08 | -12.7 | 0.9-1.1 | 35 custom | SFX |
| `sfx_imp_alerted` | yes | 3 | 1.30 | -12.3 | 0.9-1.0 | 50 custom | SFX |
| `sfx_imp_hit` | yes | 3 | 0.88 | -17.3 | 0.9-1.1 | 35 custom | SFX |
| `sfx_imp_death` | 2 layers | 3 | 2.11 | -7.5 | 0.9-1.1 | 60 custom | SFX |
| `sfx_tick_idle` | yes | 10 | 1.36 | -24.6 | 0.9-1.1 | 60 log | SFX |
| `sfx_tick_alerted` | yes | 6 | 1.00 | -18.7 | 0.9-1.1 | 60 log | SFX |
| `sfx_tick_hurt` | yes | 6 | 0.77 | -18.8 | 0.9-1.1 | 60 log | SFX |
| `sfx_seeker_idle` | yes | 11 | 1.96 | -17.5 | 0.7-0.8 | 30 log | SFX |
| `sfx_seeker_alerted` | yes | 5 | 0.77 | -12.7 | 0.9-1.0 | 30 log | SFX |
| `sfx_seeker_hurt` | yes | 8 | 0.84 | -14.2 | 0.75-0.9 | 30 log | SFX |
| `sfx_leech_idle` | yes | 9 | 2.50 | -19.4 | 0.9-1.0 | 80 log | SFX |
| `sfx_leech_alerted` | yes | 9 | 2.50 | -17.9 | 1.0-1.1 | 40 log | SFX |
| `sfx_leech_death` | yes | 3 | 0.84 | -12.5 | 1.0-1.1 | 30 log | SFX |
| `sfx_neck_idle` | yes | 3 | 0.84 | -22.1 | 1.0-1.1 | 80 log | SFX |
| `sfx_neck_alerted` | yes | 1 | 1.07 | -14.0 | 1.1-1.2 | 40 log | SFX |
| `sfx_neck_death` | yes | 2 | 0.76 | -12.2 | 0.7-0.8 | 30 log | SFX |
| `sfx_deer_idle` | yes | 3 | 2.41 | -18.6 | 1.6-1.8 | 100 custom | SFX |
| `sfx_deer_alerted` | yes | 1 | 2.18 | -13.8 | 2.8-3.0 | 50 custom | SFX |
| `sfx_deer_death` | yes | 3 | 1.51 | -8.1 | 1.6-1.8 | 30 custom | SFX |
| `sfx_hare_idle` | yes | 10 | 1.09 | -22.8 | 0.9-1.0 | 40 custom | SFX |
| `sfx_bat_idle` | yes | 2 | 1.70 | -30.2 | 1.0-1.2 | 35 log | SFX |
| `sfx_bat_alerted` | yes | 2 | 1.01 | -13.8 | 1.4-1.6 | 40 log | SFX |
| `sfx_crow_idle` | yes | 6 | 2.91 | -18.9 | 0.9-1.0 | 100 log | SFX |
| `sfx_seagull_idle` | yes | 4 | 0.78 | -20.0 | 0.9-1.0 | 100 log | SFX |
| `sfx_raven_kaw` | yes | 8 | 4.54 | -25.0 | 1.0-1.2 | 20 linear | SFX |

Creature bodies and shared creature sounds:

| prefab | copy | clips | length s | played LUFS | pitch | reach | group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `sfx_greydwarf_attack_hit` (a bite or claw landing; shared by eight creatures) | yes | 2 | 0.30 | -19.9 | 0.8-0.9 | 30 custom | SFX |
| `sfx_troll_attack_hit` (a heavy blow landing) | yes | 1 | 0.79 | -12.4 | 0.9-1.1 | 50 custom | SFX |
| `sfx_troll_footstep` (a heavy creature's step) | yes | 2 | 0.79 | -15.6 | 0.6-0.85 | 50 custom | SFX |
| `sfx_bear_footstep` (a large animal's step) | yes | 10 | 0.93 | -20.3 | 1.0 | 100 custom | SFX |
| `sfx_troll_rock_destroyed` (a boulder shattering, a ground slam) | yes | 3 | 1.39 | -14.5 | 0.8-0.9 | 40 log | SFX |
| `sfx_creature_consume` (a creature eating) | yes | 1 | 0.46 | -26.1 | 0.9-1.1 | 10 log | SFX |
| `sfx_lox_chew1` (a big animal chewing) | yes | 1 | 2.04 | -12.9 | 0.7-0.8 | 30 custom | SFX |
| `sfx_dragon_flap` (big wings) | yes | 3 | 0.58 | -24.1 | 0.7-0.8 | 100 log | SFX_LARGE |
| `sfx_raven_flap` (bird wings) | yes | 5 | 2.27 | -33.0 | 0.8-0.9 | 100 custom | SFX |

## Combat

| prefab | copy | clips | length s | played LUFS | pitch | reach | group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `sfx_sword_swing` | 2 layers | 4 | 0.64 | -25.6 | 0.8-1.0 | 20 linear | SFX |
| `sfx_sword_hit` | 2 layers | 5 | 1.67 | -22.0 | 0.75-0.95 | 25 log | SFX |
| `sfx_knife_swing` | yes | 5 | 0.34 | -39.1 | 0.9-1.1 | 20 linear | SFX |
| `sfx_axe_swing` | yes | 4 | 0.66 | -31.2 | 1.1-1.2 | 20 linear | SFX |
| `sfx_axe_hit` | yes | 5 | 0.47 | -24.3 | 0.95-1.0 | 100 custom | SFX |
| `sfx_battleaxe_swing_wosh` | yes | 4 | 0.94 | -24.6 | 1.0-1.2 | 20 linear | SFX |
| `sfx_battleaxe_hit` | yes | 5 | 0.47 | -23.8 | 0.9-1.0 | 100 custom | SFX |
| `sfx_club_swing` | yes | 4 | 0.46 | -26.8 | 0.85-1.0 | 20 linear | SFX |
| `sfx_club_hit` | yes | 1 | 0.74 | -25.0 | 0.7-1.1 | 50 log | SFX |
| `sfx_sledge_swing` | yes | 3 | 1.75 | -18.6 | 1.0-1.04 | 20 linear | SFX |
| `sfx_sledge_hit` | yes | 3 | 1.89 | -11.5 | 0.85-1.0 | 50 log | SFX |
| `sfx_spear_poke` | yes | 4 | 0.61 | -30.6 | 0.9-1.0 | 30 log | SFX |
| `sfx_spear_throw` | yes | 3 | 0.69 | -32.4 | 0.9-1.0 | 20 linear | SFX |
| `sfx_spear_hit` | yes | 5 | 0.25 | -19.3 | 0.8-1.0 | 20 custom | SFX |
| `sfx_atgeir_attack` | yes | 4 | 0.61 | -28.1 | 0.9-1.0 | 20 linear | SFX |
| `sfx_unarmed_swing` | yes | 4 | 0.80 | -28.2 | 1.0-1.2 | 20 linear | SFX |
| `sfx_unarmed_hit` | yes | 4 | 0.38 | -21.4 | 0.8-1.1 | 20 linear | SFX |
| `sfx_claw_swing` | 2 layers | 4 | 0.64 | -27.0 | 1.3-1.5 | 20 linear | SFX |
| `sfx_bow_draw` | yes | 3 | 2.38 | -33.9 | 1.1-1.4 | 20 log | SFX |
| `sfx_bow_fire` | yes | 3 | 1.00 | -19.3 | 1.0-1.1 | 30 custom | SFX |
| `sfx_arrow_hit` | yes | 3 | 0.25 | -22.7 | 0.9-1.0 | 40 custom | SFX |
| `sfx_arbalest_fire` | yes | 6 | 1.69 | -16.2 | 0.9-1.0 | 30 custom | SFX |
| `sfx_bomb_throw` | yes | 3 | 0.88 | -27.0 | 0.8-0.9 | 20 linear | SFX |
| `sfx_metal_blocked` | 2 layers | 4 | 0.88 | -17.9 | 0.7-0.9 | 25 linear | SFX |
| `sfx_wood_blocked` | 2 layers | 3 | 0.58 | -15.8 | 0.7-0.8 | 25 linear | SFX |
| `sfx_metal_shield_blocked` (its ZSFX volume range is 1 to 8; Unity clamps it to 1) | 2 layers | 3 | 0.88 | -11.8 | 0.8-1.0 | 25 linear | SFX |
| `sfx_perfectblock` | 2 layers | 1 | 1.63 | -13.4 | 0.9-1.05 | 25 linear | SFX |

The shared stingers `fx_crit` (`sfx_crit`) and `fx_backstab` (`sfx_backstab`) are what every creature plays on a
critical hit and a sneak attack: use them as they are.

## Impacts, breaking and trees

| prefab | copy | clips | length s | played LUFS | pitch | reach | group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `sfx_tree_hit` | yes | 7 | 1.38 | -21.3 | 0.8-1.1 | 50 custom | SFX |
| `sfx_wood_hit` | yes | 1 | 1.19 | -19.2 | 1.0-1.2 | 50 custom | SFX |
| `sfx_rock_hit` | yes | 2 | 0.78 | -15.0 | 0.9-1.1 | 30 log | SFX |
| `sfx_ice_hit` | yes | 5 | 1.28 | -21.1 | 0.9-1.1 | 30 custom | SFX |
| `sfx_metalbars_hit` | yes | 6 | 0.93 | -17.7 | 0.85-1.1 | 30 custom | SFX |
| `sfx_skeleton_hit` (bone) | yes | 4 | 0.72 | -18.2 | 0.9-1.1 | 35 custom | SFX |
| `sfx_bush_hit` | yes | 1 | 0.90 | -27.2 | 0.9-1.1 | 20 linear | SFX |
| `sfx_MudHit` (= `sfx_blob_hit`) | yes | 3 | 1.14 | -14.9 | 0.9-1.1 | 30 custom | SFX |
| `sfx_wood_destroyed` | yes | 4 | 2.71 | -14.2 | 0.85-1.1 | 15 linear | SFX |
| `sfx_wood_break` | yes | 4 | 2.71 | -14.2 | 0.9-1.1 | 50 custom | SFX |
| `sfx_rock_destroyed` | yes | 4 | 2.72 | -16.9 | 0.9-1.1 | 40 log | SFX |
| `sfx_ice_destroyed` | yes | 5 | 2.16 | -16.8 | 0.9-1.1 | 50 linear | SFX |
| `sfx_metalbars_break` | yes | 4 | 2.20 | -13.6 | 0.9-1.05 | 45 custom | SFX |
| `sfx_clay_pot_break` | yes | 8 | 1.88 | -19.0 | 0.9-1.1 | 60 custom | SFX |
| `sfx_coins_destroyed` | yes | 4 | 0.85 | -14.4 | 0.95-1.0 | 15 linear | SFX |
| `sfx_MudDestroyed` (= `sfx_GuckSackDestroyed`) | yes | 1 | 0.96 | -14.4 | 0.9-1.1 | 30 custom | SFX |
| `sfx_tree_fall` | yes | 5 | 3.98 | -15.5 | 0.9-1.0 | 60 custom | SFX |
| `sfx_branch_break` | yes | 3 | 3.21 | -14.1 | 0.9-1.05 | 45 custom | SFX |
| `sfx_ship_impact` | yes | 3 | 0.80 | -13.7 | 0.7-1.1 | 50 log | SFX |
| `sfx_ship_destroyed` | yes | 5 | 1.56 | -10.1 | 0.8-0.9 | 70 log | SFX |
| `sfx_cart_hit` | yes | 1 | 0.69 | -15.3 | 0.8-1.2 | 20 log | SFX |

## Building, pieces and stations

| prefab | copy | clips | length s | played LUFS | pitch | reach | group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `sfx_build_hammer_wood` | yes | 5 | 1.27 | -21.4 | 0.8-1.1 | 30 log | SFX |
| `sfx_build_hammer_stone` | yes | 5 | 1.26 | -20.8 | 0.8-1.1 | 30 log | SFX |
| `sfx_build_hammer_metal` | yes | 5 | 1.27 | -22.3 | 0.8-1.1 | 30 log | SFX |
| `sfx_build_hammer_crystal` | yes | 5 | 0.86 | -22.1 | 0.9-1.05 | 30 log | SFX |
| `sfx_build_hammer_default` | yes | 5 | 1.27 | -23.2 | 0.8-1.1 | 30 log | SFX |
| `sfx_build_hoe` | yes | 5 | 0.45 | -24.9 | 0.9-1.0 | 25 log | SFX |
| `sfx_build_cultivator` | yes | 5 | 0.75 | -24.3 | 0.85-0.9 | 25 log | SFX |
| `sfx_door_open` | yes | 1 | 0.74 | -26.8 | 1.1 | 30 custom | SFX |
| `sfx_door_close` | 2 layers | 1 | 0.74 | -26.8 | 1.0 | 30 custom | SFX |
| `sfx_darkwood_door_open` | yes | 3 | 1.41 | -23.0 | 0.95-1.0 | 30 custom | SFX |
| `sfx_metalgate_open` | yes | 1 | 0.93 | -19.1 | 1.1-1.2 | 30 custom | SFX |
| `sfx_chest_open` | yes | 3 | 0.38 | -31.4 | 0.9-1.0 | 30 log | SFX |
| `sfx_chest_close` | yes | 3 | 0.54 | -23.1 | 0.8-1.0 | 30 custom | SFX |
| `sfx_FireAddFuel` | yes | 1 | 1.13 | -10.0 | 0.9-1.1 | 30 log | SFX |
| `sfx_smelter_add` | yes | 1 | 1.13 | -12.0 | 0.9-1.1 | 30 log | SFX |
| `sfx_smelter_produce` | yes | 1 | 2.12 | -13.0 | 0.9-1.1 | 50 custom | SFX |
| `sfx_kiln_produce` | yes | 2 | 1.27 | -11.3 | 0.9-1.1 | 50 custom | SFX |
| `sfx_fermenter_add` | yes | 1 | 2.71 | -14.8 | 0.9-1.1 | 30 log | SFX |
| `sfx_fermenter_tap` | yes | 1 | 2.74 | -15.9 | 0.9-1.1 | 20 log | SFX |
| `sfx_oven_open` | yes | 3 | 0.91 | -22.6 | 0.9-1.0 | 30 log | SFX |
| `sfx_oven_done` | yes | 3 | 4.53 | -21.5 | 0.9-1.0 | 25 linear | SFX |
| `sfx_mill_add` | yes | 1 | 0.57 | -12.6 | 0.9-1.1 | 40 log | SFX |
| `sfx_mill_produce` | yes | 1 | 0.84 | -15.7 | 0.9-1.1 | 50 custom | SFX |
| `sfx_cooking_station_done` | yes | 1 | 2.44 | -22.7 | 0.9-1.0 | 25 linear | SFX |
| `sfx_gui_craftitem_workbench` | yes | 1 | 1.86 | -24.2 | 1.0 | 20 linear | SFX |
| `sfx_gui_craftitem_forge` (loops while crafting) | yes | 1 | 2.00 | -30.1 | 1.0 | 20 linear | SFX |
| `sfx_gui_repairitem_workbench` | yes | 8 | 0.55 | -16.3 | 0.9-1.0 | 20 linear | SFX |
| `sfx_runestone_activate` | yes | 1 | 2.22 | -16.0 | 0.9-1.0 | 50 linear | SFX |
| `sfx_offering` (an altar taking an offering) | yes | 1 | 4.02 | -11.5 | 1.0 | 60 log | SFX |
| `sfx_spawn` (a boss appearing) | yes | 1 | 1.66 | -11.6 | 1.0 | 60 log | SFX |

## Items, the player and the interface

| prefab | copy | clips | length s | played LUFS | pitch | reach | group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `sfx_pickup` (2D) | yes | 3 | 0.56 | -34.2 | 0.85-1.0 | 25 linear | SFX |
| `sfx_drop` (2D) | yes | 1 | 0.53 | -32.9 | 0.8 | 10 linear | SFX |
| `sfx_eat` | yes | 1 | 0.54 | -31.0 | 0.9-1.0 | 10 log | SFX |
| `sfx_drink` | yes | 1 | 0.49 | -22.7 | 0.9-1.0 | 10 log | SFX |
| `sfx_MeadBurp` | 2 layers | 6 | 0.57 | -22.9 | 0.5-0.9 | 25 custom | SFX |
| `sfx_equip` | yes | 4 | 0.84 | -28.6 | 0.9-1.1 | 10 log | SFX |
| `sfx_Potion_health_Start` | yes | 4 | 3.84 | -24.4 | 0.9-1.0 | 25 log | SFX |
| `sfx_Potion_stamina_Start` | yes | 3 | 4.51 | -23.9 | 0.9-1.0 | 25 log | SFX |
| `sfx_levelup` (2D) | yes | 1 | 5.81 | -23.2 | 0.96-1.0 | 20 linear | SFX |
| `sfx_jump` | yes | 3 | 0.70 | -42.4 | 0.8-0.9 | 20 linear | SFX |
| `sfx_dodge` | yes | 4 | 0.88 | -26.0 | 0.85-1.0 | 20 linear | SFX |
| `sfx_hit` (the player hurt) | yes | 5 | 2.45 | -28.3 | 0.8-1.0 | 20 linear | SFX |
| `sfx_pickable_pick` | yes | 1 | 1.41 | -30.1 | 0.8-1.0 | 10 log | SFX |
| `sfx_bones_pick` | yes | 1 | 0.88 | -24.5 | 0.8-1.0 | 10 log | SFX |
| `sfx_lootspawn` | yes | 1 | 1.18 | -12.7 | 1.0 | 10 log | SFX |
| `sfx_gui_button` (2D) | yes | 1 | 0.18 | -29.7 | 1.0 | 50 linear | GUI |
| `sfx_gui_select` (2D) | yes | 1 | 0.18 | -31.3 | 1.0 | 50 linear | GUI |
| `sfx_gui_inventory_open` (2D) | yes | 1 | 0.68 | -44.0 | 1.0 | 50 linear | GUI |
| `sfx_gui_inventory_close` (2D) | yes | 1 | 0.45 | -44.9 | 1.0 | 50 linear | GUI |
| `sfx_gui_sell` (2D) | yes | 4 | 0.62 | -18.2 | 0.9-1.0 | 50 linear | GUI |
| `sfx_achievement_unlocked` (2D) | yes | 1 | 5.66 | -20.1 | 1.0 | 50 linear | GUI |
| `sfx_secretfound` (a bright chime) | yes | 1 | 1.24 | -16.3 | 1.0 | 40 custom | SFX |
| `sfx_gui_biomefound` (2D stinger) | yes | 1 | 4.12 | -14.6 | 1.0 | 50 linear | GUI |

## Magic, explosions and weather

| prefab | copy | clips | length s | played LUFS | pitch | reach | group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `sfx_staff_lightning_fire` | yes | 6 | 4.38 | -8.8 | 1.0 | 80 custom | SFX |
| `sfx_staff_elder_cast` | yes | 5 | 3.56 | -13.5 | 0.9-1.1 | 60 custom | SFX |
| `sfx_stafffrostorbs_cast` | yes | 3 | 3.49 | -11.8 | 0.85-1.15 | 80 custom | SFX |
| `sfx_staffspiritcaller_cast` | yes | 3 | 4.92 | -10.2 | 0.85-1.15 | 120 custom | SFX |
| `sfx_staffthunderblood_thunder` | yes | 4 | 2.90 | -12.8 | 0.85-1.15 | 100 custom | SFX_LARGE |
| `sfx_bombdynamite_explosion` | yes | 4 | 3.83 | -13.2 | 0.95-1.05 | 150 custom | SFX_LARGE |
| `sfx_unstablerock_explosion` | yes | 5 | 3.00 | -9.5 | 0.9-1.1 | 90 custom | SFX |
| `sfx_shieldgenerator_hit` | yes | 6 | 1.79 | -13.1 | 1.0 | 50 log | SFX |
| `sfx_shieldgenerator_startup` | yes | 3 | 4.81 | -12.9 | 0.9-1.1 | 80 custom | SFX |
| `sfx_weapons_lightning_impact` | yes | 5 | 1.12 | -12.7 | 0.8-1.2 | 40 custom | SFX |
| `sfx_weapons_nature_impact` | yes | 5 | 1.25 | -15.1 | 0.8-1.2 | 40 custom | SFX |
| `sfx_weapons_blood_impact` | yes | 5 | 0.81 | -16.0 | 0.8-1.2 | 40 custom | SFX |
| `sfx_WishbonePing_near` | yes | 1 | 0.79 | -20.8 | 1.4 | 25 log | SFX |
| `sfx_thunder` | yes | 12 | 8.41 | -14.6 | 0.8-1.0 | 1,000 linear | SFX_LARGE |
| `sfx_mistlands_thunder` | yes | 8 | 12.25 | -18.9 | 0.9-1.0 | 1,000 linear | SFX_LARGE |

## Loops

| prefab | copy | clips | length s | played LUFS | pitch | reach | group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `sfx_fire_loop` (fires, torches, braziers) | yes | 1 | 10.00 | -32.1 | 0.9-1.1 | 20 custom | SFX |
| `sfx_trollfire_fire_loop` (a burning creature) | yes | 1 | 12.00 | -30.7 | 0.7-1.3 | 100 custom | SFX |
| `sfx_morkhalla_torch_loop` | yes | 3 | 24.00 | -35.9 | 0.9-1.1 | 20 custom | SFX |
| `sfx_EternalPyre_Fire_Loop` | yes | 1 | 46.00 | -31.9 | 1.0 | 40 custom | SFX |
| `sfx_frostcore_idle_loop` (a faint magical hum) | yes | 3 | 12.00 | -42.3 | 0.9-1.1 | 15 custom | SFX |
| `sfx_glowworm_idle_loop` | yes | 3 | 14.00 | -40.8 | 0.9-1.1 | 15 custom | SFX |
| `sfx_shieldgenerator_powered_loop` | yes | 1 | 20.00 | -25.7 | 0.9-1.1 | 35 custom | SFX |
| `sfx_malicious_ice_loop` | yes | 1 | 30.00 | -29.7 | 0.9-1.1 | 55 custom | SFX |
| `sfx_charred_spawner_loop` (a spawner's menace) | yes | 1 | 30.00 | -21.4 | 1.0 | 80 custom | SFX |
| `sfx_elaking_spawner_idle_loop` | yes | 1 | 26.00 | -37.0 | 0.9-1.1 | 25 custom | SFX |
| `sfx_frostfoundry_loop` (a working machine) | yes | 1 | 32.00 | -29.3 | 1.0 | 20 log | SFX |
| `sfx_battering_ram_engine` | yes | 1 | 66.00 | -29.4 | 1.0 | 10 linear | SFX |
| `sfx_prespawn` (an altar charging) | yes | 1 | 3.79 | -15.3 | 1.0 | 50 log | SFX |
| `sfx_ui_player_firedamage_loop` (the player burning) | yes | 1 | 15.00 | -32.3 | 1.0 | 14 custom | SFX |
| `sfx_grapplinghook_pull` | yes | 1 | 6.53 | -37.1 | 0.9-1.1 | 20 log | SFX |

## Footsteps and water

| prefab | copy | clips | length s | played LUFS | pitch | reach | group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `fx_footstep_run` (earth, with dust) | particles | 12 | 0.66 | -37.4 | 0.9-1.0 | 40 log | SFX |
| `fx_footstep_jog` | particles | 12 | 0.56 | -41.2 | 0.9-1.0 | 40 log | SFX |
| `fx_footstep_mud_run` | particles | 18 | 0.58 | -33.8 | 0.8-0.9 | 20 log | SFX |
| `fx_footstep_snow_run` | particles | 10 | 0.65 | -34.0 | 0.9-1.0 | 30 log | SFX |
| `fx_footstep_stone_run` | particles | 12 | 0.52 | -35.4 | 0.9-1.0 | 40 log | SFX |
| `fx_footstep_wood_run` | particles | 12 | 0.58 | -31.3 | 0.8-0.9 | 40 log | SFX |
| `fx_footstep_grass_run` | particles | 20 | 0.58 | -33.4 | 0.8-0.9 | 40 log | SFX |
| `fx_land` | particles | 3 | 0.61 | -27.5 | 0.9-1.0 | 25 linear | SFX |
| `sfx_footstep_water` | yes | 7 | 0.99 | -35.0 | 0.6-0.8 | 30 log | SFX |
| `sfx_land_water` | yes | 3 | 1.28 | -20.5 | 0.6 | 30 linear | SFX |
| `sfx_footstep_swim` | yes | 8 | 2.48 | -26.0 | 0.8-0.9 | 30 log | SFX |
| `sfx_ship_waterimpact` | yes | 4 | 2.72 | -17.2 | 0.9-1.1 | 50 log | SFX |

A new creature's `FootStep` entries point at `fx_footstep_*` prefabs (the player's), or at its own sound prefab for a
heavy creature (`sfx_troll_footstep` at pitch 0.6 to 0.85).

## Ambience and music

Ambience and music are not sound prefabs: `EnvMan` environments name an ambient loop clip, `AudioMan` holds the wind
(`Audio/Ambients/loops/Amb_MainWind_S_Loop.ogg`), the ocean (`Amb_MainOcean_S_Loop.ogg`) and 13 lists of random
ambient clips, and `MusicMan` 46 named tracks (`combat`, `boss_eikthyr`, `meadows`, `location_forest`...). A mod
changes them through those managers, not through prefabs; see [archetypes.md](archetypes.md) for their numbers.
