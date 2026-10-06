using System.Collections.Generic;
using EarthWright.Brush;
using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// The requirement verdicts the build bar shows. <c>Hud.UpdatePieceBuildStatus</c> asks <c>HaveRequirements</c> for one
    /// piece every frame in build mode; working each out builds a cost context and a bill and counts the inventory and
    /// the stations in range. Here a verdict is kept per piece for <see cref="MaxAge"/> seconds, keyed also on the tool
    /// in hand and the brush radius (the bill grows with it). The click itself never reads this: inside the game's
    /// placement the verdict is worked out fresh.
    /// </summary>
    internal static class BuildBarVerdicts
    {
        private const float MaxAge = 0.25f;

        private sealed class Verdict
        {
            public bool? Value;
            public float Until;
            public float Radius;
            public ItemDrop.ItemData Tool;
        }

        private static readonly Dictionary<Piece, Verdict> verdicts = new Dictionary<Piece, Verdict>();

        public static bool? For(Player player, Piece piece)
        {
            float now = Time.time;
            ItemDrop.ItemData tool = player.GetRightItem();
            float radius = BrushState.Radius;
            if (verdicts.TryGetValue(piece, out Verdict kept) && now < kept.Until && kept.Radius == radius && kept.Tool == tool)
                return kept.Value;
            if (kept == null)
                verdicts[piece] = kept = new Verdict();
            kept.Value = PlacementCharges.Work(player, piece);
            kept.Until = now + MaxAge;
            kept.Radius = radius;
            kept.Tool = tool;
            return kept.Value;
        }

        /// <summary>Forgets every verdict (a click was charged: the inventory changed).</summary>
        public static void Clear() => verdicts.Clear();
    }
}
