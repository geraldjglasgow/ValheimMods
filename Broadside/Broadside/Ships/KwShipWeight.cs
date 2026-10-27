using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a launched ship weighs and where its weight's centre lies, in the ship's frame, from its slices
    /// (<see cref="KwHullSlices"/>) and its design waterline (<see cref="KwShipTuning.Draught"/>): as heavy as the water
    /// the slices displace to that waterline (at least <see cref="Least"/>), its centre straight over the middle of that
    /// water (the centre of buoyancy, along the ship and across), so the ship floats level at its waterline whatever the
    /// shape of its ends, and <see cref="Ballast"/> of the way up from the keel line to the waterline. The one reckoning
    /// for both the launched ship's body (<see cref="KwShipHydro"/>) and the waterline drawn on the dock
    /// (<see cref="KwWaterlineLevel"/>). Pure arithmetic, no Unity objects.
    /// </summary>
    public readonly struct KwShipWeight
    {
        public const float Density = 1000f, Least = 100f;

        // The centre of weight a share of the way up from the keel to the waterline: a ship's weight lies low (its keel,
        // ballast, stores), so it rights itself; at the waterline a tall ship heeled far over under sail.
        public const float Ballast = 0.35f;

        private KwShipWeight(float mass, Vector3 centre, float waterplane)
        {
            Mass = mass;
            Centre = centre;
            Waterplane = waterplane;
        }

        /// <summary>Kilograms.</summary>
        public float Mass { get; }

        /// <summary>The weight's centre in the ship's frame.</summary>
        public Vector3 Centre { get; }

        /// <summary>The hull's area at the waterline, square metres (every slice's breadth there times its length).</summary>
        public float Waterplane { get; }

        /// <summary>
        /// The weight a ship's figures are reckoned by (<see cref="KwShipStats"/>: its health, its speed for its size): what
        /// its hull displaces to <see cref="KwShipTuning.Sizing"/>, as its waterline stood when they were tuned (the game's
        /// longship's 1000 health at 104 t). Its waterline now goes by its depth amidships, which floats a ship with tall
        /// ends higher and lighter; its figures do not change with it.
        /// </summary>
        public static float Sized(List<KwHullSlice> slices, KwBpHull hull) => Of(slices, KwShipTuning.Sizing(hull)).Mass;

        public static KwShipWeight Of(List<KwHullSlice> slices, float waterline)
        {
            float volume = 0f, x = 0f, z = 0f, plane = 0f;
            foreach (KwHullSlice s in slices)
            {
                float wet = KwSectionMath.Wet(s.Shape, 0f, 1f, waterline, out Vector2 at) * s.Length;
                (volume, x, z) = (volume + wet, x + wet * at.x, z + wet * s.Z);
                plane += s.Breadth * s.Length;
            }
            Vector3 centre = new Vector3(0f, waterline * Ballast, 0f);
            if (volume > 1e-6f)
                (centre.x, centre.z) = (x / volume, z / volume);
            return new KwShipWeight(Mathf.Max(Least, Density * volume), centre, plane);
        }
    }
}
