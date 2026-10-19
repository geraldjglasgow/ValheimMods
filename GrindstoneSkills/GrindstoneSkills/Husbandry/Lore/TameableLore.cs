using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// The lore lines of a tameable creature: taming time left while it is being tamed, fed time left, contentment, and
    /// the breeding lines of <see cref="BreedingLore"/>. The Taming Levels hint ("Needs a Husbandry 40 keeper to tame")
    /// shows to everyone, lore or not, since it explains why taming does not move. Read on the looking client from
    /// replicated ZDO values.
    /// </summary>
    public static class TameableLore
    {
        public static string Lines(Tameable tameable, bool lore)
        {
            string lines = GateLine(tameable);
            if (!lore)
                return lines;
            lines = LoreText.Add(lines, TamingLine(tameable));
            lines = LoreText.Add(lines, FedLine(tameable));
            lines = LoreText.Add(lines, ContentLine(tameable));
            return LoreText.Add(lines, BreedingLore.Lines(tameable));
        }

        private static string GateLine(Tameable tameable)
        {
            if (tameable.IsTamed())
                return "";
            float required = TamingLevels.Required(Herd.PrefabName(tameable));
            return required > 0f && TamingSpeed.Factor(tameable) <= 0f
                ? Lang.Format("gs_herd_needs_keeper", required.ToString("0"), tameable.m_tamingSpeedMultiplierRange.ToString("0")) : "";
        }

        private static string TamingLine(Tameable tameable)
        {
            if (!Herd.IsBeingTamed(tameable))
                return "";
            float factor = TamingSpeed.Factor(tameable);
            return factor > 0f ? Lang.Format("gs_herd_tamed_in", LoreText.Duration(Herd.TamingLeft(tameable) / factor)) : "";
        }

        private static string FedLine(Tameable tameable)
        {
            if (tameable.m_character == null || !(tameable.IsTamed() || Herd.IsBeingTamed(tameable)) || tameable.IsHungry())
                return "";
            double left = tameable.m_fedDuration - Herd.SecondsSince(tameable.m_nview.GetZDO().GetLong(ZDOVars.s_tameLastFeeding));
            return left > 0.0 ? Lang.Format("gs_herd_fed_for", LoreText.Duration(left)) : "";
        }

        private static string ContentLine(Tameable tameable)
        {
            if (!tameable.IsTamed() || !BreedingPace.IsContent(tameable.m_nview))
                return "";
            double left = -Herd.SecondsSince(tameable.m_nview.GetZDO().GetLong(Keys.ContentUntil));
            return Lang.Format("gs_herd_content_for", LoreText.Duration(left));
        }
    }
}
