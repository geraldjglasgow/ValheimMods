namespace GrindstoneSkills
{
    /// <summary>
    /// The lines the game's weapon skills share on their pages, from the game's own formulas (GrindstoneSkills leaves
    /// these skills as the game has them): the share of a weapon's damage a hit rolls (Skills.GetRandomSkillRange: 25–55%
    /// at level 0 up to 85–100% at 100) and the cut in what an attack costs (Attack.GetAttackStamina, GetAttackEitr and
    /// GetAttackHealth, ItemData.GetDrawStaminaDrain: 33% at level 100, linear from 0).
    /// </summary>
    internal static class WeaponLines
    {
        /// <summary>The cut in an attack's stamina, eitr and health cost, and in a bow's draw drain, at level 100.</summary>
        public const float CostCut = 0.33f;

        /// <summary>A melee skill's page: what it is, the damage a hit rolls and the attack stamina.</summary>
        public static void Melee(SkillPage page, string about)
        {
            page.About = about;
            Damage(page, "the weapon's", "");
            Cost(page, "Attack stamina", "Any eitr or health an attack costs drops by the same share.");
        }

        /// <summary>"Damage 47–77% of the weapon's": the range a hit rolls in at this level, the range at 100 in the tip.</summary>
        public static void Damage(SkillPage page, string whose, string more)
        {
            Roll(page, out float low, out float high);
            page.Line($"Damage {SkillPage.Range(low, high)} of {whose}", "Damage",
                $"Each hit rolls its damage and knockback in this share of the full amount: the yellow range on the weapon's tooltip. At 100: 85–100%.{more}");
        }

        /// <summary>"Attack stamina -12.2%": a cost the skill cuts by up to <see cref="CostCut"/>, the thing itself explained on hover.</summary>
        public static void Cost(SkillPage page, string what, string more)
        {
            page.Line($"{what} -{SkillPage.Percent(CostCut * page.Factor)}", what, $"At 100: -{SkillPage.Percent(CostCut)}. {more}");
        }

        /// <summary>The share of a weapon's damage a hit rolls at the player's level, lowest and highest: the game's own range.</summary>
        public static void Roll(SkillPage page, out float low, out float high) =>
            page.Player.GetSkills().GetRandomSkillRange(out low, out high, page.Type);

        /// <summary>The weapon in the player's hands when it trains the page's skill; otherwise null.</summary>
        public static ItemDrop.ItemData Held(SkillPage page)
        {
            ItemDrop.ItemData weapon = page.Player.GetCurrentWeapon();
            return weapon != null && weapon.m_shared.m_skillType == page.Type ? weapon : null;
        }

        /// <summary>A game token ("$item_bow") in the player's language.</summary>
        public static string Name(string token) =>
            Localization.instance != null ? Localization.instance.Localize(token) : token;

        /// <summary>A short time with up to two decimals: "0.64 s".</summary>
        public static string Seconds(float seconds) => $"{seconds:0.##} s";
    }
}
