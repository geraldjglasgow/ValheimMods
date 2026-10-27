using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Mountains rudder, of ancient bark and silver: a tall, slender blade of three broad bark-wood boards, its whole
    /// after edge and its foot bound in silver with silver studs along the binding; three slim silver gudgeons, each
    /// knuckle beaded above and below, a silver boss a side; a round bark head under a silver cap and ring, and a round
    /// tiller with two silver rings and a silver knob, or a round yoke ringed in silver, and rope lines. The bark is the
    /// ancient bark's own, the silver the silver bars'.
    /// </summary>
    public static class KwRudderTierMountains
    {
        private const int Boards = 3;
        private static readonly float[] Gudgeons = { 0.1f, 0.37f, 0.64f };

        public static KwRudderTier Tier { get; } = new KwRudderTier("mountains", "Rudder, Mountains", "ancient bark and silver", new[]
        {
            new Vector2(0.05f, 0.6f), new Vector2(0.3f, 0.88f), new Vector2(1.9f, 0.9f), new Vector2(2.45f, 0.52f), new Vector2(2.7f, 0.12f),
        })
        {
            Made = "A tall bark-wood blade bound in silver along its edge, on slim silver gudgeons.",
            Recipe = new[] { ("ElderBark", 12), ("Silver", 6) },
            Steering = 1.15f,
            StockRadius = 0.065f,
            BladeHalf = 0.04f,
            Wood = KwLook.Bark,
            Fitting = KwLook.Silver,
            Fixed = Straps,
            Blade = Blade,
        };

        // Three slim silver straps, each knuckle with a bead ring over and under it, and a silver boss on each side.
        private static void Straps(KwRudderKit k)
        {
            MeshBuilder silver = k.Fixed(KwLook.Silver);
            float h = 0.05f * k.T, t = 0.012f;
            foreach (float share in Gudgeons)
            {
                float z = share * k.Top;
                KwRudderShapes.Gudgeon(silver, k, z, h, t, 12);
                foreach (float at in new[] { z, z + h })
                    KwRound.Ring(silver, k.Pivot + new Vector3(0f, 0f, at), KwRudderShapes.Up, k.Stock + t, 0.008f * k.T, 12, 5);
                KwRudderShapes.Studs(silver, 0.5f * k.Fit.PostDepth, z + h / 2f, k.Fit.PostHalf + t, 0.02f * k.T);
            }
        }

        private static void Blade(KwRudderKit k)
        {
            MeshBuilder bark = k.Blade(KwLook.Bark), silver = k.Blade(KwLook.Silver);
            KwRudderShapes.Stock(bark, k, 0.2f * k.S, 10);
            float step = (k.Tip - k.Foot) / Boards;
            for (int i = 0; i < Boards; i++)
                KwRudderShapes.Board(bark, k, k.Foot + i * step, k.Foot + (i + 1) * step, k.Fore, k.Half);
            Binding(silver, k);
            Head(bark, silver, k);
        }

        // Silver along the after edge and under the foot, a little proud of the faces, studded every 35 cm.
        private static void Binding(MeshBuilder silver, KwRudderKit k)
        {
            float half = k.Half + 0.006f, width = 0.04f * k.B;
            KwRudderShapes.EdgeStrip(silver, k, k.Foot, k.Tip, -0.008f, width, half);
            KwRudderShapes.FootBinding(silver, k, 0.015f * k.BT, 0.03f * k.BT, half);
            for (float z = k.Foot + 0.2f * k.B; z < k.Tip - 0.15f * k.B; z += 0.35f * k.B)
                KwRudderShapes.Studs(silver, -k.AftAt(z) + width / 2f, z, half, 0.012f * k.BT);
        }

        // A round bark head under a silver cap with a silver ring at its foot; a round tiller with two silver rings and a
        // knob, or the yoke.
        private static void Head(MeshBuilder bark, MeshBuilder silver, KwRudderKit k)
        {
            float t = k.T, z = k.HeadBase, r = 0.085f * t, tall = 0.14f * t;
            Solid.Log(bark, new Vector3(0f, 0f, z), new Vector3(0f, 0f, z + tall), r, 10);
            Solid.Log(silver, new Vector3(0f, 0f, z + tall), new Vector3(0f, 0f, z + tall + 0.025f * t), r + 0.005f, 10);
            KwRound.Ring(silver, new Vector3(0f, 0f, z + 0.01f * t), KwRudderShapes.Up, r, 0.01f * t, 12, 5);
            if (KwRudderLinkShapes.Yoked(bark, silver, k, 0.034f * t, 10))
                return;
            Vector3 from = new Vector3(r - 0.01f, 0f, z + 0.07f * t), to = k.TillerTo(new Vector3(1.1f * k.S, 0f, z + 0.12f * t));
            KwRound.Taper(bark, from, to, 0.038f * t, 0.027f * t, 10);
            Vector3 along = (to - from).normalized;
            float length = (to - from).magnitude;
            foreach (float share in new[] { 0.4f, 0.8f })
                KwRound.Ring(silver, from + along * share * length, along, Mathf.Lerp(0.038f, 0.027f, share) * t, 0.007f * t, 10, 4);
            Solid.Log(silver, to - along * 0.01f, to + along * 0.04f * t, 0.034f * t, 10);
            KwRudderLinkShapes.Grip(bark, silver, k, to);
        }
    }
}
