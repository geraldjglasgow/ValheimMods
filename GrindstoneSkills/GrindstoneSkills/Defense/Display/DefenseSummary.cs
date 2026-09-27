using System.Text;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Defense plate's tooltip (<see cref="DefensePlate"/>): every Defense bonus at the local player's level, then
    /// each milestone with the level it needs. Values are the synced settings scaled by the level, as the features use
    /// them.
    /// </summary>
    public static class DefenseSummary
    {
        public static string Topic() => $"Defense {Mathf.FloorToInt(DefenseSkill.Local())}";

        public static string Tip()
        {
            float level = DefenseSkill.Local();
            StringBuilder text = new StringBuilder();
            Perks(text, level);
            Guard(text, level);
            text.Append('\n');
            Milestones(text, level);
            return text.ToString().TrimEnd();
        }

        private static void Perks(StringBuilder text, float level)
        {
            text.Append($"Max health +{Vitality.BonusHealth(level):0.#}\n");
            text.Append($"Food health +{Percent(DefenseSettings.FoodHealth.Value, level)}\n");
            text.Append($"Damage taken -{Percent(DefenseSettings.DamageReduction.Value, level)}\n");
            text.Append($"Out of combat: {Percent(DefenseSettings.Regeneration.Value, level)} of max health every {DefenseSettings.RegenerationInterval.Value:0.#} s\n");
            text.Append($"Poise +{Percent(DefenseSettings.Poise.Value, level)}\n");
            text.Append($"Parry window +{DefenseSettings.ParryWindow.Value * DefenseSkill.Factor(level):0.00} s\n");
            text.Append($"Block stamina -{Percent(DefenseSettings.BlockStamina.Value, level)}, dodge stamina -{Percent(DefenseSettings.DodgeStamina.Value, level)}\n");
        }

        private static void Guard(StringBuilder text, float level)
        {
            text.Append($"Reflex {Percent(DefenseGuardSettings.ReflexChance.Value, level)}, shield bash {Percent(DefenseGuardSettings.BashChance.Value, level)}, thorns {Percent(DefenseGuardSettings.Thorns.Value, level)}\n");
            text.Append($"Adrenaline +{Percent(DefenseGuardSettings.Adrenaline.Value, level)}, block wear -{Percent(DefenseGuardSettings.ShieldWear.Value, level)}, knockback -{Percent(DefenseGuardSettings.Knockback.Value, level)}\n");
            if (DefenseGuardSettings.DesperationHealth.Value > 0f)
                text.Append($"Below {DefenseGuardSettings.DesperationHealth.Value:0}% health: damage reduction x{DefenseGuardSettings.DesperationMultiplier.Value:0.#}\n");
        }

        private static void Milestones(StringBuilder text, float level)
        {
            Milestone(text, "Riposte", DefenseMilestoneSettings.RiposteLevel.Value, level);
            Milestone(text, "Shield Wall", DefenseMilestoneSettings.ShieldWallLevel.Value, level);
            Milestone(text, "Hardened", DefenseMilestoneSettings.HardenedLevel.Value, level);
            Milestone(text, "Last Stand", DefenseMilestoneSettings.LastStandLevel.Value, level);
        }

        private static void Milestone(StringBuilder text, string name, float milestone, float level)
        {
            if (milestone > DefenseSkill.MaxLevel)
                return;
            string state = DefenseSkill.Reached(level, milestone) ? "unlocked" : $"at level {milestone:0}";
            text.Append($"{name}: {state}\n");
        }

        private static string Percent(float percentAt100, float level) => $"{DefenseSkill.Share(percentAt100, level) * 100f:0.#}%";
    }
}
