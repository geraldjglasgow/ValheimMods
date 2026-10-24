using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// Swim's page in the info pane: what the Swimming perks (section 39) add at the player's level, each always on
    /// (<see cref="QuickDry"/>, <see cref="LongBreath"/>, <see cref="Salvage"/>, <see cref="Diving"/> with its
    /// <see cref="Breath"/>), and the <see cref="WaterBuilder"/> milestone, with the numbers they use. Written on the
    /// local player's client as the page opens; nothing while the server has Swimming off.
    /// </summary>
    public static class SwimmingPage
    {
        public static void Write(SkillPage page)
        {
            if (!GameSkillSettings.Swimming.Value)
                return;
            float factor = page.Factor;
            page.Perk(Lang.Get("gs_perk_quickdry"), 0f, QuickDryTip(factor));
            page.Perk(Lang.Get("gs_perk_longbreath"), 0f, LongBreathTip(factor));
            page.Perk(Lang.Get("gs_perk_salvage"), 0f, SalvageTip(page.Player, factor));
            page.Perk(Lang.Get("gs_perk_diving"), 0f, Lang.Format("gs_swim_diving_tip", SkillPage.Number(Breath.Seconds(factor)),
                SkillPage.Number(Breath.Seconds(0f)), SkillPage.Number(Breath.Seconds(1f))));
            page.Perk(Lang.Get("gs_perk_waterbuilder"), WaterBuilder.Level, Lang.Get("gs_swim_waterbuilder_tip"));
        }

        private static string QuickDryTip(float factor)
        {
            string sooner = SkillPage.Percent(1f - QuickDry.Kept(factor));
            float wet = WetTime();
            if (wet <= 0f)
                return Lang.Format("gs_swim_quickdry_tip", sooner, SkillPage.Percent(1f - QuickDry.Kept(1f)));
            return Lang.Format("gs_swim_quickdry_tip_times", sooner, SkillPage.Duration(wet * QuickDry.Kept(factor)),
                SkillPage.Duration(wet), SkillPage.Duration(wet * QuickDry.Kept(1f)));
        }

        private static string LongBreathTip(float factor)
        {
            float hurt = Breath.DrownShare * LongBreath.Kept(factor);
            return Lang.Format("gs_swim_longbreath_tip", SkillPage.Number(LongBreath.Delay(factor)), SkillPage.Percent(1f - LongBreath.Kept(factor)),
                SkillPage.Percent(hurt), SkillPage.Percent(Breath.DrownShare), SkillPage.Number(LongBreath.Delay(1f)), SkillPage.Percent(1f - LongBreath.Kept(1f)));
        }

        private static string SalvageTip(Player player, float factor)
        {
            float range = player.m_autoPickupRange;
            return Lang.Format("gs_swim_salvage_tip", SkillPage.Number(range * Salvage.Reach(factor)), SkillPage.Number(range),
                SkillPage.Number(range * Salvage.Reach(1f)));
        }

        /// <summary>The game's Wet time in seconds, or 0 when it cannot be read.</summary>
        private static float WetTime()
        {
            StatusEffect wet = ObjectDB.instance != null ? ObjectDB.instance.GetStatusEffect(SEMan.s_statusEffectWet) : null;
            return wet != null ? wet.m_ttl : 0f;
        }
    }
}
