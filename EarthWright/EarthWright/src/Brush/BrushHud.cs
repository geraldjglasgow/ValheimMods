using System.Text;
using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Hands the brush's lines to the HUD block ("brush" and "target" in <see cref="HudText"/>) and its key hint to
    /// <see cref="HintText"/> ("brush"); the Preview module draws both. The hint only changes with the entry or the
    /// key settings, so it is rebuilt twice a second at most. Local display only.
    /// </summary>
    public static class BrushHud
    {
        public const int BrushOrder = 10;
        public const int TargetOrder = 20;

        private static float hintBuiltAt = -10f;
        private static string hintFor;

        public static void Show(ToolAction action, BrushValues values)
        {
            HudText.Set("brush", BrushHudText.BrushLine(action, values), BrushOrder);
            HudText.Set("target", BrushHudText.TargetLine(action, values), TargetOrder);
            if (hintFor == action.Id && Time.unscaledTime - hintBuiltAt < 0.5f)
                return;
            hintFor = action.Id;
            hintBuiltAt = Time.unscaledTime;
            HintText.Set("brush", Hint(action));
        }

        public static void Hide()
        {
            HudText.Clear("brush");
            HudText.Clear("target");
            HintText.Set("brush", null);
            hintFor = null;
        }

        private static string Hint(ToolAction action)
        {
            StringBuilder hint = new StringBuilder();
            Key(hint, KeyNames.Of(ControlSettings.NextValueKey), BrushWords.HintValue);
            Key(hint, KeyNames.Wheel() + " " + KeyNames.Of(ControlSettings.DecreaseKey) + KeyNames.Of(ControlSettings.IncreaseKey), BrushWords.HintChange);
            if (!EntryKinds.IsPath(action))
                Key(hint, KeyNames.Of(ControlSettings.ShapeKey), BrushWords.HintShape);
            if (StyleCycle.Applies(action))
                Key(hint, KeyNames.Of(ControlSettings.StyleKey), BrushWords.HintStyle);
            Key(hint, KeyNames.Of(ControlSettings.PaintKey), BrushWords.HintPaint);
            if (TargetHeight.Used(action))
            {
                Key(hint, KeyNames.Of(TargetKeys.Lock), BrushWords.HintLock);
                Key(hint, KeyNames.Of(TargetKeys.Cycle), BrushWords.HintTarget);
            }
            if (!EntryKinds.IsPath(action))
            {
                Key(hint, KeyNames.Of(ControlSettings.GridKey), BrushWords.HintGrid);
                Key(hint, KeyNames.Of(ControlSettings.EdgeKey), BrushWords.HintEdge);
            }
            if (HardLevel.Applies(action))
                Key(hint, KeyNames.Of(ControlSettings.HardLevelKey), BrushWords.HintHard);
            return hint.ToString();
        }

        private static void Key(StringBuilder hint, string key, string word)
        {
            if (hint.Length > 0)
                hint.Append("   ");
            hint.Append('[').Append(key).Append("] ").Append(word);
        }
    }
}
