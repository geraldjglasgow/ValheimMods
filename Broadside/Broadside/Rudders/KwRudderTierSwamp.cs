using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Swamp rudder, of ancient bark and iron nails: four thick boards of dark bark wood on a stout eight-sided stock,
    /// an upright blade with a raked heel. Three heavy iron straps round the post with rows of nail heads down its sides;
    /// on the blade, three iron pintle straps just under them, each with a collar round the stock and a row of nails along
    /// both faces; an iron-banded head and a squared tiller with an iron band at its end, or a squared yoke with iron rods
    /// to the deck tiller. The dark wood is the darkwood beams' material, the iron the iron walls'.
    /// </summary>
    public static class KwRudderTierSwamp
    {
        private const int Boards = 4, StrapNails = 4;
        private static readonly float[] Gudgeons = { 0.1f, 0.37f, 0.64f };
        private static readonly float[] PostNails = { 0.2f, 0.5f, 0.8f };

        public static KwRudderTier Tier { get; } = new KwRudderTier("swamp", "Rudder, Swamp", "ancient bark and iron", new[]
        {
            new Vector2(0.05f, 0.7f), new Vector2(0.2f, 0.92f), new Vector2(1.7f, 0.95f), new Vector2(2.25f, 0.55f), new Vector2(2.45f, 0.12f),
        })
        {
            Made = "Thick dark bark-wood boards, iron-strapped and nailed in rows.",
            Recipe = new[] { ("ElderBark", 10), ("IronNails", 20) },
            Steering = 1.1f,
            StockRadius = 0.07f,
            BladeHalf = 0.045f,
            Wood = KwLook.DarkWood,
            Fitting = KwLook.Iron,
            Rod = KwLook.Iron,
            Fixed = Straps,
            Blade = Blade,
        };

        // Three heavy iron straps round the post, each with its knuckle and a row of three nails a side.
        private static void Straps(KwRudderKit k)
        {
            MeshBuilder iron = k.Fixed(KwLook.Iron);
            float h = 0.1f * k.T, t = 0.02f;
            foreach (float share in Gudgeons)
            {
                float z = share * k.Top;
                KwRudderShapes.Gudgeon(iron, k, z, h, t, 8);
                foreach (float along in PostNails)
                    KwRudderShapes.Studs(iron, along * k.Fit.PostDepth, z + h / 2f, k.Fit.PostHalf + t, 0.016f * k.T);
            }
        }

        private static void Blade(KwRudderKit k)
        {
            MeshBuilder wood = k.Blade(KwLook.DarkWood), iron = k.Blade(KwLook.Iron);
            KwRudderShapes.Stock(wood, k, 0.2f * k.S, 8);
            float step = (k.Tip - k.Foot) / Boards;
            for (int i = 0; i < Boards; i++)
                KwRudderShapes.Board(wood, k, k.Foot + i * step, k.Foot + (i + 1) * step, k.Fore, k.Half);
            foreach (float share in Gudgeons)
                Pintle(iron, k, k.OnBlade(share) - 0.012f);
            Head(wood, iron, k);
        }

        // An iron strap on both faces from the stock most of the way aft, ending under z, a collar round the stock, nails along it.
        private static void Pintle(MeshBuilder iron, KwRudderKit k, float z1)
        {
            float z0 = z1 - 0.07f * k.BT, z = (z0 + z1) / 2f, t = 0.01f, end = -0.72f * k.AftAt(z);
            KwRudderShapes.Plates(iron, k.Fore, end, z0, z1, k.Half, t);
            KwRudderShapes.Collar(iron, k, z0, z1 - z0, 0.012f, 8);
            for (int i = 0; i < StrapNails; i++)
            {
                float x = Mathf.Lerp(-0.12f * k.B, end + 0.04f * k.B, i / (StrapNails - 1f));
                KwRudderShapes.Studs(iron, x, z, k.Half + t, 0.014f * k.BT);
            }
        }

        // A dark-wood head banded in iron top and bottom, a squared tiller narrowing forward with an iron band at its end,
        // or the yoke.
        private static void Head(MeshBuilder wood, MeshBuilder iron, KwRudderKit k)
        {
            float t = k.T, z = k.HeadBase, head = 0.09f * t, band = head + 0.008f, tall = 0.16f * t;
            Solid.Box(wood, new Vector3(-head, -head, z), new Vector3(head, head, z + tall));
            foreach (float at in new[] { 0.01f * t, tall - 0.04f * t })
                Solid.Box(iron, new Vector3(-band, -band, z + at), new Vector3(band, band, z + at + 0.03f * t));
            if (KwRudderLinkShapes.Yoked(wood, iron, k, 0.036f * t, 4))
                return;
            Vector3 from = new Vector3(head, 0f, z + 0.08f * t), to = k.TillerTo(new Vector3(1.05f * k.S, 0f, z + 0.12f * t));
            KwRudderShapes.Beam(wood, from, to, 0.036f * t, 0.026f * t);
            Vector3 cap = to - (to - from).normalized * 0.08f * t;
            KwRudderShapes.Beam(iron, cap, to + (to - from).normalized * 0.005f, 0.031f * t, 0.031f * t);
            KwRudderLinkShapes.Grip(wood, iron, k, to);
        }
    }
}
