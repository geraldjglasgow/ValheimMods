using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Everything that bounds the free spans of an edge a ladder may stand against, in the world, for the ladder tool's
    /// centring (<see cref="KwLadderSpans"/>): what is under it (<see cref="KwLadderBelow"/>: things in the way, where the
    /// floor ends), and from the parts loaded round it (<see cref="KwLadderFinds"/>) the doorways and windows in the walls
    /// right under it (a cabin's own walls and raised sides, the planking's), cabins' corner posts and the ladders already
    /// on it. Worked out once for each edge the aim finds (<see cref="KwLadderEdges"/> measures its edges again every
    /// second) and again after a ladder is built (<see cref="Forget"/>). Local to the player aiming.
    /// </summary>
    public static class KwLadderNear
    {
        // How far past an edge's ends parts are looked for.
        private const float Round = 4f;

        private static readonly string Ladder = KwToolPieces.PartPrefix + KwLadderTool.Tool, Planking = KwToolPieces.PartPrefix + "planking";
        private static readonly List<Piece> Found = new List<Piece>();
        private static KwLadderEdge? measured;
        private static List<KwLadderBound> bounds = new List<KwLadderBound>();

        /// <summary>The edge's bounds, worked out once for it.</summary>
        public static List<KwLadderBound> Of(KwLadderEdge edge)
        {
            if (edge != measured)
                (measured, bounds) = (edge, Measure(edge));
            return bounds;
        }

        /// <summary>The bounds worked out again on the next look (a ladder was built).</summary>
        public static void Forget() => measured = null;

        // What is under the edge, then what the parts round it put along it.
        private static List<KwLadderBound> Measure(KwLadderEdge edge)
        {
            List<(float S, float Y)> floors = new List<(float S, float Y)>();
            List<KwLadderBound> found = KwLadderBelow.Of(edge, floors);
            float none = edge.Middle.y - KwLadder.MaxLength;
            Func<float, float> floor = s => KwLadderBelow.Floor(floors, s, none);
            Found.Clear();
            Piece.GetAllPiecesInRadius(edge.Middle, edge.Length / 2f + Round, Found);
            List<Piece> near = Found.FindAll(piece => piece != null);
            Found.Clear();
            foreach (Piece piece in near)
                Add(edge, piece, floor, found);
            return found;
        }

        // One part's bounds along the edge: a ladder on it, a cabin's posts and openings, the planking's openings.
        private static void Add(KwLadderEdge edge, Piece piece, Func<float, float> floor, List<KwLadderBound> found)
        {
            string prefab = Utils.GetPrefabName(piece.gameObject);
            Transform at = piece.transform;
            Func<Vector3, Vector3> world = kit => at.TransformPoint(KwMesh.ToUnity(kit));
            if (prefab == Ladder)
                KwLadderFinds.Ladder(edge, at.position, at.forward, found);
            else if (KwHouses.IsPart(prefab) && KwCabinRoofs.Spec(piece, out KwCabinSpec spec))
            {
                KwLadderFinds.Posts(edge, KwCabinPosts.Of(spec), world, floor, found);
                if (spec.Openings.Count > 0)
                    KwLadderFinds.Openings(edge, KwOpeningSurfaces.Cabin(spec), world, floor, found);
            }
            else if (prefab == Planking && Saved(piece) is string saved && saved.Contains("|open="))
                KwLadderFinds.Openings(edge, KwOpeningSurfaces.Of("planking", saved), world, floor, found);
        }

        // A part's saved text; null for none.
        private static string? Saved(Piece piece)
        {
            ZDO? zdo = piece.m_nview != null && piece.m_nview.IsValid() ? piece.m_nview.GetZDO() : null;
            return zdo?.GetString(KwGenerated.Key);
        }
    }
}
