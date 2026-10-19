using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// Crafting's page in the info pane: what the Crafting perks (section 42) add at the player's level, with the numbers
    /// at that level and at 100, from the same constants the perks use. Written on the local player's client as the page
    /// opens, in the player's language. Nothing while the module is off.
    /// </summary>
    public static class CraftingPerksPage
    {
        public static void Write(SkillPage page)
        {
            if (!CraftingPerks.Enabled)
                return;
            float level = page.Level;
            page.Perk(Lang.Get("gs_perk_makersmark"), 0f, Lang.Format("gs_craft_makersmark_tip",
                SkillPage.Percent(MakersMark.Share(level)), SkillPage.Percent(MakersMark.Share(100f))));
            page.Perk(Lang.Get("gs_perk_sturdybuild"), 0f, Lang.Format("gs_craft_sturdybuild_tip",
                SkillPage.Percent(SturdyBuild.Share(level)), SkillPage.Percent(SturdyBuild.Share(100f))));
            page.Perk(Lang.Get("gs_perk_thrifty"), 0f, Lang.Format("gs_craft_thrifty_tip",
                SkillPage.Percent(Thrifty.Chance(level)), SkillPage.Percent(Thrifty.Chance(100f))));
            page.Perk(Lang.Get("gs_perk_journeyman"), Journeyman.Level, Lang.Get("gs_craft_journeyman_tip"));
        }
    }
}
