using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a ship's cargo does (<see cref="KwShipCargo"/>), worked out from its hull: the most it can float down to
    /// (<see cref="Most"/>: the water at most 5 cm under its lowest deck, or at 85% of its depth with no deck, and never
    /// more than half a metre over its design waterline, where the hull's water mask ends), the drag a load adds
    /// (<see cref="Drag"/>), and the carts standing on its deck (<see cref="Carts"/>). Weights are the game's item weights,
    /// taken as kilograms.
    /// </summary>
    public static class KwShipLoad
    {
        private const float Density = 1000f, UnderDeck = 0.05f, Sheer = 0.85f, MaskReach = 0.5f;

        /// <summary>How much the drag along the ship grows per ship's weight of load, and at most (times the drag unladen).</summary>
        public const float DragPerWeight = 1f, MostDrag = 4f;

        /// <summary>The highest the water may stand on the hull with a load (ship frame, up from the keel line).</summary>
        public static float Line(KwBpHull hull, float waterline)
        {
            float deck = hull.DeckLow < 1e5f ? hull.DeckLow - UnderDeck : Sheer * hull.Depth;
            return Mathf.Max(waterline, Mathf.Min(deck, waterline + MaskReach));
        }

        /// <summary>The most load the hull floats down to <see cref="Line"/>: the water it pushes aside there less its own weight.</summary>
        public static float Most(List<KwHullSlice> slices, KwBpHull hull, float waterline, float unladen)
        {
            float line = Line(hull, waterline), volume = 0f;
            foreach (KwHullSlice s in slices)
                volume += KwSectionMath.Wet(s.Shape, 0f, 1f, line, out _) * s.Length;
            return Mathf.Max(0f, Density * volume - unladen);
        }

        /// <summary>The drag along the ship with a load, times its drag unladen: one more per ship's weight, at most four times.</summary>
        public static float Drag(float load, float unladen) => Mathf.Min(1f + DragPerWeight * Mathf.Max(0f, load) / Mathf.Max(unladen, 1f), MostDrag);

        /// <summary>The carts standing on the deck: each its own mass and its load, as the cart weighs itself.</summary>
        public static float Carts(Transform ship, KwBpHull hull)
        {
            float sum = 0f;
            foreach (Vagon cart in Vagon.m_instances)
            {
                if (cart == null || cart.m_container == null || !Aboard(ship.InverseTransformPoint(cart.transform.position), hull))
                    continue;
                Inventory? load = cart.m_container.GetInventory();
                sum += cart.m_baseMass + (load != null ? load.GetTotalWeight() * cart.m_itemWeightMassFactor : 0f);
            }
            return sum;
        }

        /// <summary>Whether a point in the ship's frame is over its deck: within its beam and length, from the keel line to a little over the sheer.</summary>
        public static bool Aboard(Vector3 at, KwBpHull hull) =>
            Mathf.Abs(at.x) <= hull.Beam / 2f && Mathf.Abs(at.z) <= hull.Length / 2f && at.y > -0.5f && at.y < hull.Depth + 2f;
    }
}
