using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Black Forest rudder, of fine wood, resin and bronze nails: five fitted fine-wood boards on a round stock, their
    /// seams payed with dark resin standing a little proud, bronze nail heads in two rows down each face where the boards
    /// are nailed to the battens inside; three bronze gudgeon straps round the post, two nails a side; a square fine-wood
    /// head with a bronze band and a tapered tiller with a bronze ferrule, or a fine-wood yoke with bronze bands, and rope
    /// lines. The fine wood is the table's, the resin that wood darkened, the bronze the bronze bars'.
    /// </summary>
    public static class KwRudderTierBlackForest
    {
        private const int Boards = 5;
        private static readonly float[] Gudgeons = { 0.1f, 0.37f, 0.64f };
        private static readonly float[] Rows = { 0.3f, 0.62f };
        private static readonly float[] PostNails = { 0.3f, 0.75f };

        public static KwRudderTier Tier { get; } = new KwRudderTier("blackforest", "Rudder, Black Forest", "fine wood, resin and bronze", new[]
        {
            new Vector2(0.05f, 0.85f), new Vector2(0.8f, 0.9f), new Vector2(1.6f, 0.8f), new Vector2(2.2f, 0.45f), new Vector2(2.45f, 0.1f),
        })
        {
            Made = "Fitted fine-wood boards, resin in the seams, bronze-nailed and hung on bronze straps.",
            Recipe = new[] { ("FineWood", 10), ("Resin", 6), ("BronzeNails", 16) },
            Steering = 1.05f,
            StockRadius = 0.06f,
            BladeHalf = 0.04f,
            Wood = KwLook.FineWood,
            Fitting = KwLook.Bronze,
            Fixed = Straps,
            Blade = Blade,
        };

        // Three bronze straps round the post, each with its knuckle and two nails a side.
        private static void Straps(KwRudderKit k)
        {
            MeshBuilder bronze = k.Fixed(KwLook.Bronze);
            float h = 0.07f * k.T, t = 0.014f;
            foreach (float share in Gudgeons)
            {
                float z = share * k.Top;
                KwRudderShapes.Gudgeon(bronze, k, z, h, t);
                foreach (float along in PostNails)
                    KwRudderShapes.Studs(bronze, along * k.Fit.PostDepth, z + h / 2f, k.Fit.PostHalf + t, 0.014f * k.T);
            }
        }

        private static void Blade(KwRudderKit k)
        {
            MeshBuilder wood = k.Blade(KwLook.FineWood), resin = k.Blade(KwLook.Resin), bronze = k.Blade(KwLook.Bronze);
            KwRudderShapes.Stock(wood, k, 0.2f * k.S, 10);
            float step = (k.Tip - k.Foot) / Boards, seam = 0.007f * k.BT;
            for (int i = 0; i < Boards; i++)
            {
                float z0 = k.Foot + i * step;
                KwRudderShapes.Board(wood, k, z0, z0 + step, k.Fore, k.Half);
                if (i > 0)
                    KwRudderShapes.Board(resin, k, z0 - seam, z0 + seam, k.Fore, k.Half + 0.003f);
                Nails(bronze, k, z0 + step / 2f);
            }
            Head(wood, bronze, k);
        }

        // A nail in each row that crosses the board at z, on both faces.
        private static void Nails(MeshBuilder bronze, KwRudderKit k, float z)
        {
            foreach (float row in Rows)
            {
                if (k.AftAt(z) > (row + 0.06f) * k.B)
                    KwRudderShapes.Studs(bronze, -row * k.B, z, k.Half, 0.013f * k.BT);
            }
        }

        // A square fine-wood head on the stock over the post's top, banded in bronze; a tapered tiller with a bronze ferrule,
        // or the yoke.
        private static void Head(MeshBuilder wood, MeshBuilder bronze, KwRudderKit k)
        {
            float t = k.T, z = k.HeadBase, head = 0.08f * t, band = head + 0.006f;
            Solid.Box(wood, new Vector3(-head, -head, z), new Vector3(head, head, z + 0.15f * t));
            Solid.Box(bronze, new Vector3(-band, -band, z + 0.1f * t), new Vector3(band, band, z + 0.13f * t));
            if (KwRudderLinkShapes.Yoked(wood, bronze, k, 0.035f * t, 8))
                return;
            Vector3 from = new Vector3(head, 0f, z + 0.07f * t), to = k.TillerTo(new Vector3(1.1f * k.S, 0f, z + 0.12f * t));
            KwRound.Taper(wood, from, to, 0.04f * t, 0.028f * t, 8);
            float length = (to - from).magnitude;
            KwRudderShapes.Sleeve(bronze, from, to, length - 0.06f * t, 0.065f * t, 0.033f * t);
            KwRudderLinkShapes.Grip(wood, bronze, k, to);
        }
    }
}
