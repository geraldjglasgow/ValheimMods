using System.Collections.Generic;
using System.Globalization;
using EarthWright.Actions;
using EarthWright.Brush;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The preset buttons of the panel, from the "Raise Presets" setting ("amount x radius" pairs such as
    /// "1x2, 5x2, 5x3, 8x3"): a click sets the raise or lower amount and the brush radius of the selected entry in one
    /// go (into its remembered values, clamped by the Brush module's limits). Pairs that do not parse are skipped; the
    /// list is parsed again only when the setting changes.
    /// </summary>
    internal static class PanelPresets
    {
        private static readonly List<Vector2> presets = new List<Vector2>();
        private static readonly List<string> labels = new List<string>();
        private static string parsedFrom;

        /// <summary>Drawn while an entry that raises or lowers by an amount is selected.</summary>
        public static void Draw()
        {
            ToolAction action = BrushState.Active ? BrushState.Action : null;
            if (action == null || action.IsSpecial)
                return;
            BrushValues values = BrushMemory.For(action);
            Parse(HudSettings.RaisePresets.Value);
            if (presets.Count == 0 || ValueSelector.AmountOf(action, values) != AmountKind.Amount)
                return;
            PanelFields.Heading("$ew_preview_presets");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < presets.Count; i++)
            {
                if (GUILayout.Button(labels[i]))
                    Apply(action, values, presets[i]);
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>Writes the preset into the entry's remembered values, clamped as the brush clamps them.</summary>
        private static void Apply(ToolAction action, BrushValues values, Vector2 preset)
        {
            values.Amount = BrushLimits.Amount(preset.x);
            if (values.Defaults.Resizable)
                values.Radius = BrushLimits.Radius(values, preset.y);
            BrushPublisher.Publish(action, values);
        }

        private static void Parse(string text)
        {
            if (text == parsedFrom)
                return;
            parsedFrom = text;
            presets.Clear();
            labels.Clear();
            foreach (string pair in (text ?? "").Split(','))
            {
                if (!TryPair(pair, out Vector2 preset))
                    continue;
                presets.Add(preset);
                labels.Add(preset.x.ToString("0.##", CultureInfo.InvariantCulture) + " × " + preset.y.ToString("0.##", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>"5x3" (also "5×3" or "5*3") as amount 5 m, radius 3 m.</summary>
        private static bool TryPair(string pair, out Vector2 preset)
        {
            preset = default;
            string[] parts = pair.Trim().ToLowerInvariant().Split('x', '×', '*');
            if (parts.Length != 2)
                return false;
            bool amount = float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out preset.x);
            bool radius = float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out preset.y);
            return amount && radius && preset.x > 0f && preset.y > 0f;
        }
    }
}
