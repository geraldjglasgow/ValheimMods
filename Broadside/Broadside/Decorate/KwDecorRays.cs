using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Each cell of the area read from above: one ray down through the cell's middle from 4 m over the aim's height to 8 m
    /// under it (a large area's band grows with it, to 11 m over and 16 m under for a 64 m area, so a slope far off is still
    /// met), through water (the ray never meets it; the water is read from the game's level), every surface it passes
    /// listed top down. The surface taken is the one nearest the aim's height among those with 2 m clear over them, one more
    /// than 1.5 m under the aim counted 3 m farther (a roof over a dock leaves the dock; a chest on it is what stands there;
    /// a high dock beside the water aimed at is the dock, not the sea floor). Then what it is (<see cref="KwDecorSurface"/>):
    /// terrain dry or under water, a floor, or something standing, and a floor right under what stands (a covered cell).
    /// </summary>
    public static class KwDecorRays
    {
        private const float Above = 4f, Below = 8f, AboveShare = 0.35f, BelowShare = 0.5f, Headroom = 2f, CoverDrop = 2.5f, Wet = 0.05f, Sunk = 1.5f, SunkCost = 3f;
        private static readonly RaycastHit[] hits = new RaycastHit[32];

        /// <summary>The cells from one index up to another (not included).</summary>
        public static void Read(KwDecorGrid grid, int from, int to)
        {
            for (int k = from; k < to; k++)
                ReadCell(grid, k);
        }

        private static void ReadCell(KwDecorGrid grid, int k)
        {
            float above = Mathf.Max(Above, grid.Half * AboveShare), below = Mathf.Max(Below, grid.Half * BelowShare);
            Vector3 top = grid.At(k) + Vector3.up * above;
            int n = Physics.RaycastNonAlloc(top, Vector3.down, hits, above + below, KwDecorSurface.RayMask, QueryTriggerInteraction.Ignore);
            if (n == 0)
                return;
            System.Array.Sort(hits, 0, n, Nearer.Instance);
            Classify(ref grid.Cells[k], Level(n, grid.Centre.y), n);
        }

        // The surface nearest the aim's height among those with headroom over them (the first always has); one more than
        // 1.5 m under the aim counts 3 m farther, so a high dock beside the water aimed at wins over the sea floor under it.
        private static int Level(int n, float aim)
        {
            (int best, float gap) = (0, float.MaxValue);
            for (int h = 0; h < n; h++)
            {
                if (h > 0 && hits[h - 1].point.y - hits[h].point.y < Headroom)
                    continue;
                float y = hits[h].point.y, off = Mathf.Abs(y - aim) + (y < aim - Sunk ? SunkCost : 0f);
                if (off < gap)
                    (best, gap) = (h, off);
            }
            return best;
        }

        private static void Classify(ref KwCell cell, int pick, int n)
        {
            RaycastHit hit = hits[pick];
            (cell.Y, cell.Up) = (hit.point.y, hit.normal.y);
            Heightmap ground = hit.collider.GetComponent<Heightmap>();
            if (ground != null)
            {
                Terrain(ref cell, ground, hit.point);
                return;
            }
            Piece piece = hit.collider.GetComponentInParent<Piece>();
            if (piece != null && KwDecorSurface.Floor(piece, hit))
            {
                Deck(ref cell, piece, hit.point);
                return;
            }
            cell.Kind = KwCellKind.Blocked;
            Under(ref cell, pick, n);
        }

        private static void Terrain(ref KwCell cell, Heightmap ground, Vector3 at)
        {
            cell.Depth = KwDecorSurface.Depth(at);
            cell.Kind = cell.Depth > Wet ? KwCellKind.Water : KwCellKind.Ground;
            if (cell.Kind == KwCellKind.Ground && KwDecorSurface.Painted(ground, at))
                cell.Marks |= KwCellMark.Paint;
        }

        // A floor under water is left alone.
        private static void Deck(ref KwCell cell, Piece piece, Vector3 at)
        {
            cell.Depth = KwDecorSurface.Depth(at);
            cell.Kind = cell.Depth > Wet ? KwCellKind.Blocked : KwCellKind.Deck;
            cell.Floor = piece;
            if (KwDecorSurface.Wood(piece))
                cell.Marks |= KwCellMark.Wood;
        }

        // A floor right under what stands there: the cell counts for the floor's width (covered), at the floor's height.
        private static void Under(ref KwCell cell, int pick, int n)
        {
            if (pick + 1 >= n)
                return;
            RaycastHit below = hits[pick + 1];
            Piece piece = below.collider.GetComponentInParent<Piece>();
            if (cell.Y - below.point.y > CoverDrop || piece == null || !KwDecorSurface.Floor(piece, below))
                return;
            (cell.Marks, cell.Y, cell.Floor) = (cell.Marks | KwCellMark.Cover, below.point.y, piece);
        }

        // Hits nearest the ray's start first.
        private sealed class Nearer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly Nearer Instance = new Nearer();

            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
