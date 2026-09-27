using EarthWright.Brush;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// Keeps the cost line ("cost" in <see cref="HudText"/>) and the preview's cost warning ("cost" in
    /// <see cref="PreviewStatus"/>) current while a terrain entry is in use, four times a second; clears both when the
    /// tool is put away. The warning is reported even when the player hid the cost line, and "Free build" is shown
    /// while it is on either way.
    /// </summary>
    internal static class CostDisplay
    {
        private const string Key = "cost";
        private const int Order = 400;
        private const float Interval = 0.25f;

        private static float next;
        private static bool shown;

        public static void Tick()
        {
            if (!BrushState.Active || BrushState.Action == null || !LocalTool.InTerrainTool)
            {
                Clear();
                return;
            }
            if (Time.unscaledTime < next)
                return;
            next = Time.unscaledTime + Interval;
            CostContext ctx = CostContext.ForSelected(Player.m_localPlayer);
            if (ctx == null)
            {
                Clear();
                return;
            }
            Show(CostLine.For(ctx));
        }

        private static void Show(CostLine line)
        {
            bool visible = CostSettings.ShowCosts.Value || FreeBuild.On;
            HudText.Set(Key, visible ? line.Text : null, Order);
            PreviewStatus.Report(Key, line.ShortReason);
            shown = true;
        }

        private static void Clear()
        {
            if (!shown)
                return;
            shown = false;
            next = 0f;
            HudText.Clear(Key);
            PreviewStatus.Report(Key, null);
        }
    }
}
