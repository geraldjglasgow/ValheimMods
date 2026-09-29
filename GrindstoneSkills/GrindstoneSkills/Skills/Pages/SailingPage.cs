using BepInEx.Configuration;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sailing's page in the info pane: ship health, helm speed and the map reveal at the player's level, how the skill
    /// trains, and the lookout milestone. The numbers are the synced settings scaled by the level exactly as the
    /// features scale them (<see cref="ShipwrightHealth.Factor"/>, <see cref="SailingSkill.Share"/>).
    /// </summary>
    public static class SailingPage
    {
        /// <summary>The game's map exploration radius, used while the map is not loaded.</summary>
        private const float GameExploreRadius = 100f;

        public static void Write(SkillPage page)
        {
            page.About = "Tougher, faster ships. Trained by sailing them.";
            if (!SailingSkill.Active)
            {
                page.Line("Turned off on this server.");
                return;
            }
            Ships(page);
            Training(page);
            Lookout(page);
        }

        private static void Ships(SkillPage page)
        {
            float level = page.Level;
            page.Line($"Ships you build: health +{SkillPage.Percent(ShipwrightHealth.Factor(level) - 1f)}", "Ships you build",
                "Fixed by your level when you place the ship. Ships built before keep their health.");
            page.Line($"Ship speed at the helm +{Share(SailingSettings.ShipSpeed.Value, level)}", "at the helm",
                "Top speed of any ship you steer, under sail and at the oars.");
            float share = SailingSkill.Share(SailingSettings.ExploreRadius.Value, level);
            float ashore = ExploreRadius();
            page.Line($"Map reveal aboard {ashore * (1f + share):0} m (+{SkillPage.Percent(share)})", "Map reveal",
                $"How far around you the map uncovers while you are aboard a ship. Ashore it is {ashore:0} m.");
        }

        private static void Training(SkillPage page)
        {
            float perKilometre = Mathf.Max(0f, SailingSettings.HelmExperience.Value);
            if (perKilometre <= 0f)
                return;
            float crew = Mathf.Clamp01(SailingSettings.CrewShare.Value / 100f);
            string crewText = crew > 0f ? $"Crew aboard earn {SkillPage.Percent(crew)} of it" : "Crew aboard earn none";
            page.Line($"Experience {SkillPage.Number(perKilometre)} per km at the helm", "Experience",
                $"For the distance a ship moves while you steer it. {crewText}; a ship nobody steers earns nothing.");
        }

        private static void Lookout(SkillPage page)
        {
            float cooldown = LookoutSettings.Cooldown.Value;
            string again = cooldown > 0f ? $" Once every {SkillPage.Duration(cooldown)}." : "";
            page.Perk("Lookout", LookoutSettings.Level.Value,
                $"Press {KeyText()} aboard a ship: everyone aboard sees the name tags of enemies (not bosses) within {LookoutSettings.Radius.Value:0} m for {SkillPage.Duration(LookoutSettings.Duration.Value)}.{again}");
        }

        private static string KeyText()
        {
            KeyboardShortcut key = LookoutSettings.Key.Value;
            return key.MainKey == KeyCode.None ? "the Lookout Key (not set)" : key.ToString();
        }

        private static float ExploreRadius() => Minimap.instance != null ? Minimap.instance.m_exploreRadius : GameExploreRadius;

        private static string Share(float percentAt100, float level) => SkillPage.Percent(SailingSkill.Share(percentAt100, level));
    }
}
