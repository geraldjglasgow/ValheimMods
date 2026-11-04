using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Wall faces beside the walkable cells, every metre (the cells on even rows and columns): a ray 1.2 m over the surface
    /// out along each of the frame's four ways, 0.9 m long, against building pieces only; a face counts when it stands
    /// upright, its piece is a building piece (it wears and tears) that works at nothing, and its collider is at least 1.4 m
    /// tall. The cell is marked <see cref="KwCellMark.Wall"/>.
    /// </summary>
    public static class KwDecorWalls
    {
        private const float Height = 1.2f, Reach = 0.9f, Upright = 0.3f, Tall = 1.4f;

        /// <summary>The cells from one index up to another (not included).</summary>
        public static void Read(KwDecorGrid grid, int from, int to, List<KwWallSpot> spots)
        {
            for (int k = from; k < to; k++)
            {
                if ((k % grid.N) % 2 == 0 && (k / grid.N) % 2 == 0 && grid.Cells[k].Walkable)
                    Cast(grid, k, spots);
            }
        }

        private static void Cast(KwDecorGrid grid, int k, List<KwWallSpot> spots)
        {
            Vector3 from = grid.On(k) + Vector3.up * Height;
            for (int dir = 0; dir < 4; dir++)
            {
                if (!Physics.Raycast(from, grid.Dir(dir), out RaycastHit hit, Reach, KwDecorSurface.PieceMask, QueryTriggerInteraction.Ignore))
                    continue;
                Piece piece = hit.collider.GetComponentInParent<Piece>();
                if (Mathf.Abs(hit.normal.y) > Upright || piece == null || !WallLike(piece, hit.collider))
                    continue;
                Vector3 normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
                spots.Add(new KwWallSpot(k, hit.point, normal));
                grid.Cells[k].Marks |= KwCellMark.Wall;
            }
        }

        private static bool WallLike(Piece piece, Collider collider) =>
            piece.GetComponent<WearNTear>() != null && collider.bounds.size.y >= Tall && !KwDecorSurface.Obstacle(piece)
            && !KwDecorSurface.Decoration(piece) && !KwDecorKeepClear.DryDock(piece.name);
    }
}
