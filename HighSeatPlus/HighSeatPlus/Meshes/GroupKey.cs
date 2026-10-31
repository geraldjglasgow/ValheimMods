using System;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// What one combined group draws: the pieces of one cell of the world (<see cref="CellGroups.CellSize"/> metres each
    /// way) whose renderers are all switched by a LOD group of their own (<see cref="Lod"/> 1: the group gets one LOD
    /// group for all of them, <see cref="GroupLod"/>) or all left unmanaged (0: always drawn, as the game draws them).
    /// Renderer settings (layer, shadows, probes) differ even between the LOD levels of one piece, so they are kept per
    /// combined renderer instead (<see cref="Bucket"/>).
    /// </summary>
    internal readonly struct GroupKey : IEquatable<GroupKey>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public readonly int Lod;

        public GroupKey(int x, int y, int z, int lod)
        {
            X = x;
            Y = y;
            Z = z;
            Lod = lod;
        }

        public bool Equals(GroupKey other)
        {
            return X == other.X && Y == other.Y && Z == other.Z && Lod == other.Lod;
        }

        public override bool Equals(object obj)
        {
            return obj is GroupKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791);
                return hash * 397 + Lod;
            }
        }
    }
}
