using System;

namespace GrindstoneSkills
{
    /// <summary>
    /// A growing plant's clock. The game grows a plant when the time since its s_plantTime passes its grow time
    /// (Plant.SUpdate, on the owner) and shows it half grown past half of that on every client. Moving s_plantTime back
    /// by some seconds is growth those seconds long, seen by every client through the ZDO: rain and tending do that, on
    /// the owner only. Grow times here are the ones Farming's speed applies to (<see cref="GrowTime"/>).
    /// </summary>
    public static class PlantClock
    {
        /// <summary>Seconds since the plant was planted (its ZDO's plant time).</summary>
        public static double Age(Plant plant) => plant.TimeSincePlanted();

        /// <summary>Seconds until the plant is due to grow; 0 or less when it is due.</summary>
        public static double Left(Plant plant) => plant.GetGrowTime() - plant.TimeSincePlanted();

        /// <summary>Grows the plant by <paramref name="seconds"/>, on its owner. False when this machine does not own it.</summary>
        public static bool Advance(Plant plant, double seconds)
        {
            ZNetView nview = plant != null ? plant.m_nview : null;
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || seconds <= 0.0)
                return false;
            ZDO zdo = nview.GetZDO();
            long now = ZNet.instance.GetTime().Ticks;
            long planted = zdo.GetLong(ZDOVars.s_plantTime, now);
            zdo.Set(ZDOVars.s_plantTime, planted - TimeSpan.FromSeconds(seconds).Ticks);
            return true;
        }
    }
}
