using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A fish's size improves its fillets. Cleaning a fish is a prep table recipe (Recipe_Fish1, one fish of any kind), so
    /// the raw fish it gives are kitchen items that roll Cooking stars (<see cref="CraftStars"/>); the game already gives
    /// more of them for a bigger fish (1 plus 3 per level above 1). The fish used up is recorded with its level
    /// (<see cref="CraftRecord"/>), and each level above 1 adds Fillet Levels Per Fish Level to the cook's effective level
    /// for the roll, like starred ingredients do: a fisher's level 5 catch gives the cook better odds, whoever cleans it.
    /// </summary>
    public static class Fillets
    {
        /// <summary>The effective Cooking levels the fish among <paramref name="lots"/> add; 0 without a fish.</summary>
        public static float Levels(IEnumerable<CraftRecord.Lot> lots)
        {
            float perLevel = Mathf.Max(0f, FishingCatchSettings.FilletLevelsPerFishLevel.Value);
            if (!FishSkill.Active || perLevel <= 0f || lots == null)
                return 0f;
            int biggest = 1;
            foreach (CraftRecord.Lot lot in lots)
            {
                if (lot?.Item != null && FishInfo.IsFishItem(lot.Item))
                    biggest = Mathf.Max(biggest, lot.Item.m_quality);
            }
            return (biggest - 1) * perLevel;
        }
    }
}
