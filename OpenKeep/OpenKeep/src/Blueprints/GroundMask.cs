using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Where a blueprint's building stands on the ground, in its own frame on a half-metre grid: the drawn footprint of
    /// every piece whose bottom reaches down to the levelled ground (floors, walls, posts, stairs, stations). The ground
    /// there is painted dirt, so no grass grows through the floors. Worked out once per blueprint.
    /// </summary>
    public sealed class GroundMask
    {
        private const float Cell = 0.5f;
        private const float Margin = 6f;

        private static readonly Dictionary<Blueprint, GroundMask> masks = new Dictionary<Blueprint, GroundMask>();

        private float x0;
        private float z0;
        private int width;
        private int depth;
        private bool[] cells;

        /// <summary>The blueprint's mask; pieces the game does not have count as a one-metre square around their pivot.</summary>
        public static GroundMask For(Blueprint bp)
        {
            if (masks.TryGetValue(bp, out GroundMask known))
                return known;
            if (masks.Count > 16)
                masks.Clear();
            GroundMask mask = Build(bp);
            masks[bp] = mask;
            return mask;
        }

        /// <summary>A building piece stands on the ground at this frame point.</summary>
        public bool Covered(float x, float z)
        {
            int ix = Mathf.FloorToInt((x - x0) / Cell), iz = Mathf.FloorToInt((z - z0) / Cell);
            return ix >= 0 && iz >= 0 && ix < width && iz < depth && cells[iz * width + ix];
        }

        private static GroundMask Build(Blueprint bp)
        {
            Rect r = bp.PieceBounds;
            GroundMask mask = new GroundMask
            {
                x0 = r.xMin - Margin, z0 = r.yMin - Margin,
                width = Mathf.CeilToInt((r.width + 2f * Margin) / Cell) + 1, depth = Mathf.CeilToInt((r.height + 2f * Margin) / Cell) + 1,
            };
            mask.cells = new bool[mask.width * mask.depth];
            foreach (BlueprintPiece piece in bp.Pieces)
                mask.Add(piece, PieceShapes.Of(piece.Prefab));
            return mask;
        }

        private void Add(BlueprintPiece piece, PieceShape shape)
        {
            Bounds b = shape != null && shape.Template != null ? shape.Bounds : new Bounds(Vector3.zero, new Vector3(1f, 1f, 1f));
            if (piece.Y + b.min.y > BlueprintRules.GroundContact)
                return;
            Quaternion turn = Quaternion.Euler(0f, piece.Yaw, 0f);
            Vector3 centre = new Vector3(piece.X, 0f, piece.Z) + turn * new Vector3(b.center.x, 0f, b.center.z);
            Vector3 across = turn * Vector3.right, along = turn * Vector3.forward;
            float reach = Mathf.Sqrt(b.extents.x * b.extents.x + b.extents.z * b.extents.z);
            Fill(centre, across, along, b.extents, reach);
        }

        /// <summary>Marks every cell whose centre lies inside the turned rectangle.</summary>
        private void Fill(Vector3 centre, Vector3 across, Vector3 along, Vector3 extents, float reach)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt((centre.x - reach - x0) / Cell)), maxX = Mathf.Min(width - 1, Mathf.CeilToInt((centre.x + reach - x0) / Cell));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((centre.z - reach - z0) / Cell)), maxZ = Mathf.Min(depth - 1, Mathf.CeilToInt((centre.z + reach - z0) / Cell));
            for (int iz = minZ; iz <= maxZ; iz++)
            {
                for (int ix = minX; ix <= maxX; ix++)
                {
                    Vector3 d = new Vector3(x0 + (ix + 0.5f) * Cell - centre.x, 0f, z0 + (iz + 0.5f) * Cell - centre.z);
                    if (Mathf.Abs(Vector3.Dot(d, across)) <= extents.x && Mathf.Abs(Vector3.Dot(d, along)) <= extents.z)
                        cells[iz * width + ix] = true;
                }
            }
        }
    }
}
