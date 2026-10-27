using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The parts that link a rudder to its helm (<see cref="KwRudderLay"/>), in a tier's own wood and fittings
    /// (<see cref="KwRudderTier.Wood"/>, <see cref="KwRudderTier.Fitting"/>). On the rudder's head (the blade's frame): a
    /// yoke across it, an eye at each end for a line or a rod, or a peg hanging from a tiller's end for the hand. On the
    /// deck (the piece's frame): the deck tiller's pivot post, standing on the deck with a foot plate, a band and a pin.
    /// The deck tiller itself (its own frame, the pivot at its bar's height): a boss round the pin, a yoke as long as the
    /// head's, a handle forward to the helmsman and a peg hanging from its end to his idle hand. With the steering gear
    /// (<see cref="KwRudderGear"/>) the head carries the gear's quadrant in place of a tiller or yoke
    /// (<see cref="KwGearShapes.Quadrant"/>), and the post the line shaft's bearings (<see cref="KwGearFittings.PostBearings"/>).
    /// The links (the piece's frame): a rod with an eye at each end. Kit axes: x forward, y left, z up, metres; the deck's
    /// parts are the size of a man's hands, not of the post.
    /// </summary>
    public static class KwRudderLinkShapes
    {
        public const float EyeRadius = 0.024f, Pin = 0.016f, Boss = 0.05f, Bar = 0.03f, PostHalf = 0.055f;
        private const float Eye = 0.007f, PegRadius = 0.02f, RodRadius = 0.012f;

        /// <summary>
        /// A yoke across the rudder's head at its yoke height: a bar of radius r (round with that many sides, or squared
        /// with four), banded near each end, an eye or a pin at each end.
        /// </summary>
        public static void Yoke(MeshBuilder wood, MeshBuilder fitting, KwRudderKit k, float r, int sides)
        {
            float z = k.YokeZ, half = k.Lay.Half;
            YokeBar(wood, new Vector3(0f, -(half - 0.015f), z), new Vector3(0f, half - 0.015f, z), r, sides);
            foreach (float side in new[] { 1f, -1f })
            {
                float at = side * (half - 0.07f);
                YokeBar(fitting, new Vector3(0f, at, z), new Vector3(0f, at + side * 0.035f, z), r + 0.006f, sides);
                End(fitting, new Vector3(0f, side * half, z), k.Tier.Rod != null);
            }
        }

        // A bar across the ship from a to c: a squared timber for four sides, else round.
        private static void YokeBar(MeshBuilder b, Vector3 a, Vector3 c, float r, int sides)
        {
            if (sides == 4)
                Solid.Box(b, Vector3.Min(a, c) - new Vector3(r, 0f, r), Vector3.Max(a, c) + new Vector3(r, 0f, r));
            else
                Solid.Log(b, a, c, r, sides);
        }

        /// <summary>
        /// A tier's yoke when its rudder is yoked, or the steering gear's quadrant when the gear steers it (true); else
        /// nothing (false), and the tier draws its tiller.
        /// </summary>
        public static bool Yoked(MeshBuilder wood, MeshBuilder fitting, KwRudderKit k, float r, int sides)
        {
            if (k.Lay.Geared)
                KwGearShapes.Quadrant(wood, fitting, k);
            else if (k.Lay.Yoked)
                Yoke(wood, fitting, k, r, sides);
            return k.Lay.Geared || k.Lay.Yoked;
        }

        /// <summary>The peg at a tier's tiller's end when a helmsman holds the tiller.</summary>
        public static void Grip(MeshBuilder wood, MeshBuilder fitting, KwRudderKit k, Vector3 end)
        {
            if (k.Lay.Kind == KwRudderHelm.Tiller)
                Peg(wood, fitting, end);
        }

        /// <summary>A peg hanging from a tiller's end (end, in the blade's frame) down to the helmsman's idle hand, ringed at its top.</summary>
        public static void Peg(MeshBuilder wood, MeshBuilder fitting, Vector3 end)
        {
            float drop = KwRudderLay.GripHeight - KwRudderLay.Hand + 0.03f;
            Solid.Log(wood, end + new Vector3(0f, 0f, 0.02f), end - new Vector3(0f, 0f, drop), PegRadius, 8);
            KwRound.Ring(fitting, end - new Vector3(0f, 0f, 0.03f), KwRudderShapes.Up, PegRadius + 0.004f, 0.006f, 10, 4);
        }

        /// <summary>
        /// The deck's parts of a rudder steered by lines: the pivot post on the post's parts, the deck tiller on its own; or
        /// for the steering gear, the line shaft's bearings on the post's faces.
        /// </summary>
        public static void Deck(KwRudderKit k)
        {
            KwRudderTier tier = k.Tier;
            if (k.Lay.Geared)
                KwGearFittings.PostBearings(k.Fixed(tier.Fitting), k);
            if (k.Lay.Kind != KwRudderHelm.Lines)
                return;
            PivotPost(k.Fixed(tier.Wood), k.Fixed(tier.Fitting), k.Lay);
            Helm(k.Helm(tier.Wood), k.Helm(tier.Fitting), k.Lay.Half, tier.Rod != null);
        }

        // Where a link meets a yoke: an eye a line is tied through, or a pin a rod's eye turns on.
        private static void End(MeshBuilder fitting, Vector3 at, bool rods)
        {
            if (rods)
                Solid.Log(fitting, at - new Vector3(0f, 0f, 0.03f), at + new Vector3(0f, 0f, 0.03f), 0.01f, 6);
            else
                KwRound.Ring(fitting, at, Vector3.right, EyeRadius, Eye, 10, 4);
        }

        // A square post from the deck to just under the deck tiller's boss, a plate at its foot, a band at its top and the
        // pin the deck tiller turns on, with a head over the boss.
        private static void PivotPost(MeshBuilder wood, MeshBuilder fitting, KwRudderLay lay)
        {
            Vector3 foot = new Vector3(lay.Reach, 0f, lay.Deck);
            float top = KwRudderLay.GripHeight - Boss - 0.01f, h = PostHalf;
            Solid.Box(wood, foot + new Vector3(-h, -h, -0.04f), foot + new Vector3(h, h, top));
            Solid.Box(fitting, foot + new Vector3(-0.09f, -0.09f, -0.005f), foot + new Vector3(0.09f, 0.09f, 0.012f));
            Solid.Box(fitting, foot + new Vector3(-h - 0.006f, -h - 0.006f, top - 0.06f), foot + new Vector3(h + 0.006f, h + 0.006f, top - 0.025f));
            Vector3 pivot = lay.Pivot;
            Solid.Log(fitting, foot + new Vector3(0f, 0f, top - 0.02f), pivot + new Vector3(0f, 0f, Boss + 0.012f), Pin, 8);
            Solid.Log(fitting, pivot + new Vector3(0f, 0f, Boss + 0.004f), pivot + new Vector3(0f, 0f, Boss + 0.02f), Pin + 0.01f, 8);
        }

        // The deck tiller round its pivot: a boss round the pin, the yoke across with an eye at each end, the handle
        // forward and the peg hanging from its end.
        private static void Helm(MeshBuilder wood, MeshBuilder fitting, float half, bool rods)
        {
            Solid.Log(fitting, new Vector3(0f, 0f, -Boss + 0.005f), new Vector3(0f, 0f, Boss), Boss, 10);
            Solid.Log(wood, new Vector3(0f, -(half - 0.015f), 0f), new Vector3(0f, half - 0.015f, 0f), Bar, 8);
            foreach (float side in new[] { 1f, -1f })
            {
                Solid.Log(fitting, new Vector3(0f, side * (half - 0.07f), 0f), new Vector3(0f, side * (half - 0.035f), 0f), Bar + 0.006f, 8);
                End(fitting, new Vector3(0f, side * half, 0f), rods);
            }
            Vector3 end = new Vector3(KwRudderLay.Handle, 0f, 0f);
            KwRound.Taper(wood, new Vector3(Boss - 0.01f, 0f, 0f), end + new Vector3(0.03f, 0f, 0f), 0.034f, 0.027f, 8);
            Peg(wood, fitting, end);
        }

        /// <summary>A rod from 0 to <paramref name="span"/> (the piece's frame), an eye round a pin at each end.</summary>
        public static void Rod(MeshBuilder rod, Vector3 span)
        {
            Vector3 along = span.normalized * 0.02f;
            Solid.Log(rod, along, span - along, RodRadius, 6);
            foreach (Vector3 end in new[] { Vector3.zero, span })
                KwRound.Ring(rod, end, KwRudderShapes.Up, EyeRadius - 0.004f, Eye, 10, 4);
        }
    }
}
