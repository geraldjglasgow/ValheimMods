using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hull railing a railing tool's click on the hull under it stands for (the user's request, 2026-10-25: "If there's
    /// already railing on an area and you click the area instead of the existing railing, it should still put you in the
    /// edit menu to edit the current railing", <see cref="KwRailEdit.Over"/>). A click on planking, a rib or a keel end
    /// piece is under a railing when its place along the ship lies within the railing's stretch and it is on a side the
    /// railing stands on (a one-sided railing covers only its own side, so a click on the bare side still starts a new one).
    /// Where two railings meet at a rib the one the click is further into is taken. A click on a rib or a keel end at a
    /// railing's end with no railing past it is the tool's own (a new railing starts there, as before). Read from the parts
    /// loaded on this peer; nothing is sent.
    /// </summary>
    public static class KwRailOver
    {
        // How far round a click railings are looked for; how far into a railing a rib's click must be to edit it; how far
        // past a railing's ends and out from its edge a click still counts.
        private const float Reach = 30f, Inside = 0.25f, Past = 0.3f, Wide = 0.75f;

        private static readonly List<Piece> Found = new List<Piece>();
        private static readonly string RailPrefab = KwToolPieces.PartPrefix + KwBiomeCost.Rail;

        /// <summary>The hull railing a click on the hull stands for; null for a click on anything else, or on no railing's stretch.</summary>
        public static Piece? At(KwClick click)
        {
            bool starts = KwTools.IsRib(click.Piece) || KwKeelEnds.Of(click.Piece) != null;
            if (!starts && Utils.GetPrefabName(click.Piece.gameObject) != KwToolPieces.PartPrefix + "planking")
                return null;
            List<(Piece Rail, float Depth)> over = Over(click.Point);
            if (over.Count == 0)
                return null;
            over.Sort((a, b) => b.Depth.CompareTo(a.Depth));
            bool inside = over[0].Depth > Inside;
            return inside || !starts || over.Count > 1 ? over[0].Rail : null;
        }

        // Every hull railing loaded round a point whose stretch it lies on, on a side it has, with how far into it the point
        // lies along the ship (negative a little past an end).
        private static List<(Piece Rail, float Depth)> Over(Vector3 point)
        {
            List<(Piece, float)> over = new List<(Piece, float)>();
            Found.Clear();
            Piece.GetAllPiecesInRadius(point, Reach, Found);
            foreach (Piece piece in Found)
            {
                float? depth = piece != null && Utils.GetPrefabName(piece.gameObject) == RailPrefab ? Depth(piece, point) : null;
                if (depth != null)
                    over.Add((piece!, depth.Value));
            }
            Found.Clear();
            return over;
        }

        /// <summary>How far into a hull railing's stretch a point lies (its nearer end's distance, negative past it); null off it or on a side it does not stand on.</summary>
        public static float? Depth(Piece rail, Vector3 point)
        {
            ZDO? zdo = rail.m_nview != null && rail.m_nview.IsValid() ? rail.m_nview.GetZDO() : null;
            if (zdo == null || !KwSpan.TryParse(zdo.GetString(KwGenerated.Key), out KwSpan span))
                return null;
            List<Station> stations = KwSpanEnds.Live(span, rail.transform).Def(KwBiomeCost.Rail).Stations;
            Vector3 kit = KwMesh.ToKit(Quaternion.Inverse(rail.transform.rotation) * (point - rail.transform.position));
            return Depth(stations, KwRailText.Sides(span.Rail), kit);
        }

        /// <summary>The same in the railing's own kit axes (x along it, y to its left): null off its stretch, beside its edge or on a side it lacks.</summary>
        public static float? Depth(List<Station> stations, string sides, Vector3 kit)
        {
            if (stations.Count < 2)
                return null;
            float first = stations[0].X, last = stations[stations.Count - 1].X;
            float depth = Mathf.Min(kit.x - first, last - kit.x);
            if (depth < -Past || !KwRailText.Has(sides, kit.y >= 0f ? 1f : -1f))
                return null;
            float edge = KwRailing.EdgeAt(stations, Mathf.Clamp(kit.x, first, last)).x;
            return Mathf.Abs(kit.y) <= edge + Wide ? depth : (float?)null;
        }
    }
}
