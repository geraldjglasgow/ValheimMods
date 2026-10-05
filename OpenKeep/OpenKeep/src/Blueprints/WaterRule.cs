using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Water in Valheim is one sea level for the whole world: ground dug below it holds water, anywhere. A blueprint
    /// with water (a canal, a moat, a pool) therefore sets its levelled ground from the sea, not from the spot: high
    /// enough to stay dry (<see cref="BlueprintRules.DryMargin"/> above the sea) and low enough that its dug floor lies
    /// the blueprint's water depth below the sea. Placed on a hill, the hill is cut down to that height.
    /// </summary>
    public static class WaterRule
    {
        public static float Sea => ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;

        /// <summary>The levelled ground for a wanted height: unchanged without water, else within the water's band.</summary>
        public static float Fit(Blueprint bp, float wanted)
        {
            if (!bp.HasWater)
                return wanted;
            float high = Sea - bp.WaterFloor - bp.WaterDepth;
            float low = Mathf.Min(Sea + BlueprintRules.DryMargin, high);
            return Mathf.Clamp(wanted, low, high);
        }
    }
}
