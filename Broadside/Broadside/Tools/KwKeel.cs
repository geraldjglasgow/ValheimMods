using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The keels, laid first: each is one the keel tool made (<see cref="KwShapeTool"/>, <see cref="KwKeelSpec"/>), of any
    /// length, thickness and biome tier (<see cref="KwBiomeLooks"/>), straight, or climbing to
    /// its far end by the quick curve, the oval's late sweep or a flat stern's, with its post there. Every hull size hangs
    /// from the same keel line (z = 0), and a stem's keel rises by its own rise and curve, so one keel fits under every
    /// hull. Keels join at "Keel joint" points; their "Keel foot" points sit on the dock's keel supports and the loose
    /// cradles; ribs stand on its "Keel top" points, one every metre; a post's "Rudder" point, at the foot of its after
    /// face, takes the rudder, and its "Post top" a cap.
    /// </summary>
    public static class KwKeel
    {
        /// <summary>The standard keel, every keel's default: 0.35 m deep under the keel line, 0.15 m each side of it.</summary>
        public const float Depth = 0.35f, HalfWidth = 0.15f;

        // The standard keel's post: PostDepth along the keel, PostHalf each side, PostAbove over the keel's tip (a made
        // keel's post follows its own thickness, KwKeelBody, and height). The hull's planking finishes narrowing at the
        // post's inner face and its top edge stays under the post's top, so the post always covers the hull's end (KwShapes).
        public const float PostDepth = 0.3f, PostHalf = 0.2f, PostAbove = 3f;

        /// <summary>Where along a stem (0 joint, 1 tip) the post's inner face is: the hull is a sliver from there on.</summary>
        public static float PostFace(float length, float postDepth = PostDepth) => Mathf.Clamp01(1f - postDepth / length);

        /// <summary>The highest the hull's top edge may reach at a stem's tip: just under the post's top.</summary>
        public static float PostTop(float rise, float postAbove) => rise + postAbove - 0.15f;

        /// <summary>The standard keel's thickness, every keel's default: 0.3 m wide, 0.35 m deep.</summary>
        public static readonly KwKeelBody Standard = new KwKeelBody(HalfWidth, Depth);

        /// <summary>The generated part a keel is.</summary>
        public const string MadeName = "KW_gen_keel";

        /// <summary>The share of a keel's rise climbed at t along it: quick (t^2.5), the oval's late sweep (t^5.5) or a flat stern's (t^2).</summary>
        public static float Curve(KwKeelCurve curve, float t) => Mathf.Pow(t, curve == KwKeelCurve.Oval ? 5.5f : curve == KwKeelCurve.Flat ? 2f : 2.5f);

        /// <summary>A keel piece's half width and depth: its own, the standard keel's when it saves none.</summary>
        public static KwKeelBody BodyOf(Piece piece)
        {
            ZDO? zdo = IsKeel(piece) ? piece.GetComponent<ZNetView>()?.GetZDO() : null;
            return zdo != null && KwKeelSpec.TryParse(zdo.GetString(KwGenerated.Key), out KwKeelSpec spec) ? spec.Body : Standard;
        }

        /// <summary>Whether a piece is a keel.</summary>
        public static bool IsKeel(Piece piece) => Utils.GetPrefabName(piece.gameObject) == MadeName;

        /// <summary>
        /// A keel (<see cref="KwKeelSpec"/>): it starts at x = 0 and climbs away from it; joints at its start, and at its far
        /// end too when it is straight with no post; feet wherever it is still flat; a post at its far end if it has one,
        /// with the rudder's point at the foot of the post's outer face. A stem's post is the keel's last stretch; a flat
        /// stern's stands just past its flat end, where the hull's transom closes against it.
        /// </summary>
        public static KwDef Custom(KwKeelSpec spec)
        {
            float length = spec.Length, half = spec.Body.PostDepth / 2f;
            bool open = spec.Rise < 0.01f && !spec.Post;
            float? post = spec.Post ? length + (spec.Flat ? half : -half) : (float?)null;
            KwKeelLine line = new KwKeelLine(length, spec.RiseAt, post, spec.PostAbove, spec.Body);
            KwDef def = Build(line, open ? new[] { 0f, length } : new[] { 0f }, Feet(spec));
            if (post.HasValue)
                def.Snaps.Add(("Rudder", new Vector3(post.Value + half, 0f, spec.RiseAt(length) - spec.Body.Depth)));
            return def;
        }

        // Every metre where the keel is still flat.
        private static float[] Feet(KwKeelSpec spec)
        {
            List<float> feet = new List<float>();
            for (float x = 0f; x <= spec.Length + 0.01f; x += 1f)
            {
                if (spec.RiseAt(x) < 0.02f)
                    feet.Add(Mathf.Min(x, spec.Length));
            }
            return feet.ToArray();
        }

        private static KwDef Build(KwKeelLine line, float[] joints, float[] feet)
        {
            KwDef def = new KwDef(MadeName, "Keel", "keel") { Wood = true, Pivot = new Vector2(0f, -line.Body.Depth) };
            def.Stations = new List<Station> { new Station(0f, new[] { Vector2.zero }), new Station(line.Length, new[] { Vector2.zero }) };
            def.Shape = b => Shape(b, line);
            foreach (float x in joints)
                def.Snaps.Add(("Keel joint", new Vector3(x, 0f, line.Rise(x))));
            foreach (float x in feet)
                def.Snaps.Add(("Keel foot", new Vector3(x, 0f, line.Rise(x) - line.Body.Depth)));
            for (float x = 0f; x <= line.Length + 0.01f; x += 1f)
                def.Snaps.Add(("Keel top", new Vector3(x, 0f, line.Rise(x))));
            if (line.Post.HasValue)
                def.Snaps.Add(("Post top", new Vector3(line.Post.Value, 0f, line.RiseAt(line.Post.Value) + line.PostAbove)));
            return def;
        }

        /// <summary>How many straight segments a keel of this length is drawn in (each 0.4 m or less, at least four).</summary>
        public static int Segments(float length) => Mathf.Max(4, Mathf.CeilToInt(length / 0.4f));

        // Segments half a metre or less long; the post at the far end.
        private static void Shape(MeshBuilder b, KwKeelLine line)
        {
            int steps = Segments(line.Length);
            for (int i = 0; i < steps; i++)
            {
                float x0 = line.Length * i / steps, x1 = line.Length * (i + 1) / steps;
                Solid.Hexa(b, Ring(x0, x1, line.Rise(x0), line.Rise(x1), line.Body), KwKeelUv.Keel(line.Body.Depth));
            }
            if (line.Post.HasValue)
                Post(b, line);
        }

        // The post: its foot on the keel's underside at both its faces, its top flat PostAbove over the keel at its middle,
        // a little wider than the keel each side.
        private static void Post(MeshBuilder b, KwKeelLine line)
        {
            float x = line.Post!.Value, half = line.Body.PostHalf, depth = line.Body.Depth;
            float xa = x - line.Body.PostDepth / 2f, xb = x + line.Body.PostDepth / 2f, top = line.RiseAt(x) + line.PostAbove;
            float za = line.RiseAt(xa) - depth, zb = line.RiseAt(xb) - depth;
            Solid.Hexa(b, new[]
            {
                new Vector3(xa, -half, za), new Vector3(xb, -half, zb), new Vector3(xb, half, zb), new Vector3(xa, half, za),
                new Vector3(xa, -half, top), new Vector3(xb, -half, top), new Vector3(xb, half, top), new Vector3(xa, half, top),
            }, KwKeelUv.Post(x > line.Length / 2f, line.Body.PostDepth));
        }

        private static Vector3[] Ring(float x0, float x1, float z0, float z1, KwKeelBody body)
        {
            float h = body.Half, d = body.Depth;
            return new[]
            {
                new Vector3(x0, -h, z0 - d), new Vector3(x1, -h, z1 - d), new Vector3(x1, h, z1 - d), new Vector3(x0, h, z0 - d),
                new Vector3(x0, -h, z0), new Vector3(x1, -h, z1), new Vector3(x1, h, z1), new Vector3(x0, h, z0),
            };
        }
    }
}
