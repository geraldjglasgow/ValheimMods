using System.Collections.Generic;
using System.Linq;

namespace Broadside
{
    /// <summary>
    /// The "Material" choice in a hull tool's window (planking, deck, keel, rib, railing, cabin) and in each of their edit
    /// windows: the tiers offered for new work, by name (<see cref="KwBiomeTiers.Offered"/>: Meadows and Black Forest), opening on the
    /// tier last picked for that kind of part. An edit opens it on the part's own (<see cref="KwEdit"/>); a part built or saved in
    /// another tier has that tier after the offered ones, picked, so an edit keeps it unless the player picks one of
    /// those. Read back by the option's name, whichever options the window has. Local to this player.
    /// </summary>
    public static class KwBiomeField
    {
        public const string Key = "tier";
        private static readonly Dictionary<KwPart, KwBiomeTier> last = new Dictionary<KwPart, KwBiomeTier>();

        public static KwField Field(KwPart part) => Field(part, Last(part));

        /// <summary>The choice opened on a tier given: the offered tiers, and that tier after them when it is not one.</summary>
        public static KwField Field(KwPart part, KwBiomeTier tier)
        {
            List<KwBiomeTier> tiers = new List<KwBiomeTier>(KwBiomeTiers.Offered);
            if (!tiers.Contains(tier))
                tiers.Add(tier);
            return KwField.Choice(Key, "Material", tiers.Select(t => t.Name).ToArray(), tiers.IndexOf(tier));
        }

        /// <summary>The tier the window's choice holds, kept as the one last picked for that kind of part.</summary>
        public static KwBiomeTier Picked(KwFormValues values, KwPart part)
        {
            KwBiomeTier tier = Shown(values);
            last[part] = tier;
            return tier;
        }

        /// <summary>The tier the window's choice holds, by its name, not kept (an edit's); Meadows for a name no tier has.</summary>
        public static KwBiomeTier Shown(KwFormValues values)
        {
            string name = values.Choice(Key);
            foreach (KwBiomeTier tier in KwBiomeTiers.All)
            {
                if (tier.Name == name)
                    return tier;
            }
            return KwBiomeTiers.Meadows;
        }

        /// <summary>The tier last picked for a kind of part, when new work may still pick it; Meadows before any.</summary>
        public static KwBiomeTier Last(KwPart part) =>
            last.TryGetValue(part, out KwBiomeTier tier) && KwBiomeTiers.Offers(tier) ? tier : KwBiomeTiers.Meadows;
    }
}
