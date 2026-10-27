using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin's roof as a roof railing would stand on it (<see cref="KwRoofRailTool"/>): the cabin, the railing's origin
    /// (over the cabin's own origin, at the roof's top) and heading (the cabin's, level), the roof's outline in that frame
    /// (<see cref="KwCabinRoofs"/>), and the places on it where a ladder already comes up (<see cref="KwLadderGaps"/>).
    /// </summary>
    public sealed class KwRoofSite
    {
        private static readonly List<Piece> Found = new List<Piece>();

        private KwRoofSite(Piece cabin, Vector3 origin, Quaternion turn, List<KwRoofSegment> edges)
        {
            Cabin = cabin;
            Origin = origin;
            Turn = turn;
            Edges = edges;
        }

        public Piece Cabin { get; }
        public Vector3 Origin { get; }
        public Quaternion Turn { get; }
        public List<KwRoofSegment> Edges { get; }

        /// <summary>The roof of a cabin; null for any other piece, or a roof with no edge a rail can follow.</summary>
        public static KwRoofSite? Of(Piece piece)
        {
            if (!KwCabinRoofs.Roof(piece, out float top, out List<KwRoofEdge> roof) || roof == null)
                return null;
            Vector3 at = piece.transform.position;
            Vector3 origin = new Vector3(at.x, top, at.z);
            Quaternion turn = Quaternion.Euler(0f, piece.transform.eulerAngles.y, 0f);
            List<KwRoofSegment> edges = new List<KwRoofSegment>();
            foreach (KwRoofEdge edge in roof)
            {
                char side = KwRoofRail.Letter(edge.Side);
                if (side != '?')
                    edges.Add(new KwRoofSegment(side, Flat(KwRoofFrame.ToKit(edge.A, origin, turn)), Flat(KwRoofFrame.ToKit(edge.B, origin, turn))));
            }
            return edges.Count > 0 ? new KwRoofSite(piece, origin, turn, edges) : null;
        }

        /// <summary>A roof railing on this roof: every edge, those sides railed, posts that far apart, a gap at every ladder up to it.</summary>
        public KwRoofRail Rail(float spacing, string on) => KwLadderGaps.Ladders(new KwRoofRail(spacing, on, Edges, new List<Vector2>()), Origin, Turn);

        /// <summary>A roof railing standing on this roof already (at its origin), or null.</summary>
        public Piece? Railed()
        {
            Found.Clear();
            Piece.GetAllPiecesInRadius(Origin, 0.3f, Found);
            Piece? railed = Found.Find(piece => piece != null && KwLadderGaps.RoofRail(piece, out _, out _));
            Found.Clear();
            return railed;
        }

        private static Vector2 Flat(Vector3 kit) => new Vector2(kit.x, kit.y);
    }

    /// <summary>A level frame of the kit's axes (x forward, y to port, z up) at an origin and a heading: points into it and back. Pure.</summary>
    public static class KwRoofFrame
    {
        public static Vector3 ToKit(Vector3 world, Vector3 origin, Quaternion turn) => KwMesh.ToKit(Back(turn) * (world - origin));

        public static Vector3 ToWorld(Vector3 kit, Vector3 origin, Quaternion turn) => origin + turn * KwMesh.ToUnity(kit);

        // A unit rotation's inverse, worked out here (Unity's own is native code).
        private static Quaternion Back(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);
    }
}
