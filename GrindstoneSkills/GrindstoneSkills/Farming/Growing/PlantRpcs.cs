using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Farming's RPCs on a plant, to its owner, registered on a plant's ZNetView beside the game's when the first of them
    /// arrives for it (<see cref="LazyRpcs"/>), so the thousands of plants nobody tends or fertilizes carry none:
    /// <list type="bullet">
    /// <item><see cref="Keys.RpcTend"/> (int day): tended on that in-game day. Once per day the plant gains Tending Bonus
    /// percent of its grow time (<see cref="PlantClock"/>).</item>
    /// <item><see cref="Keys.RpcFertilize"/> (): a compost bin fertilized it (<see cref="CompostFeed"/>).</item>
    /// </list>
    /// </summary>
    public static class PlantRpcs
    {
        /// <summary>Registers both RPCs on a plant's view, when the first of them arrives for it (<see cref="LazyRpcs"/>).</summary>
        public static void RegisterOn(ZNetView nview)
        {
            Plant plant = nview.GetComponent<Plant>();
            if (plant == null || LazyRpcs.Has(nview, Keys.RpcTend) || LazyRpcs.Has(nview, Keys.RpcFertilize))
                return;
            nview.Register<int>(Keys.RpcTend, (sender, day) => Guard.Run(Keys.RpcTend, () => Tend(plant, day)));
            nview.Register(Keys.RpcFertilize, sender => Guard.Run(Keys.RpcFertilize, () => Fertilize(plant)));
        }

        /// <summary>On the owner: records the day and adds the growth, unless the plant was already tended that day.</summary>
        public static void Tend(Plant plant, int day)
        {
            ZDO zdo = PlantKeys.Of(plant);
            if (zdo == null || !plant.m_nview.IsOwner() || PlantKeys.TendedDay(zdo) >= day)
                return;
            PlantKeys.SetTended(zdo, day);
            float bonus = FarmingPerkSettings.TendingBonus.Value / 100f;
            if (FarmSkill.Active && bonus > 0f)
                PlantClock.Advance(plant, plant.GetGrowTime() * bonus);
        }

        /// <summary>On the owner: marks the plant fertilized.</summary>
        public static void Fertilize(Plant plant)
        {
            ZDO zdo = PlantKeys.Of(plant);
            if (zdo != null && plant.m_nview.IsOwner())
                PlantKeys.SetFed(zdo);
        }
    }
}
