namespace GrindstoneSkills
{
    /// <summary>
    /// The pages of the game's melee skills in the info pane: the damage a hit rolls and the attack stamina at the
    /// player's level (<see cref="WeaponLines"/>), plus what one skill does differently: axes on trees, bare fists.
    /// </summary>
    public static class MeleePages
    {
        public static void Swords(SkillPage page) =>
            WeaponLines.Melee(page, "Swords hit harder and cost less stamina. Trained by hitting creatures with a sword.");

        public static void Knives(SkillPage page) =>
            WeaponLines.Melee(page, "Knives hit harder and cost less stamina. Trained by hitting creatures with a knife.");

        public static void Clubs(SkillPage page) =>
            WeaponLines.Melee(page, "Clubs, maces and sledges hit harder and cost less stamina. Trained by hitting creatures with one.");

        public static void Polearms(SkillPage page) =>
            WeaponLines.Melee(page, "Atgeirs hit harder and cost less stamina. Trained by hitting creatures with one.");

        public static void Spears(SkillPage page) =>
            WeaponLines.Melee(page, "Spears hit harder, thrown or held, and cost less stamina. Trained by hitting creatures with a spear.");

        public static void Axes(SkillPage page)
        {
            WeaponLines.Melee(page, "Axes and battleaxes hit harder and cost less stamina. Trained by hitting creatures with an axe.");
            page.Line("Trees use Woodcutting instead", "Woodcutting",
                "Most axes roll their damage on trees, logs and stumps from Woodcutting and train it there, not Axes. Axes still cuts the swing's stamina.");
        }

        public static void Unarmed(SkillPage page)
        {
            WeaponLines.Melee(page, "Fists hit harder and cost less stamina. Trained by hitting creatures unarmed.");
            Fists(page);
        }

        /// <summary>"Bare fists hit for 2–4": the player's fists' damage times the range a hit rolls in.</summary>
        private static void Fists(SkillPage page)
        {
            ItemDrop fists = page.Player.m_unarmedWeapon;
            if (fists == null)
                return;
            float damage = fists.m_itemData.GetDamage().GetTotalDamage();
            if (damage <= 0f)
                return;
            WeaponLines.Roll(page, out float low, out float high);
            page.Line($"Bare fists hit for {damage * low:0}–{damage * high:0}", "Bare fists",
                "Damage of a punch before the target's armour and resistances.");
        }
    }
}
