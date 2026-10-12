namespace GrindstoneSkills
{
    /// <summary>
    /// Crafting's page in the info pane: what the Crafting perks (section 42) add at the player's level, with the numbers
    /// at that level and at 100, from the same constants the perks use. Written on the local player's client as the page
    /// opens. Nothing while the module is off.
    /// </summary>
    public static class CraftingPerksPage
    {
        public static void Write(SkillPage page)
        {
            if (!CraftingPerks.Enabled)
                return;
            float level = page.Level;
            page.Perk("Maker's Mark", 0f,
                $"Weapons, armour, shields and tools you craft or upgrade at a station that trains Crafting get +{SkillPage.Percent(MakersMark.Share(level))} max durability (+{SkillPage.Percent(MakersMark.Share(100f))} at level 100). The mark stays on the item for whoever uses it; an upgrade marks it again with the upgrader's level.");
            page.Perk("Sturdy Build", 0f,
                $"Pieces you place with the hammer get +{SkillPage.Percent(SturdyBuild.Share(level))} max health (+{SkillPage.Percent(SturdyBuild.Share(100f))} at level 100), kept on the piece for everyone. Ships keep Sailing's health.");
            page.Perk("Thrifty", 0f,
                $"Each craft or upgrade at the workbench, forge or another station that trains Crafting has a {SkillPage.Percent(Thrifty.Chance(level))} chance ({SkillPage.Percent(Thrifty.Chance(100f))} at level 100) to give back one of the materials it used. Building never does: a removed piece already gives its full cost back.");
            page.Perk("Journeyman", Journeyman.Level,
                "The workbench and the forge count one level higher for you: craft, upgrade and repair what needs one station level more than they have. New recipes still appear at the station levels you have seen.");
        }
    }
}
