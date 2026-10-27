using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Ashlands rudder, of ashwood and flametal, the largest: a deep blade of five ashwood boards on a stock shod in
    /// flametal at its foot, its after edge and foot capped in flametal with a faint ember glow along the cap's inner edge,
    /// three flametal pintle straps studded on both faces; three massive flametal gudgeons, each with an ember seam round
    /// its middle; a flametal-capped head and an ashwood tiller with a flametal end and an ember ring by the head, or a yoke
    /// with flametal rods to the deck tiller. The ashwood is the ashwood walls', the flametal the flametal pillar's, the
    /// ember the eitr's glow made warm and dim.
    /// </summary>
    public static class KwRudderTierAshlands
    {
        private const int Boards = 5, StrapStuds = 3;
        private static readonly float[] Gudgeons = { 0.1f, 0.37f, 0.64f };

        public static KwRudderTier Tier { get; } = new KwRudderTier("ashlands", "Rudder, Ashlands", "ashwood and flametal", new[]
        {
            new Vector2(0.05f, 0.8f), new Vector2(0.25f, 1.12f), new Vector2(1.5f, 1.15f), new Vector2(2.2f, 0.78f), new Vector2(2.6f, 0.15f),
        })
        {
            Made = "A deep ashwood blade capped and strapped in flametal, its seams faintly glowing.",
            Recipe = new[] { ("Blackwood", 12), ("FlametalNew", 6) },
            Steering = 1.3f,
            StockRadius = 0.075f,
            BladeHalf = 0.05f,
            Wood = KwLook.Ashwood,
            Fitting = KwLook.Flametal,
            Rod = KwLook.Flametal,
            Fixed = Straps,
            Blade = Blade,
        };

        // Three massive flametal straps with their knuckles, an ember seam round the middle of each.
        private static void Straps(KwRudderKit k)
        {
            MeshBuilder flametal = k.Fixed(KwLook.Flametal), ember = k.Fixed(KwLook.Ember);
            float h = 0.11f * k.T, t = 0.022f, seam = 0.01f * k.T;
            foreach (float share in Gudgeons)
            {
                float z = share * k.Top, mid = z + (h - seam) / 2f;
                KwRudderShapes.Gudgeon(flametal, k, z, h, t);
                KwRudderShapes.PostBand(ember, k.Fit, mid, seam, t + 0.003f);
                Solid.Log(ember, k.Pivot + new Vector3(0f, 0f, mid), k.Pivot + new Vector3(0f, 0f, mid + seam), k.Stock + t + 0.003f, 10);
            }
        }

        private static void Blade(KwRudderKit k)
        {
            MeshBuilder wood = k.Blade(KwLook.Ashwood), flametal = k.Blade(KwLook.Flametal), ember = k.Blade(KwLook.Ember);
            KwRudderShapes.Stock(wood, k, 0.2f * k.S, 10);
            KwRudderShapes.Collar(flametal, k, k.StockFoot, k.Foot - k.StockFoot + 0.1f * k.B, 0.012f);
            float step = (k.Tip - k.Foot) / Boards;
            for (int i = 0; i < Boards; i++)
                KwRudderShapes.Board(wood, k, k.Foot + i * step, k.Foot + (i + 1) * step, k.Fore, k.Half);
            Cap(flametal, ember, k);
            foreach (float share in Gudgeons)
                Pintle(flametal, k, k.OnBlade(share) - 0.015f);
            Head(wood, flametal, ember, k);
        }

        // Flametal over the after edge and the foot, an ember line glowing just inside the cap on both faces.
        private static void Cap(MeshBuilder flametal, MeshBuilder ember, KwRudderKit k)
        {
            float half = k.Half + 0.008f, width = 0.07f * k.B;
            KwRudderShapes.EdgeStrip(flametal, k, k.Foot, k.Tip, -0.01f, width, half);
            KwRudderShapes.FootBinding(flametal, k, 0.02f * k.BT, 0.045f * k.BT, half);
            float top = k.Reach(width + 0.05f * k.B);
            KwRudderShapes.EdgeStrip(ember, k, k.Foot + 0.045f * k.BT, top, width - 0.01f, 0.012f * k.BT, k.Half + 0.004f);
        }

        // A flametal strap on both faces from the stock half way aft, ending under z, with a collar and studs along it.
        private static void Pintle(MeshBuilder flametal, KwRudderKit k, float z1)
        {
            float z0 = z1 - 0.08f * k.BT, z = (z0 + z1) / 2f, t = 0.012f, end = -0.6f * k.AftAt(z);
            KwRudderShapes.Plates(flametal, k.Fore, end, z0, z1, k.Half, t);
            KwRudderShapes.Collar(flametal, k, z0, z1 - z0, 0.014f);
            for (int i = 0; i < StrapStuds; i++)
                KwRudderShapes.Studs(flametal, Mathf.Lerp(-0.14f * k.B, end + 0.05f * k.B, i / (StrapStuds - 1f)), z, k.Half + t, 0.016f * k.BT);
        }

        // An ashwood head under a flametal cap; an ashwood tiller with a flametal end and an ember ring by the head, or the
        // yoke.
        private static void Head(MeshBuilder wood, MeshBuilder flametal, MeshBuilder ember, KwRudderKit k)
        {
            float t = k.T, z = k.HeadBase, head = 0.1f * t, cap = head + 0.01f, tall = 0.17f * t;
            Solid.Box(wood, new Vector3(-head, -head, z), new Vector3(head, head, z + tall));
            Solid.Box(flametal, new Vector3(-cap, -cap, z + tall - 0.06f * t), new Vector3(cap, cap, z + tall + 0.012f * t));
            if (KwRudderLinkShapes.Yoked(wood, flametal, k, 0.04f * t, 10))
                return;
            Vector3 from = new Vector3(head, 0f, z + 0.08f * t), to = k.TillerTo(new Vector3(1.2f * k.S, 0f, z + 0.13f * t));
            KwRound.Taper(wood, from, to, 0.045f * t, 0.032f * t, 10);
            float length = (to - from).magnitude;
            KwRudderShapes.Sleeve(ember, from, to, 0.05f * t, 0.012f * t, 0.049f * t, 10);
            KwRudderShapes.Sleeve(flametal, from, to, length - 0.1f * t, 0.12f * t, 0.037f * t, 10);
            KwRudderLinkShapes.Grip(wood, flametal, k, to);
        }
    }
}
