using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a hammock hangs between its two ends (the user, 2026-11-03: "the only way that there should be a free standing
    /// piece is if you are trying to put one end or both ends of the hammock on the ground. If you try putting it on the
    /// ceiling or a different freestanding piece, then it should actually just be a hook."), plain maths in the world with
    /// an up of its own (the world's, a ship's aboard). An end aimed at a face looking up (<see cref="Walkable"/>, the ground
    /// always) stands on a post; any other takes an iron hook. The points clicked are <see cref="MinApart"/> to
    /// <see cref="MaxApart"/> apart, level. A hook hangs its ropes from where it is; a post's peg is set to match the other end:
    /// two posts level at <see cref="Hook"/> over the higher post's floor, a post with a hook at the hook's height, always
    /// <see cref="MinPeg"/> to <see cref="MaxPeg"/> over its own floor. The spreader bars hang <see cref="Standard"/> over the
    /// floor under the middle when the ropes allow: at least <see cref="MinDrop"/> under the lower rope point (more on a long
    /// span, the ropes no flatter than <see cref="RopeSlope"/>), at most <see cref="MaxDrop"/> under the higher. The cloth's
    /// lowest point then <see cref="Clear"/> to <see cref="MostOver"/> over the floor (<see cref="KwHammockChecks"/>).
    /// </summary>
    public static class KwHammockRules
    {
        public const float MinApart = 2.2f, MaxApart = 4f;

        /// <summary>A face at least this much up is a floor, where a post stands (about 45 degrees, as a player walks).</summary>
        public const float Walkable = 0.7f;

        /// <summary>Two posts' pegs over the higher one's floor; the standard hammock's posts apart.</summary>
        public const float Hook = 1.25f, StandardApart = 2.6f;

        /// <summary>A post's peg over its own floor.</summary>
        public const float MinPeg = 0.9f, MaxPeg = 2.2f;

        /// <summary>The bars under the rope points (least, the least rise over the ropes' run, most) and over the floor.</summary>
        public const float MinDrop = 0.46f, RopeSlope = 0.6f, MaxDrop = 3.2f, Standard = Hook - MinDrop;

        /// <summary>The cloth's lowest point over the floor under it.</summary>
        public const float Clear = 0.2f, MostOver = 1.3f;

        /// <summary>A vector's level part (square to up).</summary>
        public static Vector3 Flat(Vector3 v, Vector3 up) => v - up * Vector3.Dot(v, up);

        /// <summary>A point's height along up.</summary>
        public static float Height(Vector3 p, Vector3 up) => Vector3.Dot(p, up);

        /// <summary>
        /// The two ends' rope points (where each one's ropes hang from): a hook's eye; a post's peg tip, out of the post
        /// toward the other end at the height <see cref="Pegs"/> gives.
        /// </summary>
        public static (Vector3 A, Vector3 B) Ropes((Vector3 At, Vector3 Face, bool Post) a, (Vector3 At, Vector3 Face, bool Post) b, Vector3 up)
        {
            Vector3 way = Flat(b.At - a.At, up).normalized;
            Vector3 hookA = KwHammockParts.HookRope(a.At, a.Face, up), hookB = KwHammockParts.HookRope(b.At, b.Face, up);
            (float pegA, float pegB) = Pegs(a, b, hookA, hookB, up);
            Vector3 ropeA = a.Post ? a.At + way * KwHammockParts.PegIn + up * pegA : hookA;
            Vector3 ropeB = b.Post ? b.At - way * KwHammockParts.PegIn + up * pegB : hookB;
            return (ropeA, ropeB);
        }

        // Each post's peg over its own floor: two posts level at the standard height over the higher floor, a post with a
        // hook at the hook's height; within the post's reach either way.
        private static (float A, float B) Pegs((Vector3 At, Vector3 Face, bool Post) a, (Vector3 At, Vector3 Face, bool Post) b, Vector3 hookA, Vector3 hookB, Vector3 up)
        {
            float floorA = Height(a.At, up), floorB = Height(b.At, up);
            float line = a.Post && b.Post ? Mathf.Max(floorA, floorB) + Hook : Height(a.Post ? hookB : hookA, up);
            return (Mathf.Clamp(line - floorA, MinPeg, MaxPeg), Mathf.Clamp(line - floorB, MinPeg, MaxPeg));
        }

        /// <summary>
        /// The bars' lowest and highest height (along up) between two rope points: within the longest drop of the higher,
        /// the least drop under the lower; the lowest over the highest when the ends are too uneven.
        /// </summary>
        public static (float Low, float High) Bars(Vector3 ropeA, Vector3 ropeB, Vector3 up)
        {
            float span = Flat(ropeB - ropeA, up).magnitude;
            float a = Height(ropeA, up), b = Height(ropeB, up);
            return (Mathf.Max(a, b) - MaxDrop, Mathf.Min(a, b) - Drop(span));
        }

        /// <summary>The least drop from a rope point to its bar for a span between the rope points.</summary>
        public static float Drop(float span) => Mathf.Max(MinDrop, RopeSlope * (span - KwHammockShape.ClothFor(span)) * 0.5f);
    }
}
