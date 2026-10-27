using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Meadows rudder, of wood and leather scraps and no metal at all: a rough six-sided pole for a stock, four planks
    /// cut square by eye with a finger's gap between them, each bound to the stock with a leather thong, held together by
    /// an upright batten on each face at two places, lashed with a thong where it crosses each plank. Two leather lashings
    /// of three turns hang it on the post, round the post and round the stock behind it; a pole tiller goes through the
    /// stock's top, lashed there, or a pole across it for a yoke (<see cref="KwRudderLinkShapes"/>), and rope lines, or the
    /// steering gear's quadrant (<see cref="KwGearShapes"/>). The
    /// plank wood is the wood floor's, the leather the leather scraps'.
    /// </summary>
    public static class KwRudderTierMeadows
    {
        private const int Planks = 4, Turns = 3;
        private const float Gap = 0.012f, Thong = 0.012f;
        private static readonly float[] Lashings = { 0.14f, 0.6f };
        private static readonly float[] Battens = { 0.3f, 0.58f };

        // How far each plank's square end falls short of or past the outline, from the bottom up.
        private static readonly float[] ByEye = { 0f, -0.05f, 0.03f, -0.02f };

        public static KwRudderTier Tier { get; } = new KwRudderTier("meadows", "Rudder, Meadows", "wood and leather", new[]
        {
            new Vector2(0.05f, 0.75f), new Vector2(0.5f, 0.8f), new Vector2(1.1f, 0.78f), new Vector2(1.7f, 0.6f), new Vector2(2.1f, 0.32f), new Vector2(2.3f, 0.12f),
        })
        {
            Made = "Rough planks lashed together with leather thongs.",
            Recipe = new[] { ("Wood", 12), ("LeatherScraps", 6) },
            Steering = 1f,
            StockRadius = 0.06f,
            BladeHalf = 0.04f,
            Wood = KwLook.Planks,
            Fitting = KwLook.Leather,
            Fixed = Lash,
            Blade = Blade,
        };

        // Two lashings of three turns, each turn round the post and round the stock behind it.
        private static void Lash(KwRudderKit k)
        {
            MeshBuilder leather = k.Fixed(KwLook.Leather);
            float turn = 0.022f * k.T, step = 0.036f * k.T;
            foreach (float share in Lashings)
            {
                for (int i = 0; i < Turns; i++)
                {
                    float z = share * k.Top + i * step;
                    KwRudderShapes.PostBand(leather, k.Fit, z, turn, Thong);
                    KwRound.Ring(leather, k.Pivot + new Vector3(0f, 0f, z + turn / 2f), KwRudderShapes.Up, k.Stock + Thong, turn / 2f, 10, 4);
                }
            }
        }

        private static void Blade(KwRudderKit k)
        {
            MeshBuilder wood = k.Blade(KwLook.Planks), leather = k.Blade(KwLook.Leather);
            KwRudderShapes.Stock(wood, k, 0.22f * k.S, 6);
            float step = (k.Tip - k.Foot) / Planks;
            for (int i = 0; i < Planks; i++)
                Plank(wood, leather, k, k.Foot + i * step + Gap / 2f, k.Foot + (i + 1) * step - Gap / 2f, ByEye[i]);
            foreach (float aft in Battens)
                Batten(wood, leather, k, -aft * k.B, step);
            Tiller(wood, leather, k);
        }

        // A plank cut square about the outline's length at its middle, bound to the stock by a thong round both.
        private static void Plank(MeshBuilder wood, MeshBuilder leather, KwRudderKit k, float z0, float z1, float byEye)
        {
            float z = (z0 + z1) / 2f, aft = -(k.AftAt(z) + byEye * k.B), h = Thong * k.BT;
            Solid.Box(wood, new Vector3(aft, -k.Half, z0), new Vector3(k.Fore, k.Half, z1));
            KwRound.Ring(leather, new Vector3(0f, 0f, z), KwRudderShapes.Up, k.Stock + 0.008f, h, 10, 4);
            Solid.Box(leather, new Vector3(-0.13f * k.B, -(k.Half + 0.008f), z - h), new Vector3(-k.Stock, k.Half + 0.008f, z + h));
        }

        // An upright batten on each face at x, from the foot up as far as the blade is wide enough, lashed to each plank.
        private static void Batten(MeshBuilder wood, MeshBuilder leather, KwRudderKit k, float x, float step)
        {
            float w = 0.035f * k.BT, t = 0.025f * k.BT, top = k.Reach(-x + 0.06f * k.B), wrap = k.Half + t + 0.006f;
            KwRudderShapes.Plates(wood, x - w, x + w, k.Foot + 0.03f * k.B, top, k.Half, t);
            for (float z = k.Foot + step / 2f; z < top; z += step)
                Solid.Box(leather, new Vector3(x - 1.4f * w, -wrap, z - Thong * k.BT), new Vector3(x + 1.4f * w, wrap, z + Thong * k.BT));
        }

        // A pole through the stock's top, lashed round the stock above and below it and round itself in front; for a yoke,
        // a pole across the stock lashed the same way.
        private static void Tiller(MeshBuilder wood, MeshBuilder leather, KwRudderKit k)
        {
            float t = k.T, z = k.Lay.Yoked ? k.YokeZ : k.HeadBase - 0.02f + 0.12f * t, r = 0.035f * t;
            foreach (float dz in new[] { -0.05f, 0.05f })
                KwRound.Ring(leather, new Vector3(0f, 0f, z + dz * t), KwRudderShapes.Up, k.Stock + 0.008f, Thong * t, 10, 4);
            if (KwRudderLinkShapes.Yoked(wood, leather, k, r, 6))
                return;
            Vector3 from = new Vector3(-k.Stock - 0.03f, 0f, z), to = k.TillerTo(new Vector3(1f * k.S, 0f, z + 0.06f * t));
            Solid.Log(wood, from, to, r, 6);
            Vector3 along = (to - from).normalized;
            foreach (float at in new[] { 0.06f, 0.1f })
                KwRound.Ring(leather, from + along * (k.Stock + 0.03f + at * k.S), along, r + 0.006f, Thong * t, 8, 4);
            if (k.Lay.Kind == KwRudderHelm.Tiller)
                KwRudderLinkShapes.Peg(wood, leather, to);
        }
    }
}
