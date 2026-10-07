namespace GrindstoneSkills
{
    /// <summary>
    /// What Farming keeps on a growing plant's ZDO: the planter (ID and Farming level, written by the planting client,
    /// which owns the new plant), whether a compost bin fertilized it and the day it was last tended (both written by
    /// the plant's owner). A plant without a planter was not planted by a player with Farming: wild saplings,
    /// Woodcutting's replanted saplings, anything planted before. Every reader may run on any client.
    /// </summary>
    public static class PlantKeys
    {
        private static readonly int PlanterHash = Keys.FarmPlanter.GetStableHashCode();
        private static readonly int LevelHash = Keys.FarmLevel.GetStableHashCode();
        private static readonly int FedHash = Keys.FarmFed.GetStableHashCode();
        private static readonly int TendedHash = Keys.FarmTended.GetStableHashCode();

        /// <summary>The plant's ZDO, or null while it has none (a placement ghost) or is gone.</summary>
        public static ZDO Of(Plant plant)
        {
            ZNetView nview = plant != null ? plant.m_nview : null;
            return nview != null && nview.IsValid() ? nview.GetZDO() : null;
        }

        public static bool IsPlanted(ZDO zdo) => zdo != null && zdo.GetLong(PlanterHash) != 0L;

        public static long Planter(ZDO zdo) => zdo?.GetLong(PlanterHash) ?? 0L;

        public static float Level(ZDO zdo) => zdo?.GetFloat(LevelHash) ?? 0f;

        public static bool Fed(ZDO zdo) => zdo != null && zdo.GetBool(FedHash);

        /// <summary>The in-game day the plant was last tended; -1 when never.</summary>
        public static int TendedDay(ZDO zdo) => zdo?.GetInt(TendedHash, -1) ?? -1;

        public static void WritePlanter(ZDO zdo, long playerId, float level)
        {
            zdo.Set(PlanterHash, playerId);
            zdo.Set(LevelHash, level);
        }

        public static void SetFed(ZDO zdo) => zdo.Set(FedHash, true);

        public static void SetTended(ZDO zdo, int day) => zdo.Set(TendedHash, day);
    }
}
