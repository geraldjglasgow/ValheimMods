using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the sea will stand on a ship once it is launched, in the dock's frame: a straight line along the ship, its
    /// height at a place along the dock (<see cref="At"/>). Level across the ship (a hull is the same on both sides), and
    /// sloping along it when the ship will sit down by the bow or the stern.
    /// </summary>
    public readonly struct KwWaterline
    {
        public KwWaterline(float height, float slope, float middle)
        {
            Height = height;
            Slope = slope;
            Middle = middle;
        }

        /// <summary>The water's height (dock frame) at the ship's middle, halfway between its ends.</summary>
        public float Height { get; }

        /// <summary>How much higher the water stands on the hull per metre towards the dock's +z end.</summary>
        public float Slope { get; }

        /// <summary>Where the ship's middle is along the dock (z).</summary>
        public float Middle { get; }

        /// <summary>The water's height (dock frame) at a place along the dock.</summary>
        public float At(float z) => Height + Slope * (z - Middle);
    }

    /// <summary>
    /// The waterline of a ship on the dock, worked out as its launched ship floats (<see cref="KwShipHydro"/>): the
    /// hull measured (<see cref="KwBpHull"/>) and moved into the ship's own frame as launching does
    /// (<see cref="KwShipData.Rebased"/>), its design waterline (<see cref="KwShipTuning.Draught"/>), its slices
    /// (<see cref="KwHullSlices"/>), its weight and the weight's centre as the launched ship's body has them
    /// (<see cref="KwShipWeight"/>, the one reckoning for both). Then the line where that weight floats at rest: as deep
    /// as the water it pushes aside weighs as much as the ship, and tilted along the ship until the water's lift stands
    /// straight under the weight. With the weight over the middle of the water displaced to the design waterline, that is
    /// the design waterline itself, level (heavier than its hull at least, 100 kg, it sits deeper). Pure arithmetic, no
    /// Unity objects.
    /// </summary>
    public static class KwWaterlineLevel
    {
        // The steepest tilt looked for along the ship (a slope of the water on the hull), and the halvings that find it.
        private const float MostSlope = 0.35f;
        private const int Halvings = 16;

        /// <summary>
        /// The waterline of the ship on a dock, its pieces placed in the dock's frame as they stand there: worked out in the
        /// frame its saved blueprint will have, turned round when its stern is forward as saving turns it
        /// (<see cref="KwBpBow.Fix"/>, on its pieces in order along the dock as <see cref="KwBlueprint.Capture"/> has them),
        /// so the slices are the launched ship's own; and given back in the dock's frame.
        /// </summary>
        public static KwWaterline OnDock(KwBlueprint dock)
        {
            KwBlueprint saved = new KwBlueprint(dock.Name);
            saved.Pieces.AddRange(dock.Pieces);
            saved.Pieces.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));
            if (saved.Pieces.Count == 0)
                return Of(saved);
            KwBpPiece first = saved.Pieces[0];
            KwBpBow.Fix(saved);
            // Turned round, every piece is a new one, placed about the middle (twice it: its two ends' sum).
            if (saved.Pieces.Contains(first))
                return Of(saved);
            return Back(Of(saved), saved.Pieces[0].Position.z + saved.Pieces[saved.Pieces.Count - 1].Position.z);
        }

        /// <summary>A waterline worked out on the blueprint turned round about its middle (twice it given), in the dock's frame.</summary>
        public static KwWaterline Back(KwWaterline turned, float twice) => new KwWaterline(turned.Height, -turned.Slope, twice - turned.Middle);

        /// <summary>The waterline of a blueprint as it will be launched (its pieces in the frame it was saved in).</summary>
        public static KwWaterline Of(KwBlueprint saved)
        {
            KwBpHull measured = KwBpHull.Measure(saved);
            KwBlueprint own = KwShipData.Rebased(saved, measured.Origin);
            KwBpHull hull = KwBpHull.Measure(own);
            float draught = KwShipTuning.Draught(hull);
            List<KwHullSlice> slices = KwHullSlices.Of(own, hull, draught);
            KwShipWeight weight = KwShipWeight.Of(slices, draught);
            float volume = weight.Mass / KwShipWeight.Density;
            float slope = Trim(slices, volume, weight.Centre);
            return new KwWaterline(measured.Keel + Level(slices, volume, slope), slope, measured.Origin.z);
        }

        /// <summary>The water the slices push aside under the line y = height + slope·z (ship frame), in m³.</summary>
        public static float Volume(List<KwHullSlice> slices, float height, float slope)
        {
            float volume = 0f;
            foreach (KwHullSlice s in slices)
                volume += KwSectionMath.Wet(s.Shape, 0f, 1f, height + slope * s.Z, out _) * s.Length;
            return volume;
        }

        /// <summary>The line's height at the ship's middle where, at this slope, the hull pushes aside this much water.</summary>
        public static float Level(List<KwHullSlice> slices, float volume, float slope)
        {
            (float low, float high) = Reach(slices, slope);
            for (int i = 0; i < Halvings; i++)
            {
                float mid = (low + high) / 2f;
                if (Volume(slices, mid, slope) < volume)
                    low = mid;
                else
                    high = mid;
            }
            return (low + high) / 2f;
        }

        // From under the lowest point of the hull to over its highest, at this slope.
        private static (float Low, float High) Reach(List<KwHullSlice> slices, float slope)
        {
            float low = float.MaxValue, high = float.MinValue;
            foreach (KwHullSlice s in slices)
            {
                foreach (Vector2 p in s.Shape)
                    (low, high) = (Mathf.Min(low, p.y - slope * s.Z), Mathf.Max(high, p.y - slope * s.Z));
            }
            return low <= high ? (low - 0.1f, high + 0.1f) : (0f, 1f);
        }

        /// <summary>The slope at which the water's lift stands straight under the weight's centre (ship frame: its z and y used).</summary>
        public static float Trim(List<KwHullSlice> slices, float volume, Vector3 weight)
        {
            float low = -MostSlope, high = MostSlope;
            if (Lean(slices, volume, weight, low) >= 0f)
                return low;
            if (Lean(slices, volume, weight, high) <= 0f)
                return high;
            for (int i = 0; i < Halvings; i++)
            {
                float mid = (low + high) / 2f;
                if (Lean(slices, volume, weight, mid) < 0f)
                    low = mid;
                else
                    high = mid;
            }
            return (low + high) / 2f;
        }

        // How far the water's lift stands along the ship from the weight's centre (along the water, so 0 at rest): more
        // when the water is higher towards +z, which moves the lift that way.
        private static float Lean(List<KwHullSlice> slices, float volume, Vector3 weight, float slope)
        {
            float height = Level(slices, volume, slope), wet = 0f, z = 0f, y = 0f;
            foreach (KwHullSlice s in slices)
            {
                float area = KwSectionMath.Wet(s.Shape, 0f, 1f, height + slope * s.Z, out Vector2 at) * s.Length;
                (wet, z, y) = (wet + area, z + area * s.Z, y + area * at.y);
            }
            return wet <= 1e-6f ? 0f : z / wet - weight.z + slope * (y / wet - weight.y);
        }
    }
}
