// Written by AssetWorkshop/assets/ecp_headsman/sfx_table.py from the preview's sound recipes (sfx.py,
// blender_fx.SOUNDS); change those and run build.ps1, not this file.
using System.Collections.Generic;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// The Crypt Executioner's sound cues as the preview mixed them: each cue's variants, each the layers of game
    /// clips played together (<see cref="HeadsmanSoundLayer"/>). One factor (2.25) brings them all to the level
    /// the game plays its own skeleton's voice at.
    /// </summary>
    public static class HeadsmanSoundTable
    {
        public static readonly Dictionary<string, HeadsmanSoundLayer[][]> Cues = new Dictionary<string, HeadsmanSoundLayer[][]>
        {
            ["vocal"] = new[]
            {
                new[] { L("Enemy_Skeleton_Basic_Verse_Attack_01", 0f, 0f, 0.76f, 120f, 0f, 1f, 2f, false) },
                new[] { L("Enemy_Skeleton_Basic_Verse_Attack_03", 0f, 0f, 0.78f, 120f, 0f, 1f, 2f, false) },
                new[] { L("Enemy_Skeleton_Basic_Verse_Attack_05", 0f, 0f, 0.859f, 120f, 0f, 1f, 2f, false) },
                new[] { L("Enemy_Skeleton_Basic_Verse_Attack_07", 0f, 0f, 0.811f, 120f, 0f, 1f, 2f, false) },
                new[] { L("Enemy_Skeleton_Basic_Verse_Attack_02", 0f, 0f, 0.798f, 120f, 0f, 1f, 2f, false) },
                new[] { L("Enemy_Skeleton_Basic_Verse_Attack_04", 0f, 0f, 0.727f, 120f, 0f, 1f, 2f, false) },
                new[] { L("Enemy_Skeleton_Basic_Verse_Attack_06", 0f, 0f, 0.866f, 120f, 0f, 1f, 2f, false) },
            },
            ["vocal_raise"] = new[]
            {
                new[] { L("Enemy_Skeleton_Hildir_Attack_Skill_01", 0f, 0f, 0.348f, 220f, 0f, 1f, 2f, false) },
                new[] { L("Enemy_Skeleton_Hildir_Attack_Skill_02", 0f, 0f, 0.33f, 220f, 0f, 1f, 2f, false) },
            },
            ["creak"] = new[]
            {
                new[] { L("Enemy_Skeleton_Basic_Verse_Idle_02", 0f, 0f, 0.773f, 150f, 0f, 1f, 1.2f, false) },
                new[] { L("Enemy_Skeleton_Basic_Verse_Idle_05", 0f, 0f, 0.919f, 150f, 0f, 1f, 1.2f, false) },
            },
            ["whoosh_heavy"] = new[]
            {
                new[] { L("Enemy_Skeleton_Basic_Attack_Melee_01", 0.416f, 0f, 0.283f, 90f, 0f, 1f, 0.32f, true) },
                new[] { L("Enemy_Skeleton_Basic_Attack_Melee_02", 0.294f, 0f, 0.255f, 90f, 0f, 1f, 0.32f, true) },
                new[] { L("Enemy_Skeleton_Basic_Attack_Melee_03", 0.292f, 0f, 0.262f, 90f, 0f, 1f, 0.32f, true) },
            },
            ["whoosh_short"] = new[]
            {
                new[] { L("Enemy_Skeleton_Basic_Attack_Melee_01", 0.416f, 0f, 0.283f, 90f, 0f, 1f, 0.3f, true) },
                new[] { L("Enemy_Skeleton_Basic_Attack_Melee_02", 0.294f, 0f, 0.255f, 90f, 0f, 1f, 0.3f, true) },
                new[] { L("Enemy_Skeleton_Basic_Attack_Melee_03", 0.292f, 0f, 0.267f, 90f, 0f, 1f, 0.3f, true) },
            },
            ["whoosh_spin"] = new[]
            {
                new[] { L("Enemy_Skeleton_DeepNorth_Attack_Melee_01", 0f, 0.142f, 0.313f, 110f, 0f, 1f, 0.5f, false) },
                new[] { L("Enemy_Skeleton_DeepNorth_Attack_Melee_02", 0f, 0.252f, 0.337f, 110f, 0f, 1f, 0.5f, false) },
                new[] { L("Enemy_Skeleton_DeepNorth_Attack_Melee_03", 0f, 0.12f, 0.316f, 110f, 0f, 1f, 0.5f, false) },
            },
            ["throw"] = new[]
            {
                new[] { L("Player_Movement_SpearThrow_M_01", 0.147f, 0f, 1f, 120f, 0f, 1f, 1f, true) },
                new[] { L("Player_Movement_SpearThrow_M_02", 0.138f, 0f, 1f, 120f, 0f, 1f, 1f, true) },
                new[] { L("Player_Movement_SpearThrow_M_03", 0.135f, 0f, 1f, 120f, 0f, 1f, 1f, true) },
            },
            ["g_swing"] = new[]
            {
                new[] { L("Player_Movement_BattleAxe_Swing_01", 0f, 0f, 1f, 240f, 0f, 1f, 0.8f, false) },
                new[] { L("Player_Movement_BattleAxe_Swing_02", 0f, 0f, 1f, 240f, 0f, 1f, 0.8f, false) },
                new[] { L("Player_Movement_BattleAxe_Swing_03", 0f, 0f, 1f, 240f, 0f, 1f, 0.8f, false) },
                new[] { L("Player_Movement_BattleAxe_Swing_04", 0f, 0f, 1f, 240f, 0f, 1f, 0.8f, false) },
            },
            ["g_spin"] = new[]
            {
                new[] { L("Enemy_Skeleton_DeepNorth_Attack_Melee_01", 0.098f, 0f, 0.357f, 110f, 0f, 1f, 0.28f, true) },
                new[] { L("Enemy_Skeleton_DeepNorth_Attack_Melee_02", 0f, 0.012f, 0.578f, 110f, 0f, 1f, 0.28f, false) },
                new[] { L("Enemy_Skeleton_DeepNorth_Attack_Melee_03", 0.12f, 0f, 0.361f, 110f, 0f, 1f, 0.28f, true) },
            },
            ["g_overhead"] = new[]
            {
                new[] { L("Player_Movement_SledgeSwing_M_01", 0f, 0f, 1f, 240f, 0f, 1f, 0.8f, false) },
                new[] { L("Player_Movement_SledgeSwing_M_02", 0f, 0f, 1f, 240f, 0f, 1f, 0.8f, false) },
                new[] { L("Player_Movement_SledgeSwing_M_03", 0f, 0f, 1f, 240f, 0f, 1f, 0.8f, false) },
            },
            ["impact_ground"] = new[]
            {
                new[] { L("Player_Movement_Axe_Hit_M_01", 0.052f, 0f, 0.924f, 130f, 0f, 1f, 1f, true), L("UI_Hoe_04", 0.058f, 0f, 0.692f, 120f, 0f, 1f, 0.35f, true) },
                new[] { L("Player_Movement_Axe_Hit_M_03", 0.052f, 0f, 0.949f, 130f, 0f, 1f, 1f, true), L("UI_Hoe_05", 0.046f, 0f, 0.46f, 120f, 0f, 1f, 0.35f, true) },
                new[] { L("Player_Movement_Axe_Hit_M_05", 0.048f, 0f, 1f, 130f, 0f, 1f, 1f, true), L("UI_Hoe_01", 0.049f, 0f, 0.858f, 120f, 0f, 1f, 0.35f, true) },
            },
            ["impact_hit"] = new[]
            {
                new[] { L("Player_Movement_Axe_Hit_M_02", 0.053f, 0f, 0.907f, 150f, 0f, 1f, 1f, true) },
                new[] { L("Player_Movement_Axe_Hit_M_04", 0.04f, 0f, 0.722f, 150f, 0f, 1f, 1f, true) },
                new[] { L("Player_Movement_Axe_Hit_M_05", 0.048f, 0f, 0.913f, 150f, 0f, 1f, 1f, true) },
            },
            ["grind"] = new[]
            {
                new[] { L("UI_Hoe_01", 0.059f, 0f, 0.61f, 150f, 0f, 1f, 1f, true), L("UI_Cultivator_01", 0f, 0.03f, 1f, 150f, 4500f, 0.85f, 0.5f, false) },
                new[] { L("UI_Hoe_03", 0.056f, 0f, 0.481f, 150f, 0f, 1f, 1f, true), L("UI_Cultivator_02", 0f, 0.03f, 1f, 150f, 4500f, 0.85f, 0.5f, false) },
                new[] { L("UI_Hoe_02", 0.03f, 0f, 0.65f, 150f, 0f, 1f, 1f, true), L("UI_Cultivator_03", 0f, 0.03f, 1f, 150f, 4500f, 0.85f, 0.5f, false) },
            },
            ["rumble"] = new[]
            {
                new[] { L("Enemy_Father_Fissure_Pillar_04", 0.112f, 0f, 0.625f, 150f, 0f, 1f, 0.8f, true), L("Enemy_Father_Fissure_Pillar_05", 0f, 0.3f, 0.341f, 170f, 0f, 1f, 0.7f, false), L("Amb_WindingTunnel_OneShots_RockCrumble_04", 0f, 0.11f, 1f, 100f, 0f, 1f, 1f, false) },
                new[] { L("Enemy_Father_Fissure_Pillar_05", 0f, 0.075f, 0.43f, 150f, 0f, 1f, 0.8f, false), L("Enemy_Father_Fissure_Pillar_03", 0f, 0.3f, 0.233f, 170f, 0f, 1f, 0.7f, false), L("Amb_WindingTunnel_OneShots_RockCrumble_02", 0.352f, 0f, 1f, 100f, 0f, 1f, 1f, true) },
                new[] { L("Enemy_Father_Fissure_Pillar_03", 0f, 0.023f, 0.476f, 150f, 0f, 1f, 0.8f, false), L("Enemy_Father_Fissure_Pillar_04", 0f, 0.3f, 0.268f, 170f, 0f, 1f, 0.7f, false), L("Amb_WindingTunnel_OneShots_RockCrumble_03", 0.844f, 0f, 1f, 100f, 0f, 1f, 1f, true) },
            },
            ["shatter"] = new[]
            {
                new[] { L("Skeleton_Hit_Shatter1", 0.065f, 0f, 0.43f, 150f, 0f, 1f, 1.5f, true), L("Skeleton_Death_BoneHit1", 0f, 0.12f, 0.232f, 150f, 0f, 1f, 1.38f, false), L("Skeleton_BonesRattle", 0f, 0.22f, 0.566f, 150f, 0f, 1f, 0.7f, false) },
                new[] { L("Skeleton_Hit_Shatter2", 0.012f, 0f, 0.436f, 150f, 0f, 1f, 1.5f, true), L("Skeleton_Death_BoneHit2", 0f, 0.12f, 0.241f, 150f, 0f, 1f, 1.38f, false), L("Skeleton_BonesRattle", 0f, 0.22f, 0.568f, 150f, 0f, 1f, 0.7f, false) },
                new[] { L("Skeleton_Hit_Shatter1", 0.065f, 0f, 0.43f, 150f, 0f, 1f, 1.5f, true), L("Skeleton_Death_BoneHit3", 0f, 0.12f, 0.267f, 150f, 0f, 1f, 1.38f, false), L("Skeleton_BonesRattle", 0f, 0.22f, 0.566f, 150f, 0f, 1f, 0.7f, false) },
            },
            ["regen"] = new[]
            {
                new[] { L("Enemy_Dverger_ChargeUp_Big_01", 0f, 0f, 1f, 0f, 0f, 1f, 2f, false) },
            },
            ["solid"] = new[]
            {
                new[] { L("Enemy_Father_CharredSummon_Impact_01", 0f, 0f, 1f, 0f, 0f, 1f, 1.2f, false) },
                new[] { L("Enemy_Father_CharredSummon_Impact_02", 0f, 0f, 1f, 0f, 0f, 1f, 1.2f, false) },
            },
        };

        private static HeadsmanSoundLayer L(string clip, float start, float delay, float volume, float low, float high, float pitch, float longest, bool fadeIn) =>
            new HeadsmanSoundLayer(clip, start, delay, volume, low, high, pitch, longest, fadeIn);
    }
}
