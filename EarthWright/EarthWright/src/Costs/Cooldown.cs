using System.Linq;
using EarthWright.Actions;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// The wait between two terrain uses of this player ("Cooldown"). A use starts it: a brush click sent through the
    /// game's placement, or a special entry's work paid through <see cref="CostApi.TryCharge"/>. The sender guard holds
    /// back brush clicks and special entries' edits inside the wait; undo and redo (restores) are never held back,
    /// and neither are the edits a special entry sends for work it has just paid for, or in the same frame as the
    /// use (a ramp or road may send several).
    /// Console commands and keys that are not terrain entries (reset, admin commands) are left alone.
    /// </summary>
    internal static class Cooldown
    {
        private static float lastUse = -1000f;
        private static int lastUseFrame = -1;
        private static bool lastUseWasPaidWork;

        /// <summary>Seconds left to wait; 0 when a use may start.</summary>
        public static float Remaining => Mathf.Max(0f, CostSettings.Cooldown.Value - (Time.time - lastUse));

        /// <summary>The wait message while the cooldown runs, else null.</summary>
        public static string Waiting()
        {
            float remaining = Remaining;
            return remaining > 0f ? CostWords.Format(CostWords.Wait, remaining.ToString("0.0")) : null;
        }

        /// <summary>Starts the wait. <paramref name="paidWork"/>: a special entry's work paid through CostApi.</summary>
        public static void MarkUse(bool paidWork)
        {
            lastUse = Time.time;
            lastUseFrame = Time.frameCount;
            lastUseWasPaidWork = paidWork;
        }

        /// <summary>Sender guard part: the wait message when this edit comes too early, else null.</summary>
        public static string Check(TerrainEdit edit)
        {
            if (edit.Has(EditFlags.IsRestore))
                return null;
            if (edit.Has(EditFlags.FromPlacement))
                return Waiting();
            if (!IsSpecialSource(edit.Source) || lastUseWasPaidWork || Time.frameCount == lastUseFrame)
                return null;
            return Waiting();
        }

        /// <summary>After an edit left: a brush click, or special work nobody paid through CostApi, starts the wait.</summary>
        public static void OnSent(TerrainEdit edit)
        {
            if (edit.Has(EditFlags.IsRestore))
                return;
            if (edit.Has(EditFlags.FromPlacement))
                MarkUse(false);
            else if (IsSpecialSource(edit.Source) && !lastUseWasPaidWork)
                MarkUse(false);
        }

        /// <summary>The edit was made by a special entry (its piece name or its special key).</summary>
        private static bool IsSpecialSource(string source)
        {
            if (string.IsNullOrEmpty(source))
                return false;
            return ActionCatalog.All.Any(a => a.IsSpecial && (a.Id == source || a.Special == source));
        }
    }
}
