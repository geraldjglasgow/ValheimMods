using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Finds rooms near a point or a piece on a storey's labelled plan. A coarse map of 4 m blocks that hold any room
    /// cell lets the many pieces far from every building (town walls, roads, fields) skip the cell search.
    /// </summary>
    internal sealed class SmartReach
    {
        private const int Block = 16;

        private readonly SmartGrid grid;
        private readonly int[] labels;
        private readonly bool[] blocks;
        private readonly int blocksWide;

        public SmartReach(SmartGrid grid, int[] labels)
        {
            this.grid = grid;
            this.labels = labels;
            blocksWide = (grid.Width + Block - 1) / Block;
            blocks = new bool[blocksWide * ((grid.Depth + Block - 1) / Block)];
            for (int cell = 0; cell < labels.Length; cell++)
            {
                if (labels[cell] > 0)
                    blocks[cell / grid.Width / Block * blocksWide + cell % grid.Width / Block] = true;
            }
        }

        /// <summary>The room with a cell nearest the frame point within <paramref name="radius"/>, or 0.</summary>
        public int Nearest(float x, float z, float radius)
        {
            int minX = Column(x - radius), maxX = Column(x + radius), minZ = Row(z - radius), maxZ = Row(z + radius);
            if (minX > maxX || minZ > maxZ || !AnyBlock(minX, maxX, minZ, maxZ))
                return 0;
            int best = 0;
            float bestSq = radius * radius;
            for (int iz = minZ; iz <= maxZ; iz++)
            {
                for (int ix = minX; ix <= maxX; ix++)
                {
                    int cell = iz * grid.Width + ix;
                    float dx = grid.CentreX(cell) - x, dz = grid.CentreZ(cell) - z, sq = dx * dx + dz * dz;
                    if (labels[cell] > 0 && sq <= bestSq)
                    {
                        bestSq = sq;
                        best = labels[cell];
                    }
                }
            }
            return best;
        }

        /// <summary>Some room may lie within <paramref name="margin"/> m of the box's footprint (a block test, cheap and generous).</summary>
        public bool AnyNear(SmartBox box, float margin)
        {
            float sx = box.Spread(true) + margin, sz = box.Spread(false) + margin;
            int minX = Column(box.X - sx), maxX = Column(box.X + sx), minZ = Row(box.Z - sz), maxZ = Row(box.Z + sz);
            return minX <= maxX && minZ <= maxZ && AnyBlock(minX, maxX, minZ, maxZ);
        }

        private int Column(float x) => Mathf.Clamp(Mathf.FloorToInt((x - grid.X0) / SmartGrid.Cell), 0, grid.Width - 1);

        private int Row(float z) => Mathf.Clamp(Mathf.FloorToInt((z - grid.Z0) / SmartGrid.Cell), 0, grid.Depth - 1);

        private bool AnyBlock(int minX, int maxX, int minZ, int maxZ)
        {
            for (int bz = minZ / Block; bz <= maxZ / Block; bz++)
            {
                for (int bx = minX / Block; bx <= maxX / Block; bx++)
                {
                    if (blocks[bz * blocksWide + bx])
                        return true;
                }
            }
            return false;
        }
    }
}
