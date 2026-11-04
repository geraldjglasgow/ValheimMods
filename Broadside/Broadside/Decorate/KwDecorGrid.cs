using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The read area: a square of 0.5 m cells round a centre, turned to a frame (<see cref="Yaw"/>: the floor's own axis, so
    /// a dock's edges run along the rows). Directions are numbered 0 +x (<see cref="Right"/>), 1 +z (<see cref="Forward"/>),
    /// 2 -x, 3 -z. Plain data and arithmetic.
    /// </summary>
    public sealed class KwDecorGrid
    {
        public const float Step = 0.5f;
        private static readonly int[] DI = { 1, 0, -1, 0 }, DJ = { 0, 1, 0, -1 };

        public KwDecorGrid(Vector3 centre, float yaw, float half)
        {
            (Centre, Yaw, Half) = (centre, yaw, half);
            N = Mathf.Max(2, Mathf.RoundToInt(2f * half / Step));
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            (Right, Forward) = (turn * Vector3.right, turn * Vector3.forward);
            Cells = new KwCell[N * N];
            for (int k = 0; k < Cells.Length; k++)
                Cells[k] = new KwCell { Region = -1, Out = -1, Water = 16f, Up = 1f };
        }

        public Vector3 Centre { get; }
        public float Yaw { get; }
        public float Half { get; }

        /// <summary>Cells along a side.</summary>
        public int N { get; }

        public Vector3 Right { get; }
        public Vector3 Forward { get; }
        public KwCell[] Cells { get; }

        public int Index(int i, int j) => j * N + i;
        public bool Inside(int i, int j) => i >= 0 && j >= 0 && i < N && j < N;

        /// <summary>A cell's middle at the centre's height.</summary>
        public Vector3 At(int i, int j) => Centre + Right * ((i + 0.5f) * Step - Half) + Forward * ((j + 0.5f) * Step - Half);

        public Vector3 At(int index) => At(index % N, index / N);

        /// <summary>A cell's middle on its surface.</summary>
        public Vector3 On(int index)
        {
            Vector3 at = At(index);
            at.y = Cells[index].Y;
            return at;
        }

        /// <summary>A world point in cells across the ground from the area's corner (x along <see cref="Right"/>, y along <see cref="Forward"/>).</summary>
        public Vector2 Coords(Vector3 point)
        {
            Vector3 d = point - Centre;
            return new Vector2((Vector3.Dot(d, Right) + Half) / Step, (Vector3.Dot(d, Forward) + Half) / Step);
        }

        /// <summary>The cell a world point is in; false outside the area.</summary>
        public bool CellOf(Vector3 point, out int index)
        {
            Vector3 d = point - Centre;
            int i = Mathf.FloorToInt((Vector3.Dot(d, Right) + Half) / Step), j = Mathf.FloorToInt((Vector3.Dot(d, Forward) + Half) / Step);
            index = Inside(i, j) ? Index(i, j) : -1;
            return index >= 0;
        }

        /// <summary>The neighbour of a cell in a direction; -1 past the area's side.</summary>
        public int Next(int index, int dir)
        {
            int i = index % N + DI[dir], j = index / N + DJ[dir];
            return Inside(i, j) ? Index(i, j) : -1;
        }

        /// <summary>A direction as a world vector.</summary>
        public Vector3 Dir(int dir) => dir switch { 0 => Right, 1 => Forward, 2 => -Right, _ => -Forward };

        /// <summary>The direction nearest a world vector across the ground.</summary>
        public int DirOf(Vector3 way)
        {
            float x = Vector3.Dot(way, Right), z = Vector3.Dot(way, Forward);
            if (Mathf.Abs(x) >= Mathf.Abs(z))
                return x >= 0f ? 0 : 2;
            return z >= 0f ? 1 : 3;
        }

        /// <summary>Whether a world point lies inside the square, across the ground.</summary>
        public bool Contains(Vector3 point)
        {
            Vector3 d = point - Centre;
            return Mathf.Abs(Vector3.Dot(d, Right)) <= Half && Mathf.Abs(Vector3.Dot(d, Forward)) <= Half;
        }

        /// <summary>A direction's yaw in degrees (the way a piece's +z would point).</summary>
        public float YawOf(int dir) => Yaw + dir switch { 0 => 90f, 1 => 0f, 2 => 270f, _ => 180f };

        /// <summary>The opposite direction.</summary>
        public static int Back(int dir) => (dir + 2) & 3;
    }
}
