using System;
using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The target height part of the panel, for entries that level: the height as a typed field (typing fixes it: the
    /// mode becomes Locked, or stays Exact), a lock toggle, and the target mode. Everything goes through the Brush
    /// module's <see cref="TargetState"/>, which the brush reads every frame, exactly as the lock and mode keys do.
    /// "Floor" is not offered here: it takes the height of the floor piece aimed at (middle mouse).
    /// </summary>
    internal static class PanelTarget
    {
        private static readonly TargetSource[] Modes =
            { TargetSource.Feet, TargetSource.Aimed, TargetSource.Continued, TargetSource.Locked, TargetSource.Exact };

        private static string[] captions;

        public static void Draw(ToolAction action)
        {
            if (!TargetHeight.Used(action))
                return;
            if (PanelFields.Float("target", "$ew_preview_target_height", BrushState.TargetHeight, out float typed, "0.00"))
                TargetState.SetFixed(TargetState.Mode == TargetSource.Exact ? TargetSource.Exact : TargetSource.Locked, typed);
            bool locked = TargetState.IsFixed;
            if (GUILayout.Toggle(locked, Language.Localize("$ew_preview_lock")) != locked)
                ToggleLock(locked);
            int current = Array.IndexOf(Modes, TargetState.Mode);
            int chosen = PanelFields.Choice("$ew_preview_source", current, Captions(), 3);
            if (chosen != current && chosen >= 0)
                SetMode(Modes[chosen]);
        }

        private static void ToggleLock(bool locked)
        {
            if (locked)
                TargetState.Release();
            else
                TargetState.SetFixed(TargetSource.Locked, BrushState.TargetHeight);
        }

        private static void SetMode(TargetSource mode)
        {
            if (TargetState.IsFixedMode(mode))
                TargetState.SetFixed(mode, BrushState.TargetHeight);
            else
                TargetState.SetLive(mode);
        }

        private static string[] Captions()
        {
            if (captions == null)
                captions = Array.ConvertAll(Modes, BrushWords.SourceName);
            return captions;
        }
    }
}
