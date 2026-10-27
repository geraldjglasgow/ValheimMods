using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What is under an edge a ladder may stand against, looked at every <see cref="Step"/> along it the way the ladder's
    /// own length is found (<see cref="KwLadderDrop.Under"/>): the floor it would stand on and how high, something else
    /// standing in the way first (a mast, the helm, a chest: a solid bound, by its name), or no floor of the ship within its
    /// reach (the floor below ends, or only the hull, a railing or the dock is there). Each run of samples alike is one bound
    /// (<see cref="KwLadderBound"/>). A ladder already there counts as nothing (it is a bound of its own,
    /// <see cref="KwLadderFinds.Ladder"/>). Local to the player aiming. On a launched ship the samples are the ship's
    /// blueprint's (<see cref="KwSeaLadderNear"/>), run together the same way (<see cref="Runs"/>).
    /// </summary>
    public static class KwLadderBelow
    {
        /// <summary>How far apart along the edge it is looked under.</summary>
        public const float Step = 0.1f;

        private static readonly string[] Hull = { "planking", "rib", "keel", "rail", "bowsprit" };

        /// <summary>The bounds under the edge, and into floors each sample's place along it and the floor's height there.</summary>
        public static List<KwLadderBound> Of(KwLadderEdge edge, List<(float S, float Y)> floors) => Runs(edge, s => Sample(edge, s, floors));

        /// <summary>The bounds along an edge from what a sample at each step along it finds (nothing in the way: no kind).</summary>
        public static List<KwLadderBound> Runs(KwLadderEdge edge, System.Func<float, (KwLadderBoundKind? Kind, string Name)> sample)
        {
            List<KwLadderBound> found = new List<KwLadderBound>();
            int count = Mathf.Max(1, Mathf.CeilToInt(edge.Length / Step));
            float step = edge.Length / count, from = 0f;
            (KwLadderBoundKind? Kind, string Name) run = (null, "");
            for (int k = 0; k < count; k++)
            {
                (KwLadderBoundKind? Kind, string Name) one = sample((k + 0.5f) * step);
                if (one == run)
                    continue;
                Close(found, run, from, k * step);
                (run, from) = (one, k * step);
            }
            Close(found, run, from, edge.Length);
            return found;
        }

        /// <summary>The floor's height under the edge at s: the nearest sample's (an edge's top less the ladder's reach for none).</summary>
        public static float Floor(List<(float S, float Y)> floors, float s, float none)
        {
            float best = none, nearest = float.MaxValue;
            foreach ((float at, float y) in floors)
            {
                if (Mathf.Abs(at - s) < nearest)
                    (best, nearest) = (y, Mathf.Abs(at - s));
            }
            return best;
        }

        // A run of samples alike as a bound, when it is one.
        private static void Close(List<KwLadderBound> found, (KwLadderBoundKind? Kind, string Name) run, float from, float to)
        {
            if (run.Kind != null && to > from)
                found.Add(new KwLadderBound(from, to, run.Kind.Value, run.Name));
        }

        // What is under the edge at s: nothing in the way (a floor, a ladder), something solid by name, or no floor.
        private static (KwLadderBoundKind?, string) Sample(KwLadderEdge edge, float s, List<(float S, float Y)> floors)
        {
            Vector3 top = edge.At(s);
            if (!KwLadderDrop.Under(top, edge.OutwardAt(s), edge.Piece!.transform, out RaycastHit hit))
            {
                floors.Add((s, top.y - KwLadder.MaxLength));
                return (KwLadderBoundKind.NoFloor, KwLadderBound.DeckEnd);
            }
            floors.Add((s, hit.point.y));
            if (KwLadderDrop.Floor(hit))
                return (null, "");
            Piece? piece = hit.collider.GetComponentInParent<Piece>();
            if (piece == null || IsHull(piece))
                return (KwLadderBoundKind.NoFloor, KwLadderBound.DeckEnd);
            return KwLadderTool.IsLadder(piece) ? ((KwLadderBoundKind?)null, "") : ((KwLadderBoundKind?)KwLadderBoundKind.Solid, Name(piece));
        }

        // The hull's own parts and the dock, which nothing climbs down to.
        private static bool IsHull(Piece piece)
        {
            string prefab = Utils.GetPrefabName(piece.gameObject);
            if (prefab.StartsWith("KW_dock", System.StringComparison.Ordinal) || prefab.StartsWith("KW_keel", System.StringComparison.Ordinal))
                return true;
            return prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal) && System.Array.IndexOf(Hull, prefab.Substring(KwToolPieces.PartPrefix.Length)) >= 0;
        }

        // A thing in the way in the player's words: a mast, else the piece's own name in lower case, else an obstacle.
        private static string Name(Piece piece)
        {
            if (Utils.GetPrefabName(piece.gameObject) == KwSailTool.Made)
                return "mast";
            string name = Localization.instance != null ? Localization.instance.Localize(piece.m_name) : piece.m_name;
            bool plain = name.Length > 0 && name.Length <= 24 && name.IndexOfAny(new[] { '$', '[', '<' }) < 0;
            return plain ? name.ToLowerInvariant() : KwLadderBound.Obstacle;
        }
    }
}
