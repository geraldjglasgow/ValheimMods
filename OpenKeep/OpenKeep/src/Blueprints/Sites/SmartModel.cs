using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Everything smart select keeps about one blueprint: every piece's box, the pieces bucketed by place, the plan
    /// grid, and the storeys worked out so far (by floor height, half-metre steps). A piece's storey is the lowest floor
    /// within <see cref="FloorSearch"/> m of its centre that is not above it (a house's ground floor for its bedroom
    /// and its roof alike), or the levelled ground there when no floor is near.
    /// </summary>
    internal sealed class SmartModel
    {
        private const float FloorSearch = 0.5f;
        private const int MaxLevels = 8;

        public readonly SmartBox[] Boxes;
        public readonly SmartHash Hash;
        public readonly SmartGrid Grid;

        private readonly Blueprint blueprint;
        private readonly Dictionary<int, SmartLevel> levels = new Dictionary<int, SmartLevel>();

        public SmartModel(Blueprint bp, Func<string, Bounds?> sizes)
        {
            blueprint = bp;
            Boxes = Measure(bp, sizes);
            Hash = new SmartHash(Boxes);
            Grid = new SmartGrid(Boxes);
        }

        /// <summary>The piece count it was made from, to notice a blueprint that changed.</summary>
        public int PieceCount => Boxes.Length;

        /// <summary>The storey the piece belongs to, worked out on first use.</summary>
        public SmartLevel LevelOf(int index)
        {
            int key = Mathf.RoundToInt(Floor(index) * 2f);
            if (levels.TryGetValue(key, out SmartLevel level))
                return level;
            if (levels.Count >= MaxLevels)
                levels.Clear();
            level = new SmartLevel(this, key * 0.5f);
            levels[key] = level;
            return level;
        }

        /// <summary>The top of the lowest floor near the piece's centre and not above its top, else the ground there.</summary>
        private float Floor(int index)
        {
            SmartBox box = Boxes[index];
            float lowest = float.MaxValue;
            foreach (int i in Hash.Near(box.X, box.Z, FloorSearch))
            {
                SmartBox floor = Boxes[i];
                if (floor.Floor && floor.Top <= box.Top + 0.05f && floor.Covers(box.X, box.Z, FloorSearch))
                    lowest = Mathf.Min(lowest, floor.Top);
            }
            return lowest < float.MaxValue ? lowest : GroundAt(box.X, box.Z);
        }

        /// <summary>The levelled ground at a frame point: the last levelled square holding it, else the saved relief, else 0.</summary>
        public float GroundAt(float x, float z)
        {
            for (int i = blueprint.Levels.Count - 1; i >= 0; i--)
            {
                if (blueprint.Levels[i].Contains(x, z))
                    return blueprint.Levels[i].Y;
            }
            return blueprint.Relief?.At(x, z) ?? 0f;
        }

        private static SmartBox[] Measure(Blueprint bp, Func<string, Bounds?> sizes)
        {
            Dictionary<string, Bounds?> known = new Dictionary<string, Bounds?>();
            SmartBox[] boxes = new SmartBox[bp.Pieces.Count];
            for (int i = 0; i < boxes.Length; i++)
            {
                BlueprintPiece piece = bp.Pieces[i];
                if (!known.TryGetValue(piece.Prefab, out Bounds? drawn))
                    known[piece.Prefab] = drawn = sizes(piece.Prefab);
                boxes[i] = SmartBox.Of(piece, drawn);
            }
            return boxes;
        }
    }
}
