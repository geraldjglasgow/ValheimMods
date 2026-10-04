using EliteCrafting.Epic;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// Pipeline step 2 while Epic Loot is installed: the target must be an item Epic Loot can enchant, identified, at a
    /// rarity our ladder has (Normal, Magic or Rare by default), and the link to Epic Loot's API must be working.
    /// </summary>
    internal static class EpicChecks
    {
        public static StoneResult? Check(StoneJob job)
        {
            if (!EpicApi.Ready)
            {
                return StoneResult.Refuse("epic_unavailable");
            }
            if (!EpicApi.CanBeMagic(job.Target))
            {
                return StoneResult.Refuse("epic_cannot");
            }
            if (EpicApi.IsUnidentified(job.Target))
            {
                return StoneResult.Refuse("epic_unidentified");
            }
            return job.Rarity == null ? StoneResult.Refuse("epic_rarity") : null;
        }
    }
}
