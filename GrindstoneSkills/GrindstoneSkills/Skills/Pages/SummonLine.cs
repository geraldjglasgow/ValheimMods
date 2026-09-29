using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A staff summon's line on a magic skill's page, from its SpawnAbility as the game applies it to each creature it
    /// spawns: damage times 1 + the caster's level of m_copySkill × m_copySkillToRandomFactor (the creature's random skill
    /// factor), and the last m_levelUpSettings step whose level the caster meets sets the creature's level (its stars) and
    /// how many may follow the caster at once (Tameable sends the oldest beyond that away).
    /// </summary>
    internal static class SummonLine
    {
        /// <summary>"Skeleton 1★, damage +30%", the steps in the tip; nothing when the summon does not grow with the level.</summary>
        public static void Write(SkillPage page, string staff, SpawnAbility summon)
        {
            List<SpawnAbility.LevelUpSettings> steps = Steps(summon);
            float damage = DamageBonus(page, summon);
            if (steps.Count == 0 && damage <= 0f)
                return;
            string name = Creature(summon);
            SpawnAbility.LevelUpSettings step = Reached(page, summon);
            int stars = step != null ? Mathf.Max(0, step.m_setLevel - 1) : 0;
            page.Line(Text(name, stars, damage), name, Tip(staff, summon, steps, step));
        }

        private static string Text(string name, int stars, float damage)
        {
            if (stars <= 0 && damage <= 0f)
                return $"{name}: no stars yet";
            string starText = stars > 0 ? $" {stars}★" : "";
            return damage > 0f ? $"{name}{starText}, damage +{SkillPage.Percent(damage)}" : name + starText;
        }

        /// <summary>The extra damage share at the player's level, as SpawnAbility writes it into the creature's random skill factor.</summary>
        private static float DamageBonus(SkillPage page, SpawnAbility summon)
        {
            if (summon.m_copySkill == Skills.SkillType.None || summon.m_copySkillToRandomFactor <= 0f)
                return 0f;
            return page.Player.GetSkillLevel(summon.m_copySkill) * summon.m_copySkillToRandomFactor;
        }

        /// <summary>The step SpawnAbility applies: the last one listed whose skill level the caster has; null for none.</summary>
        private static SpawnAbility.LevelUpSettings Reached(SkillPage page, SpawnAbility summon)
        {
            if (summon.m_levelUpSettings == null)
                return null;
            for (int i = summon.m_levelUpSettings.Count - 1; i >= 0; i--)
            {
                SpawnAbility.LevelUpSettings step = summon.m_levelUpSettings[i];
                if (step != null && page.Player.GetSkillLevel(step.m_skill) >= step.m_skillLevel)
                    return step;
            }
            return null;
        }

        /// <summary>The summon's level-up steps, lowest level first.</summary>
        private static List<SpawnAbility.LevelUpSettings> Steps(SpawnAbility summon)
        {
            List<SpawnAbility.LevelUpSettings> steps = new List<SpawnAbility.LevelUpSettings>();
            if (summon.m_levelUpSettings != null)
                steps.AddRange(summon.m_levelUpSettings.FindAll(step => step != null));
            steps.Sort((a, b) => a.m_skillLevel.CompareTo(b.m_skillLevel));
            return steps;
        }

        private static string Tip(string staff, SpawnAbility summon, List<SpawnAbility.LevelUpSettings> steps, SpawnAbility.LevelUpSettings step)
        {
            List<string> parts = new List<string> { $"Summoned with the {staff}." };
            string stars = StarSteps(steps);
            if (stars.Length > 0)
                parts.Add(stars);
            if (summon.m_copySkill != Skills.SkillType.None && summon.m_copySkillToRandomFactor > 0f)
                parts.Add($"Damage +{SkillPage.Percent(summon.m_copySkillToRandomFactor * 100f)} at level 100.");
            string follow = Followers(summon, step);
            if (follow.Length > 0)
                parts.Add(follow);
            return string.Join(" ", parts);
        }

        /// <summary>"1★ from level 30, 2★ from 60. A star adds its base health again and half its damage." or "".</summary>
        private static string StarSteps(List<SpawnAbility.LevelUpSettings> steps)
        {
            List<string> texts = new List<string>();
            foreach (SpawnAbility.LevelUpSettings step in steps)
            {
                if (step.m_setLevel > 1)
                    texts.Add($"{step.m_setLevel - 1}★ from level {step.m_skillLevel}");
            }
            return texts.Count == 0 ? "" : string.Join(", ", texts) + ". A star adds its base health again and half its damage.";
        }

        /// <summary>How many may follow the caster at the step reached, or "" when the step sets no limit.</summary>
        private static string Followers(SpawnAbility summon, SpawnAbility.LevelUpSettings step)
        {
            if (step == null)
                return "";
            if (summon.m_setMaxInstancesFromWeaponLevel)
                return "As many follow you at once as the staff's quality level; a new one sends the oldest away.";
            return step.m_maxSpawns > 0 ? $"At most {step.m_maxSpawns} follow you at once; a new one sends the oldest away." : "";
        }

        /// <summary>The summoned creature's name in the player's language (the first of its prefabs).</summary>
        private static string Creature(SpawnAbility summon)
        {
            GameObject prefab = summon.m_spawnPrefab != null && summon.m_spawnPrefab.Length > 0 ? summon.m_spawnPrefab[0] : null;
            if (prefab == null)
                return "Summon";
            Character creature = prefab.GetComponent<Character>();
            return creature != null ? WeaponLines.Name(creature.m_name) : prefab.name;
        }
    }
}
