using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// Volume stone is owed in fractions (0.25 per cubic metre raised, some per square metre paved); whole stones are
    /// charged and the rest is carried to the next use, so many small swings cost the same as one big one. Lowering
    /// is free. The carry is this player's own, kept in memory for the session.
    /// </summary>
    internal static class VolumeCharge
    {
        private static float carried;

        /// <summary>Stone owed for this raised volume (m³) and newly paved area (m²).</summary>
        public static float Cost(float raised, float paved)
        {
            return Mathf.Max(0f, raised) * MaterialSettings.StonePerCubicMetre.Value
                + Mathf.Max(0f, paved) * MaterialSettings.StonePerSquareMetre.Value;
        }

        /// <summary>True when either volume cost is set, so estimates are worth making.</summary>
        public static bool Enabled => MaterialSettings.StonePerCubicMetre.Value > 0f || MaterialSettings.StonePerSquareMetre.Value > 0f;

        /// <summary>Whole stones charged now for an amount owed, the carried fraction included.</summary>
        public static int Due(float owed) => owed <= 0f ? 0 : Mathf.FloorToInt(owed + carried + 0.0001f);

        /// <summary>Books a payment: what was owed but not charged carries over (below one stone).</summary>
        public static void Paid(float owed, int charged)
        {
            if (owed > 0f)
                carried = Mathf.Clamp(owed + carried - charged, 0f, 0.9999f);
        }

        /// <summary>
        /// Work that owes no volume stone: a reset stroke (the Reset entry returns the ground to the world's own, as the
        /// free reset keys and command do) and admin work (privileged edits: the Terraform entry, the limit override),
        /// which would otherwise need thousands of stones for one swing.
        /// </summary>
        public static bool Exempt(TerrainEdit edit)
        {
            if (edit == null)
                return false;
            if (edit.Has(EditFlags.Privileged))
                return true;
            return edit.Kind == EditKind.Stroke && edit.Stroke != null && edit.Stroke.Height == HeightOp.Reset;
        }
    }

    /// <summary>The ground area an edit newly paves, in square metres (one paint cell per square metre).</summary>
    internal static class PavedArea
    {
        /// <summary>The cells the work newly paves, from the engine's estimate (0 without one).</summary>
        public static float Of(EditEstimate estimate) => estimate != null ? estimate.PavedCells : 0f;
    }
}
