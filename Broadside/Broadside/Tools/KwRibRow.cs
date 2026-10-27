using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The rib tool's even row: a number of ribs spread evenly along the level part of the keel line through the keel
    /// clicked (that keel and the keels in line with it, <see cref="KwRudderDrop.InLine"/>), from its first level rib point
    /// to its last, each on the rib point ("Keel top", every metre) nearest its even place, square to the keel. Only the
    /// level part: ribs on a rising stem stand higher, and planking joins only ribs at one height (a stem is planked from
    /// the last rib to its tip). A point that already has a rib is left out, so a row can be added to.
    /// </summary>
    public static class KwRibRow
    {
        private const float Level = 0.05f, Taken = 0.3f;
        private static readonly List<Piece> Near = new List<Piece>();

        /// <summary>Where a row of ribs goes on the keel line through a keel: points with the turn given.</summary>
        public static List<(Vector3 Point, Quaternion Turn)> Spots(Piece keel, Quaternion turn, int count)
        {
            List<Vector3> tops = Tops(keel);
            List<(Vector3, Quaternion)> spots = new List<(Vector3, Quaternion)>();
            if (tops.Count == 0 || count < 1)
                return spots;
            Vector3 axis = Flat(keel.transform.forward);
            tops.Sort((a, b) => Vector3.Dot(a, axis).CompareTo(Vector3.Dot(b, axis)));
            float first = Vector3.Dot(tops[0], axis), last = Vector3.Dot(tops[tops.Count - 1], axis);
            HashSet<int> used = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                float want = count == 1 ? (first + last) / 2f : Mathf.Lerp(first, last, i / (float)(count - 1));
                int best = Closest(tops, axis, want, used);
                if (best < 0)
                    break;
                used.Add(best);
                if (!HasRib(tops[best]))
                    spots.Add((tops[best], turn));
            }
            return spots;
        }

        // The rib points of the keel and of every keel in line with it, the level ones only (within 5 cm of the lowest).
        private static List<Vector3> Tops(Piece keel)
        {
            List<Vector3> tops = new List<Vector3>();
            Points(keel, tops);
            Near.Clear();
            Piece.GetAllPiecesInRadius(keel.transform.position, KwRudderDrop.Reach, Near);
            foreach (Piece other in Near)
            {
                if (other != keel && KwKeel.IsKeel(other) && KwRudderDrop.InLine(keel.transform.position, keel.transform.forward, other.transform.position, other.transform.forward))
                    Points(other, tops);
            }
            float lowest = float.MaxValue;
            tops.ForEach(p => lowest = Mathf.Min(lowest, p.y));
            return tops.FindAll(p => p.y <= lowest + Level);
        }

        private static void Points(Piece keel, List<Vector3> tops)
        {
            foreach (Transform child in keel.transform)
            {
                if (child.name == "Keel top" && child.CompareTag("snappoint") && !tops.Exists(p => (p - child.position).sqrMagnitude < 0.01f))
                    tops.Add(child.position);
            }
        }

        // The unused point nearest a place along the axis, or -1.
        private static int Closest(List<Vector3> tops, Vector3 axis, float want, HashSet<int> used)
        {
            int best = -1;
            float gap = float.MaxValue;
            for (int i = 0; i < tops.Count; i++)
            {
                float d = Mathf.Abs(Vector3.Dot(tops[i], axis) - want);
                if (!used.Contains(i) && d < gap)
                    (best, gap) = (i, d);
            }
            return best;
        }

        private static bool HasRib(Vector3 point)
        {
            Near.Clear();
            Piece.GetAllPiecesInRadius(point, Taken, Near);
            return Near.Exists(p => p != null && KwTools.IsRib(p) && (p.transform.position - point).sqrMagnitude < Taken * Taken);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
