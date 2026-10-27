using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Mistlands rudder, of Yggdrasil wood and eitr, with no metal: a pale foil-shaped blade in one piece on a twelve-
    /// sided stock, glowing eitr inlaid in its faces (a line following the after edge and two lines across); three carved
    /// Yggdrasil collars round the post, each ringed with an eitr line and its knuckle with an eitr band; a six-sided head
    /// with an eitr band and a tapered tiller with an eitr ring near its end, or a yoke ringed in eitr, and rope lines. The
    /// wood is the Yggdrasil wood's own, the glow the refined eitr's.
    /// </summary>
    public static class KwRudderTierMistlands
    {
        private static readonly float[] Gudgeons = { 0.1f, 0.37f, 0.64f };
        private static readonly float[] Lines = { 1f / 3f, 2f / 3f };

        public static KwRudderTier Tier { get; } = new KwRudderTier("mistlands", "Rudder, Mistlands", "Yggdrasil wood and eitr", new[]
        {
            new Vector2(0.05f, 0.55f), new Vector2(0.3f, 0.95f), new Vector2(0.9f, 1.1f), new Vector2(1.7f, 1.02f), new Vector2(2.3f, 0.6f), new Vector2(2.6f, 0.12f),
        })
        {
            Made = "A pale Yggdrasil-wood blade in one piece, inlaid with glowing eitr.",
            Recipe = new[] { ("YggdrasilWood", 12), ("Eitr", 4) },
            Steering = 1.25f,
            StockRadius = 0.07f,
            BladeHalf = 0.045f,
            Wood = KwLook.Yggdrasil,
            Fitting = KwLook.Eitr,
            Fixed = Collars,
            Blade = Blade,
        };

        // Three carved collars round the post and the stock, an eitr line round each collar's middle.
        private static void Collars(KwRudderKit k)
        {
            MeshBuilder wood = k.Fixed(KwLook.Yggdrasil), eitr = k.Fixed(KwLook.Eitr);
            float h = 0.09f * k.T, t = 0.03f, line = 0.012f * k.T;
            foreach (float share in Gudgeons)
            {
                float z = share * k.Top, mid = z + (h - line) / 2f;
                KwRudderShapes.Gudgeon(wood, k, z, h, t, 12);
                KwRudderShapes.PostBand(eitr, k.Fit, mid, line, t + 0.004f);
                Solid.Log(eitr, k.Pivot + new Vector3(0f, 0f, mid), k.Pivot + new Vector3(0f, 0f, mid + line), k.Stock + t + 0.004f, 12);
            }
        }

        private static void Blade(KwRudderKit k)
        {
            MeshBuilder wood = k.Blade(KwLook.Yggdrasil), eitr = k.Blade(KwLook.Eitr);
            KwRudderShapes.Stock(wood, k, 0.2f * k.S, 12);
            KwRudderShapes.Board(wood, k, k.Foot, k.Tip, k.Fore, k.Half);
            Inlay(eitr, k);
            Head(wood, eitr, k);
        }

        // Eitr lines standing just proud of both faces: one following the after edge 10 cm in, two across to meet it.
        private static void Inlay(MeshBuilder eitr, KwRudderKit k)
        {
            float half = k.Half + 0.003f, inset = 0.1f * k.B, width = 0.014f * k.BT, from = k.Fore - 0.1f * k.B;
            float top = k.Reach(inset + 0.12f * k.B);
            KwRudderShapes.EdgeStrip(eitr, k, k.Foot + 0.08f * k.B, top, inset, width, half);
            foreach (float share in Lines)
            {
                float z = Mathf.Lerp(k.Foot, top, share), to = inset - k.AftAt(z);
                Solid.Box(eitr, new Vector3(to, -half, z), new Vector3(from, half, z + width));
            }
        }

        // A six-sided head with an eitr band, a tapered tiller with an eitr ring near its end, or the yoke.
        private static void Head(MeshBuilder wood, MeshBuilder eitr, KwRudderKit k)
        {
            float t = k.T, z = k.HeadBase, r = 0.1f * t, tall = 0.16f * t;
            Solid.Log(wood, new Vector3(0f, 0f, z), new Vector3(0f, 0f, z + tall), r, 6);
            Solid.Log(eitr, new Vector3(0f, 0f, z + 0.1f * t), new Vector3(0f, 0f, z + 0.115f * t), r + 0.004f, 6);
            if (KwRudderLinkShapes.Yoked(wood, eitr, k, 0.034f * t, 12))
                return;
            Vector3 from = new Vector3(r - 0.01f, 0f, z + 0.07f * t), to = k.TillerTo(new Vector3(1.15f * k.S, 0f, z + 0.13f * t));
            KwRound.Taper(wood, from, to, 0.04f * t, 0.026f * t, 12);
            float length = (to - from).magnitude;
            KwRudderShapes.Sleeve(eitr, from, to, length * 0.86f, 0.015f * t, Mathf.Lerp(0.04f, 0.026f, 0.86f) * t + 0.004f, 12);
            KwRudderLinkShapes.Grip(wood, eitr, k, to);
        }
    }
}
