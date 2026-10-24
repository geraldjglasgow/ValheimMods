using BepInEx.Configuration;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sailing's page in the info pane: ship health, helm speed and the map reveal at the player's level, how the skill
    /// trains, and the Wind Call, lookout and Fog Lift milestones. The numbers are the synced settings scaled by the level exactly as the
    /// features scale them (<see cref="ShipwrightHealth.Factor"/>, <see cref="SailingSkill.Share"/>). The abilities'
    /// names are the Sailing feature's own words (gs_sail_ability_*).
    /// </summary>
    public static class SailingPage
    {
        /// <summary>The game's map exploration radius, used while the map is not loaded.</summary>
        private const float GameExploreRadius = 100f;

        public static void Write(SkillPage page)
        {
            page.About = Lang.Get("gs_page_sailing_about");
            if (!SailingSkill.Active)
            {
                page.Line(Lang.Get("gs_page_sailing_off"));
                return;
            }
            Ships(page);
            Training(page);
            WindCall(page);
            Lookout(page);
            FogLift(page);
        }

        private static void Ships(SkillPage page)
        {
            float level = page.Level;
            string built = Lang.Get("gs_page_sailing_built");
            page.Line(Lang.Format("gs_page_sailing_built_line", built, SkillPage.Percent(ShipwrightHealth.Factor(level) - 1f)), built,
                Lang.Get("gs_page_sailing_built_tip"));
            string helm = Lang.Get("gs_page_sailing_helm");
            page.Line(Lang.Format("gs_page_sailing_helm_line", helm, Share(SailingSettings.ShipSpeed.Value, level)), helm,
                Lang.Get("gs_page_sailing_helm_tip"));
            float share = SailingSkill.Share(SailingSettings.ExploreRadius.Value, level);
            float ashore = ExploreRadius();
            string reveal = Lang.Get("gs_page_sailing_reveal");
            page.Line(Lang.Format("gs_page_sailing_reveal_line", reveal, (ashore * (1f + share)).ToString("0"), SkillPage.Percent(share)), reveal,
                Lang.Format("gs_page_sailing_reveal_tip", ashore.ToString("0")));
        }

        private static void Training(SkillPage page)
        {
            float perKilometre = Mathf.Max(0f, SailingSettings.HelmExperience.Value);
            if (perKilometre <= 0f)
                return;
            float crew = Mathf.Clamp01(SailingSettings.CrewShare.Value / 100f);
            string tip = crew > 0f
                ? Lang.Format("gs_page_sailing_xp_tip_crew", SkillPage.Percent(crew))
                : Lang.Get("gs_page_sailing_xp_tip_nocrew");
            string experience = Lang.Get("gs_page_sailing_xp");
            page.Line(Lang.Format("gs_page_sailing_xp_line", experience, SkillPage.Number(perKilometre)), experience, tip);
        }

        private static void WindCall(SkillPage page)
        {
            string tip = Lang.Format("gs_page_sailing_windcall_tip", KeyText(WindCallSettings.Key),
                SkillPage.Duration(WindCallSettings.Duration.Value));
            page.Perk(Lang.Get("gs_sail_ability_windcall"), WindCallSettings.Level.Value, tip + Again(WindCallSettings.Cooldown.Value));
        }

        private static void Lookout(SkillPage page)
        {
            string tip = Lang.Format("gs_page_sailing_lookout_tip", KeyText(LookoutSettings.Key),
                LookoutSettings.Radius.Value.ToString("0"), SkillPage.Duration(LookoutSettings.Duration.Value));
            page.Perk(Lang.Get("gs_sail_ability_lookout"), LookoutSettings.Level.Value, tip + Again(LookoutSettings.Cooldown.Value));
        }

        private static void FogLift(SkillPage page)
        {
            string tip = Lang.Format("gs_page_sailing_foglift_tip", KeyText(SailingSettings.FogLiftKey), SkillPage.Percent(FogLiftView.Thinner),
                FogLiftMist.Radius.ToString("0"), SkillPage.Duration(FogLiftInput.Duration));
            page.Perk(Lang.Get("gs_sail_ability_foglift"), FogLiftInput.Level, tip + Again(FogLiftInput.Cooldown));
        }

        /// <summary>" Once every 5 min." after a tip, or "" without a cooldown.</summary>
        private static string Again(float cooldown) =>
            cooldown > 0f ? " " + Lang.Format("gs_page_sailing_again", SkillPage.Duration(cooldown)) : "";

        /// <summary>The ability's key, or "the Wind Call Key (not set)": the .cfg setting's own name, which stays English.</summary>
        private static string KeyText(ConfigEntry<KeyboardShortcut> entry)
        {
            KeyboardShortcut key = entry.Value;
            return key.MainKey == KeyCode.None ? Lang.Format("gs_page_sailing_key_unset", entry.Definition.Key) : key.ToString();
        }

        private static float ExploreRadius() => Minimap.instance != null ? Minimap.instance.m_exploreRadius : GameExploreRadius;

        private static string Share(float percentAt100, float level) => SkillPage.Percent(SailingSkill.Share(percentAt100, level));
    }
}
