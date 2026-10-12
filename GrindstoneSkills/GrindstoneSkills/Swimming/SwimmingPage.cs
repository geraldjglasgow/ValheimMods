namespace GrindstoneSkills
{
    /// <summary>
    /// Swim's page in the info pane: what the Swimming perks (section 39) add at the player's level, each always on
    /// (<see cref="QuickDry"/>, <see cref="LongBreath"/>, <see cref="Salvage"/>), with the numbers they use. Written on the
    /// local player's client as the page opens; nothing while the server has Swimming off.
    /// </summary>
    public static class SwimmingPage
    {
        /// <summary>The game's drowning hit, as a share of max health, once a second.</summary>
        private const float DrownShare = 0.05f;

        public static void Write(SkillPage page)
        {
            if (!GameSkillSettings.Swimming.Value)
                return;
            float factor = page.Factor;
            page.Perk("Quick Dry", 0f, QuickDryTip(factor));
            page.Perk("Long Breath", 0f, LongBreathTip(factor));
            page.Perk("Salvage", 0f, SalvageTip(page.Player, factor));
        }

        private static string QuickDryTip(float factor)
        {
            string sooner = $"Wet ends {SkillPage.Percent(1f - QuickDry.Kept(factor))} sooner after you leave the water or the rain";
            float wet = WetTime();
            if (wet <= 0f)
                return $"{sooner}. {SkillPage.Percent(1f - QuickDry.Kept(1f))} at level 100.";
            return $"{sooner}: {SkillPage.Duration(wet * QuickDry.Kept(factor))} at your level, {SkillPage.Duration(wet)} untrained, {SkillPage.Duration(wet * QuickDry.Kept(1f))} at level 100.";
        }

        private static string LongBreathTip(float factor)
        {
            float hurt = DrownShare * LongBreath.Kept(factor);
            return $"Out of stamina in water, drowning starts {SkillPage.Number(LongBreath.Delay(factor))} s later and hurts {SkillPage.Percent(1f - LongBreath.Kept(factor))} less: {SkillPage.Percent(hurt)} of max health a second instead of {SkillPage.Percent(DrownShare)}. {SkillPage.Number(LongBreath.Delay(1f))} s later and {SkillPage.Percent(1f - LongBreath.Kept(1f))} less at level 100.";
        }

        private static string SalvageTip(Player player, float factor)
        {
            float range = player.m_autoPickupRange;
            return $"While you swim, loose items are picked up from {SkillPage.Number(range * Salvage.Reach(factor))} m instead of {SkillPage.Number(range)} m. {SkillPage.Number(range * Salvage.Reach(1f))} m at level 100.";
        }

        /// <summary>The game's Wet time in seconds, or 0 when it cannot be read.</summary>
        private static float WetTime()
        {
            StatusEffect wet = ObjectDB.instance != null ? ObjectDB.instance.GetStatusEffect(SEMan.s_statusEffectWet) : null;
            return wet != null ? wet.m_ttl : 0f;
        }
    }
}
