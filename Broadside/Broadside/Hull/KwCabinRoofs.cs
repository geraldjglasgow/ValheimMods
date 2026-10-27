using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A built cabin's roof for the tools that use it (the Cabin tool builds on it, the railing tool rails it, the ladder
    /// tool climbs to it): whether a piece is a cabin and what it saves, whether a point is on its roof's top, and the
    /// roof's top height and the edges of its outline in world space. The sides are named in the cabin's own frame: "fore"
    /// its front end (the ship's bow when the Cabin tool could tell it, else the end its builder faced), "aft" its back,
    /// "port" its left and "starboard" its right.
    /// </summary>
    public static class KwCabinRoofs
    {
        /// <summary>How near a roof's top a point must be to be on it (its underside is a plank's thickness lower).</summary>
        private const float OnTop = 0.04f;

        /// <summary>Whether the piece is a built cabin (the Cabin tool's generated part); if so, its roof's top height (world y) and
        /// the roof outline's edges in world space, each from a to b, with the outward horizontal normal and which side it is
        /// ("fore", "aft", "port", "starboard"; a hull-following side may be several segments with the same side).</summary>
        public static bool Roof(Piece piece, out float top, out List<KwRoofEdge> edges)
        {
            top = 0f;
            edges = new List<KwRoofEdge>();
            if (!Spec(piece, out KwCabinSpec spec))
                return false;
            Transform at = piece.transform;
            top = at.TransformPoint(KwMesh.ToUnity(new Vector3(0f, 0f, spec.Top))).y;
            foreach ((Vector3 a, Vector3 b, string side) in KitEdges(spec))
            {
                Vector3 run = b - a, outward = new Vector3(-run.y, run.x, 0f);
                Vector3 world = at.TransformDirection(KwMesh.ToUnity(outward));
                world.y = 0f;
                edges.Add(new KwRoofEdge(at.TransformPoint(KwMesh.ToUnity(a)), at.TransformPoint(KwMesh.ToUnity(b)), world.normalized, side));
            }
            return true;
        }

        /// <summary>
        /// The roof's outline at its top in the cabin's kit axes, anticlockwise seen from above (so each edge's outward side is
        /// its way turned a quarter anticlockwise): across its back, up its left side, across its front, down its right.
        /// </summary>
        public static List<(Vector3 A, Vector3 B, string Side)> KitEdges(KwCabinSpec spec)
        {
            List<(float X, float Left, float Right)> roof = KwCabinOutline.Roof(spec);
            List<(Vector3, Vector3, string)> edges = new List<(Vector3, Vector3, string)>();
            float z = spec.Top;
            var (first, last) = (roof[0], roof[roof.Count - 1]);
            Edge(edges, new Vector3(first.X, -first.Right, z), new Vector3(first.X, first.Left, z), "aft");
            for (int i = 0; i + 1 < roof.Count; i++)
                Edge(edges, new Vector3(roof[i].X, roof[i].Left, z), new Vector3(roof[i + 1].X, roof[i + 1].Left, z), "port");
            Edge(edges, new Vector3(last.X, last.Left, z), new Vector3(last.X, -last.Right, z), "fore");
            for (int i = roof.Count - 1; i > 0; i--)
                Edge(edges, new Vector3(roof[i].X, -roof[i].Right, z), new Vector3(roof[i - 1].X, -roof[i - 1].Right, z), "starboard");
            return edges;
        }

        private static void Edge(List<(Vector3, Vector3, string)> edges, Vector3 a, Vector3 b, string side)
        {
            if ((b - a).sqrMagnitude > 0.0025f)
                edges.Add((a, b, side));
        }

        /// <summary>The piece's cabin: a built cabin part (or an old castle) whose saved text reads (the last text read kept, as the tools ask every frame).</summary>
        public static bool Spec(Piece piece, out KwCabinSpec spec)
        {
            spec = null!;
            ZNetView? view = piece != null ? piece.m_nview : null;
            ZDO? zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            if (zdo == null || !KwHouses.IsPart(Utils.GetPrefabName(piece!.gameObject)))
                return false;
            string text = zdo.GetString(KwGenerated.Key);
            if (!ReferenceEquals(text, lastText))
                (lastText, last) = (text, KwCabinSpec.TryParse(text, out KwCabinSpec read) ? read : null);
            spec = last!;
            return last != null;
        }

        private static string? lastText;
        private static KwCabinSpec? last;

        /// <summary>Whether a world point is on a cabin's roof: at its top's height and inside its outline.</summary>
        public static bool OnRoof(Piece piece, Vector3 point)
        {
            if (!Spec(piece, out KwCabinSpec spec))
                return false;
            Vector3 kit = KwMesh.ToKit(piece.transform.InverseTransformPoint(point));
            if (Mathf.Abs(kit.z - spec.Top) > OnTop || kit.x < spec.X0 - 0.05f || kit.x > spec.X1 + 0.05f)
                return false;
            float left = KwCabinOutline.Reach(spec, spec.Left, kit.x, spec.Top), right = KwCabinOutline.Reach(spec, spec.Right, kit.x, spec.Top);
            return kit.y < left + 0.05f && kit.y > -right - 0.05f;
        }
    }

    /// <summary>One straight edge of a cabin roof's outline in world space: from A to B, its outward horizontal normal and its side.</summary>
    public readonly struct KwRoofEdge
    {
        public KwRoofEdge(Vector3 a, Vector3 b, Vector3 outward, string side)
        {
            A = a;
            B = b;
            Outward = outward;
            Side = side;
        }

        public Vector3 A { get; }
        public Vector3 B { get; }
        public Vector3 Outward { get; }
        public string Side { get; }
    }
}
