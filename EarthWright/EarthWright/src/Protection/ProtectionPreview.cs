using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// Tells the player before the click (the player's machine only): four times a second, while the brush is active
    /// (<see cref="BrushState.Active"/>), the protection checks run on the edit the click would make and the first refusal goes to
    /// <see cref="PreviewStatus"/> (the preview turns red and shows it). A special entry without a brush stroke of its
    /// own (ramp, road, clearing, uproot) is judged by the rules that do not depend on the ground plus the object rules
    /// at the aimed point (<see cref="ObjectRules"/>: admin zones there). The HUD also shows a line while an admin holds
    /// the limit override key.
    /// </summary>
    public static class ProtectionPreview
    {
        /// <summary>Reasons show in key order; "access" sorts before "cost" and "paths", so a ward or lock shows before a price.</summary>
        private const string StatusKey = "access";
        private const string HudKey = "protection";
        private const int HudOrder = 950;
        private const float Interval = 0.25f;

        private static float nextCheck;
        private static bool overrideShown;

        public static void Tick()
        {
            bool inTool = LocalTool.InTerrainTool;
            bool showOverride = inTool && AdminRouting.OverrideActive;
            if (showOverride != overrideShown)
            {
                HudText.Set(HudKey, showOverride ? ProtectionWords.OverrideHud : null, HudOrder);
                overrideShown = showOverride;
            }
            float now = Time.unscaledTime;
            if (now < nextCheck && nextCheck - now <= Interval)
                return;
            nextCheck = now + Interval;
            PreviewStatus.Report(StatusKey, inTool ? CurrentReason() : null);
        }

        private static string CurrentReason()
        {
            if (!BrushState.Active)
                return null;
            ToolAction action = BrushState.Action ?? ActionCatalog.Current;
            if (action == null)
                return null;
            // Without aim the brush centre is the last aimed point, not where the next click would go.
            if (!action.IsSpecial && !BrushState.HasAim)
                return null;
            if (HasBrushFootprint(action))
                return ProtectionGuards.SenderReason(EditFactory.Build(action));
            string reason = ProtectionGuards.SenderReason(GroundlessEdit(action));
            return reason ?? ObjectRules.Refusal(Player.m_localPlayer, BrushState.Center, LocalTool.RightItemName);
        }

        /// <summary>A brush entry, or a special one that shapes the brush area itself (Groundbreaker levels and paves it).</summary>
        private static bool HasBrushFootprint(ToolAction action)
        {
            return !action.IsSpecial || action.Height != HeightOp.None || action.Paint != PaintOp.None;
        }

        /// <summary>An edit that names the entry but covers no ground, for the checks that do not look at the ground.</summary>
        private static TerrainEdit GroundlessEdit(ToolAction action)
        {
            return TerrainEdit.ForVertices(new VertexSet { Mode = VertexMode.Targets }, action.Id);
        }
    }
}
