using System;
using System.Collections.Generic;

namespace GrindstoneSkills
{
    /// <summary>
    /// Which writer fills which skill's page in the info pane (<see cref="SkillBook"/>): one for every skill of the game
    /// (its own effects, from the game code, plus what GrindstoneSkills adds) and one for each of the mod's own skills.
    /// Writers live in <c>Skills/Pages</c>. A skill with no writer, such as another mod's, shows its own description; a
    /// writer that throws is logged and the page keeps what it wrote.
    /// </summary>
    public static class SkillPages
    {
        private static Dictionary<Skills.SkillType, Action<SkillPage>> writers;

        /// <summary>The local player's page for one skill of the panel, written now.</summary>
        internal static SkillPage For(Player player, Skills.SkillDef skill)
        {
            SkillPage page = new SkillPage(player, skill.m_skill, skill.m_description ?? "");
            if (Writers().TryGetValue(skill.m_skill, out Action<SkillPage> write))
                HookGuard.Run("skill page " + skill.m_skill, () => write(page));
            return page;
        }

        private static Dictionary<Skills.SkillType, Action<SkillPage>> Writers()
        {
            if (writers != null)
                return writers;
            writers = new Dictionary<Skills.SkillType, Action<SkillPage>>();
            AddWeapons(writers);
            AddBody(writers);
            AddModules(writers);
            return writers;
        }

        private static void AddWeapons(Dictionary<Skills.SkillType, Action<SkillPage>> add)
        {
            add[Skills.SkillType.Swords] = MeleePages.Swords;
            add[Skills.SkillType.Knives] = MeleePages.Knives;
            add[Skills.SkillType.Clubs] = MeleePages.Clubs;
            add[Skills.SkillType.Polearms] = MeleePages.Polearms;
            add[Skills.SkillType.Spears] = MeleePages.Spears;
            add[Skills.SkillType.Axes] = MeleePages.Axes;
            add[Skills.SkillType.Unarmed] = MeleePages.Unarmed;
            add[Skills.SkillType.Bows] = RangedPages.Bows;
            add[Skills.SkillType.Crossbows] = RangedPages.Crossbows;
            add[Skills.SkillType.ElementalMagic] = MagicPages.ElementalMagic;
            add[Skills.SkillType.BloodMagic] = MagicPages.BloodMagic;
        }

        private static void AddBody(Dictionary<Skills.SkillType, Action<SkillPage>> add)
        {
            add[Skills.SkillType.Blocking] = BlockingPage.Write;
            add[Skills.SkillType.Run] = MovementPages.Run;
            add[Skills.SkillType.Swim] = MovementPages.Swim;
            add[Skills.SkillType.Jump] = MovementPages.Jump;
            add[Skills.SkillType.Sneak] = MovementPages.Sneak;
            add[Skills.SkillType.Dodge] = MovementPages.Dodge;
            add[Skills.SkillType.Ride] = MovementPages.Ride;
            add[Skills.SkillType.Crafting] = CraftingPage.Write;
        }

        private static void AddModules(Dictionary<Skills.SkillType, Action<SkillPage>> add)
        {
            add[Skills.SkillType.Cooking] = CookingPage.Write;
            add[Skills.SkillType.WoodCutting] = WoodcuttingPage.Write;
            add[Skills.SkillType.Pickaxes] = PickaxesPage.Write;
            add[Skills.SkillType.Fishing] = FishingPage.Write;
            add[Skills.SkillType.Farming] = FarmingPage.Write;
            add[DefenseSkill.Type] = DefensePage.Write;
            add[SailingSkill.Type] = SailingPage.Write;
            add[HusbandrySkill.Type] = HusbandryPage.Write;
            add[ForagingSkill.Type] = ForagingPage.Write;
        }
    }
}
