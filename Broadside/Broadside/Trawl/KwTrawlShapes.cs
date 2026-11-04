using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The net winch's meshes, in kit axes (x outboard, the way its davit reaches; y left; z up; metres; the origin on the
    /// deck under the winch's middle), each part's mesh about its own pivot: the frame (two sleepers tied at both ends, a
    /// cheek on each carrying the drum's axle, and the davit's post with two stays and a lead block at its head); its irons
    /// (bands round the cheeks at the axle and round the post's head); the drum on its axle (barrel and flanges, the
    /// anchor windlass's sizes, <see cref="KwAnchorShapes"/>) with its crank; the roll of net wound on the drum (a unit
    /// cylinder, scaled to how full it is); the davit arm (a collar round the post's head, the arm and its brace out to
    /// the sheave's cheeks) and its sheave.
    /// </summary>
    public static class KwTrawlShapes
    {
        /// <summary>Where the drum's axle runs (across), the davit's pivot on the post's head and the sheave in the davit's frame.</summary>
        public static readonly Vector3 AxleAt = new Vector3(-0.15f, 0f, KwAnchorShapes.Axle + 0.03f);
        public static readonly Vector3 PivotAt = new Vector3(0.40f, 0f, 2.40f), SheaveAt = new Vector3(1.50f, 0f, -0.14f);

        /// <summary>The lead block on the post (the rope from the drum runs over it up to the sheave).</summary>
        public static readonly Vector3 HeelAt = new Vector3(0.29f, 0f, 2.18f);

        /// <summary>The sheave's radius.</summary>
        public const float Sheave = 0.12f;

        /// <summary>The colliders: the frame with its drum, and the post (the arm, which swings, has none).</summary>
        public static readonly Vector3 BodyMin = new Vector3(-0.56f, -0.53f, 0f), BodyMax = new Vector3(0.48f, 0.53f, 0.84f);
        public static readonly Vector3 PostMin = new Vector3(0.32f, -0.08f, 0.08f), PostMax = new Vector3(0.48f, 0.08f, 2.56f);

        public static void Frame(MeshBuilder b)
        {
            foreach (float s in new[] { -1f, 1f })
            {
                Block(b, -0.55f, 0.47f, s * 0.40f, s * 0.52f, 0f, 0.08f);
                Cheek(b, s);
                Solid.Log(b, new Vector3(0.44f, s * 0.46f, 0.10f), new Vector3(0.40f, s * 0.06f, 0.95f), 0.035f, 6);
            }
            Block(b, 0.33f, 0.47f, -0.52f, 0.52f, 0.08f, 0.14f);
            Block(b, -0.55f, -0.43f, -0.52f, 0.52f, 0.08f, 0.14f);
            Solid.Log(b, new Vector3(PivotAt.x, 0f, 0.08f), new Vector3(PivotAt.x, 0f, PivotAt.z), 0.075f, 8);
            Solid.Log(b, new Vector3(PivotAt.x, 0f, PivotAt.z), new Vector3(PivotAt.x, 0f, PivotAt.z + 0.14f), 0.045f, 8);
            Block(b, HeelAt.x - 0.05f, HeelAt.x + 0.03f, -0.05f, 0.05f, HeelAt.z - 0.10f, HeelAt.z + 0.08f);
        }

        // A cheek: a board standing on its sleeper, narrowing to its top, the axle through it.
        private static void Cheek(MeshBuilder b, float s)
        {
            float y0 = Mathf.Min(s * 0.40f, s * 0.50f), y1 = Mathf.Max(s * 0.40f, s * 0.50f), x = AxleAt.x, top = AxleAt.z + 0.18f;
            Solid.Hexa(b, new[]
            {
                new Vector3(x - 0.22f, y0, 0.08f), new Vector3(x + 0.22f, y0, 0.08f), new Vector3(x + 0.22f, y1, 0.08f), new Vector3(x - 0.22f, y1, 0.08f),
                new Vector3(x - 0.09f, y0, top), new Vector3(x + 0.09f, y0, top), new Vector3(x + 0.09f, y1, top), new Vector3(x - 0.09f, y1, top),
            });
        }

        /// <summary>The bands: round each cheek at the axle, and round the post's head under the davit's collar.</summary>
        public static void Irons(MeshBuilder b)
        {
            foreach (float s in new[] { -1f, 1f })
                Block(b, AxleAt.x - 0.11f, AxleAt.x + 0.11f, s * 0.395f, s * 0.505f, AxleAt.z - 0.06f, AxleAt.z + 0.05f);
            Solid.Log(b, new Vector3(PivotAt.x, 0f, PivotAt.z - 0.26f), new Vector3(PivotAt.x, 0f, PivotAt.z - 0.18f), 0.085f, 8);
        }

        /// <summary>The drum about its axle: barrel, flanges and the axle through both cheeks.</summary>
        public static void Barrel(MeshBuilder b)
        {
            Solid.Log(b, new Vector3(0f, -0.33f, 0f), new Vector3(0f, 0.33f, 0f), KwAnchorShapes.Drum, 10);
            foreach (float s in new[] { -1f, 1f })
                Solid.Log(b, new Vector3(0f, s * KwAnchorShapes.FlangeIn, 0f), new Vector3(0f, s * KwAnchorShapes.FlangeOut, 0f), KwAnchorShapes.Flange, 14);
            Solid.Log(b, new Vector3(0f, -0.56f, 0f), new Vector3(0f, 0.62f, 0f), 0.035f, 8);
        }

        /// <summary>The crank on the axle's left end: its arm and its grip, turning with the drum.</summary>
        public static void Crank(MeshBuilder b)
        {
            Block(b, -0.025f, 0.025f, 0.58f, 0.61f, -0.02f, 0.27f);
            Solid.Log(b, new Vector3(0f, 0.61f, 0.25f), new Vector3(0f, 0.79f, 0.25f), 0.022f, 6);
        }

        /// <summary>The roll of net between the flanges: a cylinder of radius 1 along the axle, scaled to its radius.</summary>
        public static void Roll(MeshBuilder b) => Solid.Log(b, new Vector3(0f, -0.32f, 0f), new Vector3(0f, 0.32f, 0f), 1f, 16);

        /// <summary>The davit about its pivot: a collar round the post's head, the arm out to the sheave, its brace from a lower collar, the sheave's cheeks.</summary>
        public static void Arm(MeshBuilder b)
        {
            Solid.Log(b, new Vector3(0f, 0f, -0.08f), new Vector3(0f, 0f, 0.10f), 0.10f, 10);
            Solid.Log(b, new Vector3(0.05f, 0f, 0.04f), new Vector3(SheaveAt.x + 0.14f, 0f, 0.04f), 0.06f, 8);
            Solid.Log(b, new Vector3(0f, 0f, -0.68f), new Vector3(0f, 0f, -0.56f), 0.10f, 10);
            Solid.Log(b, new Vector3(0.06f, 0f, -0.62f), new Vector3(0.80f, 0f, 0f), 0.04f, 6);
            foreach (float s in new[] { -1f, 1f })
                Block(b, SheaveAt.x - 0.15f, SheaveAt.x + 0.15f, s * 0.045f, s * 0.065f, SheaveAt.z - 0.16f, 0.02f);
        }

        /// <summary>The sheave about its pin: a wheel and the pin through the cheeks.</summary>
        public static void Wheel(MeshBuilder b)
        {
            Solid.Log(b, new Vector3(0f, -0.035f, 0f), new Vector3(0f, 0.035f, 0f), Sheave, 12);
            Solid.Log(b, new Vector3(0f, -0.075f, 0f), new Vector3(0f, 0.075f, 0f), 0.02f, 6);
        }

        private static void Block(MeshBuilder b, float x0, float x1, float y0, float y1, float z0, float z1) =>
            Solid.Box(b, new Vector3(Mathf.Min(x0, x1), Mathf.Min(y0, y1), Mathf.Min(z0, z1)), new Vector3(Mathf.Max(x0, x1), Mathf.Max(y0, y1), Mathf.Max(z0, z1)));
    }
}
