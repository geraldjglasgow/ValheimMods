using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// The lines the game's weapon skills share on their pages, from the game's own formulas (Weapon Mastery adds to
    /// these, on its own page writers, and leaves the formulas as the game has them): the share of a weapon's damage a hit rolls (Skills.GetRandomSkillRange: 25–55%
    /// at level 0 up to 85–100% at 100) and the cut in what an attack costs (Attack.GetAttackStamina, GetAttackEitr and
    /// GetAttackHealth, ItemData.GetDrawStaminaDrain: 33% at level 100, linear from 0).
    /// </summary>
    internal static class WeaponLines
    {
        /// <summary>The cut in an attack's stamina, eitr and health cost, and in a bow's draw drain, at level 100.</summary>
        public const float CostCut = 0.33f;

        /// <summary>A melee skill's page: what it is (the key of its sentence), the damage a hit rolls and the attack stamina.</summary>
        public static void Melee(SkillPage page, string aboutKey)
        {
            page.About = Lang.Get(aboutKey);
            Damage(page, "gs_page_weapon_damage_line");
            Cost(page, "gs_page_weapon_stamina", "gs_page_weapon_stamina_tip");
        }

        /// <summary>
        /// "Damage 47–77% of the weapon's": the range a hit rolls in at this level, the range at 100 in the tip.
        /// <paramref name="lineKey"/> is the line (the word as $1, the range as $2); <paramref name="tipKey"/>, when given,
        /// a tip that adds to the shared one (that one as $1).
        /// </summary>
        public static void Damage(SkillPage page, string lineKey, string tipKey = null)
        {
            Roll(page, out float low, out float high);
            string term = Lang.Get("gs_page_weapon_damage");
            string tip = Lang.Get("gs_page_weapon_damage_tip");
            page.Line(Lang.Format(lineKey, term, SkillPage.Range(low, high)), term, tipKey == null ? tip : Lang.Format(tipKey, tip));
        }

        /// <summary>
        /// "Attack stamina -12.2%": a cost the skill cuts by up to <see cref="CostCut"/>, the thing itself explained on hover.
        /// <paramref name="termKey"/> names the cost, <paramref name="moreKey"/> is the sentence after the cut at 100 in the tip.
        /// </summary>
        public static void Cost(SkillPage page, string termKey, string moreKey)
        {
            string term = Lang.Get(termKey);
            page.Line(Lang.Format("gs_page_weapon_cost_line", term, SkillPage.Percent(CostCut * page.Factor)), term,
                Lang.Format("gs_page_weapon_cost_tip", SkillPage.Percent(CostCut), Lang.Get(moreKey)));
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
        public static string Seconds(float seconds) => Lang.Format("gs_page_weapon_seconds", seconds.ToString("0.##"));
    }
}
