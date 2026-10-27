using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Plains rudder, of black metal and linen thread on carved fine-wood boards: a broad blade of four boards held in
    /// a black metal frame (along the after edge, under the foot and up the fore edge by the stock) with two black metal
    /// bands across it, riveted where they meet the frame; black metal gudgeons with a spine plate down the post's after
    /// face between them; a black metal cap on the head, and a tiller whose grip is wrapped in linen, or a yoke with black
    /// metal rods to the deck tiller. The boards are the raven throne's carved fine wood, the black metal the black metal
    /// bars', the linen the linen thread's.
    /// </summary>
    public static class KwRudderTierPlains
    {
        private const int Boards = 4, Turns = 7;
        private static readonly float[] Gudgeons = { 0.1f, 0.37f, 0.64f };
        private static readonly float[] Bands = { 1f / 3f, 2f / 3f };

        public static KwRudderTier Tier { get; } = new KwRudderTier("plains", "Rudder, Plains", "black metal and linen", new[]
        {
            new Vector2(0.05f, 0.9f), new Vector2(0.2f, 1.02f), new Vector2(1.4f, 1.05f), new Vector2(2.1f, 0.7f), new Vector2(2.5f, 0.12f),
        })
        {
            Made = "A broad blade framed and banded in black metal, its tiller's grip wrapped in linen.",
            Recipe = new[] { ("FineWood", 10), ("BlackMetal", 6), ("LinenThread", 10) },
            Steering = 1.2f,
            StockRadius = 0.07f,
            BladeHalf = 0.045f,
            Wood = KwLook.CarvedWood,
            Fitting = KwLook.BlackMetal,
            Rod = KwLook.BlackMetal,
            Fixed = Straps,
            Blade = Blade,
        };

        // Three black metal straps with their knuckles and two rivets a side, a spine plate on the post's after face between them.
        private static void Straps(KwRudderKit k)
        {
            MeshBuilder metal = k.Fixed(KwLook.BlackMetal);
            float h = 0.08f * k.T, t = 0.016f, spine = 0.05f * k.T;
            foreach (float share in Gudgeons)
            {
                float z = share * k.Top;
                KwRudderShapes.Gudgeon(metal, k, z, h, t);
                foreach (float along in new[] { 0.25f, 0.75f })
                    KwRudderShapes.Studs(metal, along * k.Fit.PostDepth, z + h / 2f, k.Fit.PostHalf + t, 0.013f * k.T);
            }
            Solid.Box(metal, new Vector3(-0.008f, -spine, Gudgeons[0] * k.Top), new Vector3(0f, spine, Gudgeons[Gudgeons.Length - 1] * k.Top + h));
        }

        private static void Blade(KwRudderKit k)
        {
            MeshBuilder wood = k.Blade(KwLook.CarvedWood), metal = k.Blade(KwLook.BlackMetal);
            KwRudderShapes.Stock(wood, k, 0.2f * k.S, 10);
            float step = (k.Tip - k.Foot) / Boards;
            for (int i = 0; i < Boards; i++)
                KwRudderShapes.Board(wood, k, k.Foot + i * step, k.Foot + (i + 1) * step, k.Fore, k.Half);
            Frame(metal, k);
            Head(wood, metal, k.Blade(KwLook.Linen), k);
        }

        // The frame round the blade and the two bands across it, riveted at both ends.
        private static void Frame(MeshBuilder metal, KwRudderKit k)
        {
            float half = k.Half + 0.008f, edge = 0.06f * k.B, fore = k.Fore - 0.06f * k.B, band = 0.06f * k.BT;
            KwRudderShapes.EdgeStrip(metal, k, k.Foot, k.Tip, -0.01f, edge, half);
            KwRudderShapes.FootBinding(metal, k, 0.02f * k.BT, 0.04f * k.BT, half);
            Solid.Box(metal, new Vector3(fore, -half, k.Foot), new Vector3(k.Fore, half, k.Tip));
            foreach (float share in Bands)
            {
                float z = Mathf.Lerp(k.Foot, k.Tip, share), aft = -k.AftAt(z + band / 2f) + edge / 2f;
                KwRudderShapes.Board(metal, k, z, z + band, k.Fore, half);
                KwRudderShapes.Studs(metal, fore + 0.02f * k.B, z + band / 2f, half, 0.014f * k.BT);
                KwRudderShapes.Studs(metal, aft, z + band / 2f, half, 0.014f * k.BT);
            }
        }

        // A wooden head under a black metal cap; a tapered tiller with its last third wrapped in linen and a black metal end
        // cap, or the yoke.
        private static void Head(MeshBuilder wood, MeshBuilder metal, MeshBuilder linen, KwRudderKit k)
        {
            float t = k.T, z = k.HeadBase, head = 0.09f * t, cap = head + 0.008f, tall = 0.15f * t;
            Solid.Box(wood, new Vector3(-head, -head, z), new Vector3(head, head, z + tall));
            Solid.Box(metal, new Vector3(-cap, -cap, z + tall - 0.05f * t), new Vector3(cap, cap, z + tall + 0.01f * t));
            if (KwRudderLinkShapes.Yoked(wood, metal, k, 0.036f * t, 10))
                return;
            Vector3 from = new Vector3(head, 0f, z + 0.07f * t), to = k.TillerTo(new Vector3(1.15f * k.S, 0f, z + 0.12f * t));
            KwRound.Taper(wood, from, to, 0.04f * t, 0.03f * t, 10);
            Grip(linen, from, to, 0.038f * t);
            KwRudderShapes.Sleeve(metal, from, to, (to - from).magnitude - 0.01f, 0.05f * t, 0.035f * t, 10);
            KwRudderLinkShapes.Grip(wood, metal, k, to);
        }

        // Linen wound round the tiller's last third: a sleeve with the turns standing out on it.
        private static void Grip(MeshBuilder linen, Vector3 from, Vector3 to, float r)
        {
            Vector3 along = (to - from).normalized;
            float length = (to - from).magnitude, start = length * 0.62f, grip = length * 0.34f;
            KwRudderShapes.Sleeve(linen, from, to, start, grip, r, 10);
            for (int i = 0; i < Turns; i++)
                KwRound.Ring(linen, from + along * (start + (i + 0.5f) * grip / Turns), along, r, 0.005f, 10, 4);
        }
    }
}
