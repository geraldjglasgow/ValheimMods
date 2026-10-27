using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a ship built from a blueprint would be, worked out at the blueprint table before it is built, with the code a
    /// launched ship runs: its blueprint in its own frame (<see cref="KwShipData.Rebased"/>), its hull measured
    /// (<see cref="KwBpHull"/>) and cut into slices (<see cref="KwHullSlices"/>), its waterline (<see cref="KwShipTuning.Draught"/>),
    /// its weight the water those slices displace there (<see cref="KwShipWeight"/>, as the launched ship and the dock
    /// waterline weigh it), its speed, sail push,
    /// steering and health (<see cref="KwShipStats"/>) and its cost from the hammer (<see cref="KwBpCost"/>). How fast it
    /// sails and how tightly it turns come from the launched ship's own step (<see cref="KwBpHandling"/>); how stable it is
    /// from the slices and the centre of weight the float sets (<see cref="KwBpStability"/>). Pure arithmetic on the saved
    /// parts: nothing is spawned, and every peer would get the same numbers.
    /// </summary>
    public sealed class KwBpFigures
    {
        public int Pieces { get; private set; }
        public bool Hull { get; private set; }
        public float Length { get; private set; }
        public float Beam { get; private set; }
        public float Depth { get; private set; }

        /// <summary>The design waterline's height over the keel line.</summary>
        public float Waterline { get; private set; }

        /// <summary>How deep the ship goes under the water: from the waterline down to its keel's foot (the keel line without a keel).</summary>
        public float Draught { get; private set; }

        /// <summary>The waterline to the lowest sheer in the middle half of the ship.</summary>
        public float Freeboard { get; private set; }

        /// <summary>Kilograms: the water the hull displaces to its waterline.</summary>
        public float Weight { get; private set; }

        public KwBpStability Stability { get; private set; } = KwBpStability.None;
        public KwShipStats Stats { get; private set; } = null!;
        public KwBpHandling Handling { get; private set; } = KwBpHandling.None;
        public List<(string Item, int Amount)> Cost { get; private set; } = new List<(string Item, int Amount)>();

        /// <summary>The figures of a ship built from this blueprint, on a ship prefab that turns and drags as this one.</summary>
        public static KwBpFigures Of(KwBlueprint blueprint, KwBpShipRef ship)
        {
            KwBpHull measured = KwBpHull.Measure(blueprint);
            KwBlueprint own = KwShipData.Rebased(blueprint, measured.Origin);
            KwBpHull hull = KwBpHull.Measure(own);
            float waterline = KwShipTuning.Draught(hull);
            List<KwHullSlice> slices = KwHullSlices.Of(own, hull, waterline);
            KwBpFigures figures = new KwBpFigures
            {
                Pieces = blueprint.Pieces.Count, Hull = KwBpHull.Parts(own).Count > 0, Length = hull.Length, Beam = hull.Beam,
                Depth = hull.Depth, Waterline = waterline, Cost = KwBpCost.Of(blueprint),
            };
            KwShipWeight weight = KwShipWeight.Of(slices, waterline);
            figures.Weight = weight.Mass;
            figures.Draught = waterline - Mathf.Min(0f, KeelFoot(own));
            figures.Freeboard = SheerOver(slices, hull, waterline);
            figures.Stats = KwShipStats.Of(own, hull, KwShipWeight.Sized(slices, hull));
            figures.Stability = KwBpStability.Of(slices, hull, waterline, weight);
            figures.Handling = KwBpHandling.Of(figures.Stats, hull, ship);
            return figures;
        }

        // The lowest keel's foot under the keel line (its piece's place less its depth); 0 with no keel.
        private static float KeelFoot(KwBlueprint own)
        {
            float foot = 0f;
            foreach (KwBpPiece piece in own.Pieces)
            {
                if (KwShipParts.Keel(piece, out _, out KwKeelBody body))
                    foot = Mathf.Min(foot, piece.Position.y - body.Depth);
            }
            return foot;
        }

        // The lowest top of a section in the middle half of the ship, over the waterline; the whole ship's when no section
        // stands in the middle half.
        private static float SheerOver(List<KwHullSlice> slices, KwBpHull hull, float waterline)
        {
            float middle = hull.Origin.z, reach = hull.Length / 4f, low = float.MaxValue, any = float.MaxValue;
            foreach (KwHullSlice s in slices)
            {
                float top = float.MinValue;
                foreach (Vector2 p in s.Shape)
                    top = Mathf.Max(top, p.y);
                any = Mathf.Min(any, top);
                if (Mathf.Abs(s.Z - middle) <= reach)
                    low = Mathf.Min(low, top);
            }
            float sheer = low < float.MaxValue ? low : any < float.MaxValue ? any : hull.Depth;
            return sheer - waterline;
        }
    }
}
