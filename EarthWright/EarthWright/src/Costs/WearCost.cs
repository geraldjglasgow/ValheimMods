using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// The tool wear of one terrain swing: the game's own durability loss, off or multiplied by the global and the
    /// entry's factor, growing with the brush radius as set; nothing during free build.
    /// </summary>
    internal static class WearCost
    {
        /// <summary>Durability points before the world's durability rate; <paramref name="vanilla"/> is the game's own wear.</summary>
        public static float For(CostContext ctx, float vanilla)
        {
            if (FreeBuild.On || !CostSettings.ToolWear.Value)
                return 0f;
            float entry = ctx.Override?.Durability ?? 1f;
            float scale = ctx.Scale(CostSettings.ToolWearRadiusExponent.Value);
            return Mathf.Max(0f, vanilla * CostSettings.ToolWearFactor.Value * entry * scale);
        }

        /// <summary>The game's own wear of a placement with this tool; 0 when the tool does not wear.</summary>
        public static float Vanilla(Player player, ItemDrop.ItemData tool)
        {
            if (player == null || tool == null || !tool.m_shared.m_useDurability)
                return 0f;
            return player.GetPlaceDurability(tool);
        }
    }
}
