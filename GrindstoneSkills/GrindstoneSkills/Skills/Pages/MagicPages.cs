namespace GrindstoneSkills
{
    /// <summary>
    /// The pages of the game's two magic skills in the info pane: the damage a spell rolls and the cut in its costs at the
    /// player's level (<see cref="WeaponLines"/>), then what the player's known staves summon and shield at that level
    /// (<see cref="StaffLines"/>).
    /// </summary>
    public static class MagicPages
    {
        public static void ElementalMagic(SkillPage page)
        {
            page.About = "Elemental staves hit harder and cost less eitr. Trained by hitting creatures with their spells.";
            WeaponLines.Damage(page, "the staff's", "");
            WeaponLines.Cost(page, "Eitr cost", "Also any eitr drained while holding a charge, and any stamina or health a spell costs.");
            StaffLines.Write(page);
        }

        public static void BloodMagic(SkillPage page)
        {
            page.About = "Stronger summons and shields, cheaper spells. Trained by using blood magic.";
            WeaponLines.Cost(page, "Eitr and health cost", "Also any stamina a spell costs.");
            if (!StaffLines.Write(page))
                page.Line("Summons and shields grow with level", "grow with level",
                    "Have a blood magic staff in your inventory once to see its numbers here.");
        }
    }
}
