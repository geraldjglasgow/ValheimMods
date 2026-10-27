using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How stiffly a ship from a blueprint stands up, as its metacentric height: the height of the centre of its
    /// underwater body over the keel line (KB, from every slice's wet area and its middle, <see cref="KwSectionMath.Wet"/>),
    /// plus how far the metacentre stands over that (BM, the waterplane's moment across the ship, every slice's breadth at
    /// the waterline cubed over twelve times its length, over the displaced volume), less the height of the centre of
    /// weight the float gives a launched ship (KG, <see cref="KwShipWeight"/>: <see cref="KwShipWeight.Ballast"/> of the
    /// way up to the waterline). The
    /// water rights a heeled ship the more, the larger it is; a hull whose metacentre falls under its centre of weight
    /// lies over. With the float's low centre of weight every ship stands up well, so the word goes by how fast the ship
    /// rolls back: its roll period from that height and the turning inertia the float gives the hull (a solid block of
    /// its beam and depth): stiff under 3 s (quick to come back, a lively roll), steady to 5.5 s, tender beyond (rolls
    /// slowly and far: a tall, narrow or very large ship), will capsize with no height, or less, left.
    /// </summary>
    public sealed class KwBpStability
    {
        private const float Stiff = 3f, Tender = 5.5f, Least = 0.01f, Gravity = 9.81f;

        public static readonly KwBpStability None = new KwBpStability();

        public float Buoyancy { get; private set; }
        public float Metacentre { get; private set; }
        public float Weight { get; private set; }

        /// <summary>The metacentric height, metres (GM = KB + BM - KG).</summary>
        public float Height => Buoyancy + Metacentre - Weight;

        /// <summary>Seconds for one roll and back; 0 when the ship would capsize.</summary>
        public float Period { get; private set; }

        public string Word => Period <= 0f ? "will capsize" : Period < Stiff ? "stiff" : Period < Tender ? "steady" : "tender";

        /// <summary>The stability at the design waterline, with the weight and its centre the launched ship gets.</summary>
        public static KwBpStability Of(List<KwHullSlice> slices, KwBpHull hull, float waterline, KwShipWeight weight)
        {
            float volume = 0f, moment = 0f, across = 0f;
            foreach (KwHullSlice s in slices)
            {
                float area = KwSectionMath.Wet(s.Shape, 0f, 1f, waterline, out Vector2 middle) * s.Length;
                (volume, moment) = (volume + area, moment + area * middle.y);
                across += s.Breadth * s.Breadth * s.Breadth / 12f * s.Length;
            }
            if (volume <= 1e-4f)
                return None;
            KwBpStability stability = new KwBpStability
            {
                Buoyancy = moment / volume,
                Metacentre = across / volume,
                Weight = weight.Centre.y,
            };
            float gyration = Mathf.Sqrt((hull.Beam * hull.Beam + hull.Depth * hull.Depth) / 12f);
            float height = stability.Height;
            stability.Period = height < Least ? 0f : 2f * Mathf.PI * gyration / Mathf.Sqrt(Gravity * height);
            return stability;
        }
    }
}
