using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The brush part of the panel: typed fields for the values that apply to the selected entry and shape (the same
    /// ones the value selector offers). The Brush module publishes <see cref="BrushState"/> from the entry's remembered
    /// <see cref="BrushValues"/> every frame, so a typed value is written into those values (clamped with the Brush
    /// module's own limits) and the rotation into <see cref="BrushPublisher.Rotation"/>, only when the player changes a
    /// field; the fields show the published values. Only the grass density lives in the brush state itself.
    /// </summary>
    internal static class PanelBrush
    {
        public static void Draw()
        {
            PanelFields.Heading("$ew_preview_panel_brush");
            ToolAction action = BrushState.Active ? BrushState.Action : null;
            if (action == null)
            {
                GUILayout.Label(Language.Localize("$ew_preview_panel_noentry"));
                return;
            }
            BrushValues values = BrushMemory.For(action);
            bool changed = false;
            if (values.Defaults.Resizable && PanelFields.Float("radius", "$ew_preview_radius", BrushState.Radius, out float radius))
                changed |= Set(ref values.Radius, BrushLimits.Radius(values, radius));
            if (!action.IsPathTool)
            {
                changed |= Shape(action, values) | Amount(action, values);
                PanelTarget.Draw(action);
                changed |= PanelChoices.Draw(action, values);
            }
            if (changed)
                BrushPublisher.Publish(action, values);
        }

        private static bool Shape(ToolAction action, BrushValues values)
        {
            bool changed = false;
            BrushShape shape = ShapeCycle.Effective(values);
            if (ValueSelector.HasDepth(shape) && PanelFields.Float("radius2", Label(BrushWords.DepthName(shape)), BrushState.Radius2, out float depth))
            {
                BrushLimits.RadiusRange(values.Defaults, out _, out float max);
                changed |= Set(ref values.Radius2, BrushLimits.Radius2(shape, BrushState.Radius, depth, max));
            }
            bool turns = ValueSelector.Applies(BrushValue.Rotation, action, values);
            if (turns && PanelFields.Float("rotation", "$ew_preview_rotation", BrushState.Rotation, out float rotation, "0.#"))
                changed |= Set(ref BrushPublisher.Rotation, Mathf.Repeat(rotation, 360f));
            bool edge = ValueSelector.Applies(BrushValue.Hardness, action, values);
            if (edge && PanelFields.Percent("hardness", "$ew_preview_hardness", BrushState.Hardness, out float hardness))
                changed |= Set(ref values.Hardness, Mathf.Clamp01(hardness));
            return changed;
        }

        /// <summary>The one "amount" value the entry uses: raise/lower metres, level max step, smooth strength or grass density.</summary>
        private static bool Amount(ToolAction action, BrushValues values)
        {
            switch (ValueSelector.AmountOf(action, values))
            {
                case AmountKind.Amount:
                    return PanelFields.Float("amount", "$ew_preview_amount", BrushState.Amount, out float amount) && Set(ref values.Amount, BrushLimits.Amount(amount));
                case AmountKind.MaxStep:
                    return PanelFields.Float("maxstep", "$ew_preview_maxstep", BrushState.MaxStep, out float step) && Set(ref values.MaxStep, BrushLimits.MaxStep(step));
                case AmountKind.Strength:
                    return PanelFields.Percent("strength", "$ew_preview_strength", BrushState.Strength, out float strength) && Set(ref values.Strength, Mathf.Clamp01(strength));
                case AmountKind.Density:
                    return PanelFields.Percent("density", "$ew_preview_density", BrushState.Density, out float density) && Set(ref BrushState.Density, Mathf.Clamp01(density));
                default:
                    return false;
            }
        }

        private static bool Set(ref float field, float value)
        {
            field = value;
            return true;
        }

        /// <summary>A Brush word as a field label with its unit.</summary>
        private static string Label(string token) => Language.Localize(token) + " (m)";
    }
}
