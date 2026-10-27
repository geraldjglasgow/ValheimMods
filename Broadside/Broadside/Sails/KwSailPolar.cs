using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The kinds of sail by how they take the wind: square sails on yards across the ship, and the fore-and-aft sails along
    /// it, a mast's gaff or triangle sail and a bowsprit's jibs.
    /// </summary>
    public enum KwSailKind
    {
        Square,
        Gaff,
        Triangle,
        Jib,
    }

    /// <summary>
    /// How hard each kind of sail drives its ship ahead, and pushes it sideways to leeward, by the apparent wind's angle off
    /// the bow (0 right ahead, 180 dead astern; <see cref="KwApparentWind"/>), per unit of the ship's sail force: 0.70 ahead
    /// with the wind dead astern is the game's own sail's push there, which a square sail keeps, so no ship runs before the
    /// wind faster than before. A square sail draws most with the wind astern, less on a broad reach, little on the beam
    /// and nothing closer than 70° to the wind. A fore-and-aft sail draws on every point outside its no-go zone, best on a
    /// close to beam reach and still some on a run: a gaff sail from 30°, a triangle sail a little closer, a jib closest
    /// of all but blanketed on a run. Close-hauled it pushes the ship sideways hardest, which the keel mostly takes
    /// (<see cref="KwShipDrive"/>). Straight lines between the table's angles. Pure arithmetic.
    /// </summary>
    public static class KwSailPolar
    {
        private static readonly float[] Angles = { 0f, 25f, 30f, 35f, 40f, 45f, 60f, 70f, 80f, 90f, 110f, 135f, 160f, 180f };

        // Ahead, by kind (square, gaff, triangle, jib), at each of the angles.
        private static readonly float[][] Ahead =
        {
            new[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.06f, 0.18f, 0.40f, 0.58f, 0.68f, 0.70f },
            new[] { 0f, 0f, 0f, 0.10f, 0.30f, 0.45f, 0.62f, 0.67f, 0.70f, 0.72f, 0.70f, 0.60f, 0.48f, 0.42f },
            new[] { 0f, 0f, 0.04f, 0.18f, 0.36f, 0.48f, 0.63f, 0.67f, 0.69f, 0.70f, 0.66f, 0.55f, 0.42f, 0.35f },
            new[] { 0f, 0f, 0.12f, 0.30f, 0.42f, 0.50f, 0.62f, 0.64f, 0.66f, 0.66f, 0.60f, 0.48f, 0.36f, 0.30f },
        };

        // To leeward, by kind, at each of the angles.
        private static readonly float[][] Aside =
        {
            new[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.10f, 0.30f, 0.45f, 0.45f, 0.35f, 0.15f, 0f },
            new[] { 0f, 0f, 0f, 0.30f, 0.75f, 0.95f, 0.85f, 0.70f, 0.55f, 0.45f, 0.30f, 0.20f, 0.08f, 0f },
            new[] { 0f, 0f, 0.10f, 0.45f, 0.80f, 0.95f, 0.85f, 0.70f, 0.55f, 0.45f, 0.30f, 0.20f, 0.08f, 0f },
            new[] { 0f, 0f, 0.30f, 0.65f, 0.85f, 0.95f, 0.85f, 0.70f, 0.55f, 0.45f, 0.30f, 0.20f, 0.08f, 0f },
        };

        /// <summary>The kinds in order (<see cref="KwSailKind"/>).</summary>
        public const int Kinds = 4;

        /// <summary>A mast's sail's kind by its shape: square, triangle or gaff.</summary>
        public static KwSailKind Of(KwSailSpec sail) => sail.Shape == 0 ? KwSailKind.Square : sail.Shape == 1 ? KwSailKind.Triangle : KwSailKind.Gaff;

        /// <summary>The push ahead of a sail of that kind with the apparent wind that far off the bow (degrees).</summary>
        public static float Drive(KwSailKind kind, float angle) => At(Ahead[(int)kind], angle);

        /// <summary>The push to leeward of a sail of that kind with the apparent wind that far off the bow (degrees).</summary>
        public static float Side(KwSailKind kind, float angle) => At(Aside[(int)kind], angle);

        /// <summary>The first of the table's angles off the apparent wind the kind draws at (degrees).</summary>
        public static float Closest(KwSailKind kind)
        {
            float[] row = Ahead[(int)kind];
            for (int i = 0; i < Angles.Length; i++)
            {
                if (row[i] > 0f)
                    return Angles[i];
            }
            return 180f;
        }

        // The row's value at the angle, along the straight line between the two angles round it.
        private static float At(float[] row, float angle)
        {
            float a = Mathf.Clamp(Mathf.Abs(angle), 0f, 180f);
            for (int i = 1; i < Angles.Length; i++)
            {
                if (a <= Angles[i])
                    return Mathf.Lerp(row[i - 1], row[i], (a - Angles[i - 1]) / (Angles[i] - Angles[i - 1]));
            }
            return row[row.Length - 1];
        }
    }
}
