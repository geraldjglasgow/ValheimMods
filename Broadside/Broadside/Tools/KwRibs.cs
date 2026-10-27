using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Ribs: the frames the rib tool stands on a keel (<see cref="KwShapeTool"/>) to give the hull its shape, of any hull
    /// shape (family), beam and height: a U-shaped frame (0.2 m thick, 0.16 m deep) just inside the planking along its
    /// cross-section on both sides, in its keel tier's look (<see cref="KwKeelUv"/>). The planking, deck and railing tools
    /// (<see cref="KwTools"/>) build between ribs from the shape and frame a rib saves in its ZDO, "rib|shape|frame".
    /// </summary>
    public static class KwRibs
    {
        public const string Made = "KW_gen_rib";
        private const string Tag = "rib";
        private const float Thick = 0.1f, Width = 0.16f;

        /// <summary>How far a rib's bars stand in from the planking's inside face, and its floor timber's top over the keel line.</summary>
        public const float BarDepth = Width, TimberTop = KwPlanks.Thick + Width;
        /// <summary>Every hull shape, in the rib tool's order.</summary>
        public static readonly string[] Families = { "viking", "round", "rounded", "galleon", "flat", "deep", "wineglass" };

        /// <summary>
        /// A rib of the player's own size, as the rib tool builds it and its preview shows it. climb: how far up its keel its
        /// foot stands over the level keel line (<see cref="KwKeelLines.Climb"/>), whose height its top reaches over that
        /// line, as the parts meeting it do (<see cref="KwSeat"/>); 0 where it is not known.
        /// </summary>
        public static KwDef Custom(string family, KwFrame frame, float climb = 0f)
        {
            Vector2[] half = KwJoints.Shape(family, frame, 1f, 0f, frame.Height - climb);
            return new KwDef(Made, $"Rib, {family}, {frame.Label}", "rib") { Wood = true, Shape = b => Shape(b, half) };
        }

        public static string Save(string family, KwFrame frame) => string.Join("|", Tag, family, frame);

        /// <summary>A made rib's shape and frame from what its ZDO saves.</summary>
        public static bool TryRead(string saved, out string family, out KwFrame frame)
        {
            string[] p = (saved ?? "").Split('|');
            family = p.Length == 3 ? p[1] : "";
            frame = default;
            return p.Length == 3 && p[0] == Tag && System.Array.IndexOf(Families, family) >= 0 && KwFrame.TryParse(p[2], out frame);
        }

        /// <summary>Whether a piece is a rib, and its shape and frame.</summary>
        public static bool Of(Piece piece, out string family, out KwFrame frame)
        {
            family = "";
            frame = default;
            if (Utils.GetPrefabName(piece.gameObject) != Made)
                return false;
            ZDO? zdo = piece.GetComponent<ZNetView>()?.GetZDO();
            return zdo != null && TryRead(zdo.GetString(KwGenerated.Key), out family, out frame);
        }

        // Each bar mapped along the frame from the keel (KwKeelUv), so the grain and any fittings run round it.
        private static void Shape(MeshBuilder b, Vector2[] half)
        {
            foreach (float side in new[] { 1f, -1f })
            {
                float round = 0f;
                for (int i = 0; i + 1 < half.Length; i++)
                {
                    float next = round + Vector2.Distance(half[i], half[i + 1]);
                    Solid.Hexa(b, Segment(side, half[i], half[i + 1]), KwKeelUv.Bar(round, next));
                    round = next;
                }
            }
            // the floor timber that ties both sides across the top of the keel
            float across = half[0].x + 0.06f;
            Solid.Hexa(b, Box(new Vector3(-Thick, -across, KwPlanks.Thick), new Vector3(Thick, across, TimberTop)), KwKeelUv.Floor());
        }

        private static Vector3[] Box(Vector3 min, Vector3 max) => new[]
        {
            new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
        };

        // One bar of the frame from section point a to c, Width deep, standing just inside the planking (which is
        // KwPlanks.Thick thick inward of the section) so the hull hides it from outside; Thick each way in x.
        private static Vector3[] Segment(float side, Vector2 a, Vector2 c)
        {
            Vector2 along = (c - a).normalized, inward = new Vector2(-along.y, along.x);
            Vector2 a0 = a + inward * KwPlanks.Thick, c0 = c + inward * KwPlanks.Thick, deep = inward * Width;
            Vector3 P(Vector2 p, float x) => new Vector3(x, side * p.x, p.y);
            return new[]
            {
                P(a0, -Thick), P(c0, -Thick), P(c0 + deep, -Thick), P(a0 + deep, -Thick),
                P(a0, Thick), P(c0, Thick), P(c0 + deep, Thick), P(a0 + deep, Thick),
            };
        }
    }
}
