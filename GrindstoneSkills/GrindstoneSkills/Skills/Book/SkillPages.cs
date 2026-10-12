using System;
using System.Collections.Generic;

namespace GrindstoneSkills
{
    /// <summary>
    /// Which writer fills which skill's page in the info pane (<see cref="SkillBook"/>): one for every skill of the game
    /// (its own effects, from the game code, then what GrindstoneSkills adds) and one for each of the mod's own skills.
    /// Every page ends with the Catch Up or Weapon Switch line while its skill trains faster (<see cref="CatchUpPage"/>).
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
            HookGuard.Run("catch up line", CatchUpPage.Write, page);
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
            add[Skills.SkillType.Swords] = Then(MeleePages.Swords, MeleeMasteryPage.Write);
            add[Skills.SkillType.Knives] = Then(MeleePages.Knives, MeleeMasteryPage.Write);
            add[Skills.SkillType.Clubs] = Then(MeleePages.Clubs, MeleeMasteryPage.Write);
            add[Skills.SkillType.Polearms] = Then(MeleePages.Polearms, MeleeMasteryPage.Write);
            add[Skills.SkillType.Spears] = Then(MeleePages.Spears, MeleeMasteryPage.Write);
            add[Skills.SkillType.Axes] = Then(MeleePages.Axes, MeleeMasteryPage.Write);
            add[Skills.SkillType.Unarmed] = Then(MeleePages.Unarmed, MeleeMasteryPage.Write);
            add[Skills.SkillType.Bows] = Then(RangedPages.Bows, RangedMasteryPage.Write);
            add[Skills.SkillType.Crossbows] = Then(RangedPages.Crossbows, RangedMasteryPage.Write);
            add[Skills.SkillType.ElementalMagic] = Then(MagicPages.ElementalMagic, RangedMasteryPage.Write);
            add[Skills.SkillType.BloodMagic] = Then(MagicPages.BloodMagic, RangedMasteryPage.Write);
        }

        private static void AddBody(Dictionary<Skills.SkillType, Action<SkillPage>> add)
        {
            add[Skills.SkillType.Blocking] = BlockingPage.Write;
            add[Skills.SkillType.Run] = Then(MovementPages.Run, RunningPage.Write);
            add[Skills.SkillType.Swim] = Then(MovementPages.Swim, SwimmingPage.Write);
            add[Skills.SkillType.Jump] = Then(MovementPages.Jump, JumpingPage.Write);
            add[Skills.SkillType.Sneak] = Then(MovementPages.Sneak, SneakingPage.Write);
            add[Skills.SkillType.Dodge] = Then(MovementPages.Dodge, DodgingPage.Write);
            add[Skills.SkillType.Ride] = Then(MovementPages.Ride, RidingPage.Write);
            add[Skills.SkillType.Crafting] = Then(CraftingPage.Write, CraftingPerksPage.Write);
        }

        /// <summary>The game's own page for a skill, then what GrindstoneSkills adds to it.</summary>
        private static Action<SkillPage> Then(Action<SkillPage> game, Action<SkillPage> added) => page =>
        {
            game(page);
            added(page);
        };

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
