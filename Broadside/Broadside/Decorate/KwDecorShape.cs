using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>A rectangle in a scene's frame: x to its right, z to its front, metres. Plain arithmetic.</summary>
    public readonly struct KwRect
    {
        public KwRect(float minX, float minZ, float maxX, float maxZ)
        {
            (MinX, MinZ, MaxX, MaxZ) = (minX, minZ, maxX, maxZ);
        }

        public float MinX { get; }
        public float MinZ { get; }
        public float MaxX { get; }
        public float MaxZ { get; }

        public KwRect Grow(float by) => new KwRect(MinX - by, MinZ - by, MaxX + by, MaxZ + by);

        public KwRect Join(KwRect other) =>
            new KwRect(Mathf.Min(MinX, other.MinX), Mathf.Min(MinZ, other.MinZ), Mathf.Max(MaxX, other.MaxX), Mathf.Max(MaxZ, other.MaxZ));

        /// <summary>A footprint of a size about a point, turned a multiple of 90 degrees (as a piece's own turn).</summary>
        public static KwRect About(float x, float z, float width, float depth, float yaw)
        {
            bool across = Mathf.RoundToInt(yaw / 90f) % 2 != 0;
            (float hx, float hz) = across ? (depth / 2f, width / 2f) : (width / 2f, depth / 2f);
            return new KwRect(x - hx, z - hz, x + hx, z + hz);
        }
    }

    /// <summary>A scene's piece as this game has it: the member, its decoration, its footprint in the scene's frame.</summary>
    public sealed class KwScenePiece
    {
        public KwScenePiece(KwSceneMember member, KwDecorKind kind)
        {
            (Member, Kind) = (member, kind);
            Rect = KwRect.About(member.X, member.Z, kind.Fit.Width, kind.Fit.Depth, member.Yaw);
        }

        public KwSceneMember Member { get; }
        public KwDecorKind Kind { get; }
        public KwRect Rect { get; }

        /// <summary>Stands on the ground where the scene puts it (not hung on a wall).</summary>
        public bool Stands => Member.Place != KwDecorPlace.Wall;
    }

    /// <summary>
    /// A scene as this game can build it (<see cref="KwDecorScene"/>): its pieces whose decorations are in the game (a
    /// piece not yet made is left out, and a scene missing one of its core pieces is never placed), the rectangle round
    /// everything that stands, and its core's front and back lines. Worked out once a layout.
    /// </summary>
    public sealed class KwSceneShape
    {
        public KwSceneShape(KwDecorScene scene)
        {
            Scene = scene;
            Valid = true;
            foreach (KwSceneMember member in scene.Members)
            {
                KwDecorKind? kind = KwDecorKind.Find(member.Piece);
                if (kind != null)
                    Pieces.Add(new KwScenePiece(member, kind));
                else if (member.Extra == 0)
                    Valid = false;
            }
            Measure();
        }

        public KwDecorScene Scene { get; }
        public List<KwScenePiece> Pieces { get; } = new List<KwScenePiece>();
        public bool Valid { get; private set; }

        /// <summary>Round every standing piece.</summary>
        public KwRect Bounds { get; private set; }

        /// <summary>The core's front and back lines (z).</summary>
        public float Front { get; private set; }
        public float Back { get; private set; }

        private void Measure()
        {
            (KwRect? all, float front, float back) = (null, float.MinValue, float.MaxValue);
            foreach (KwScenePiece piece in Pieces)
            {
                if (!piece.Stands)
                    continue;
                all = all is KwRect had ? had.Join(piece.Rect) : piece.Rect;
                if (piece.Member.Extra == 0)
                    (front, back) = (Mathf.Max(front, piece.Rect.MaxZ), Mathf.Min(back, piece.Rect.MinZ));
            }
            Valid &= all != null;
            Bounds = all ?? new KwRect(0f, 0f, 0f, 0f);
            (Front, Back) = all != null ? (front, back) : (0f, 0f);
        }
    }

    /// <summary>
    /// Where and how a scene stands (DECORATE.md, "Scenes"): the cell its middle is on, its front turned to one of the
    /// area's four ways, mirrored or not; scene coordinates to the world and to the grid's cells. Turned square to the
    /// area's frame (a dock's own way, a building's), so its footprints are rectangles of whole cells. Plain arithmetic.
    /// </summary>
    public readonly struct KwSiteFrame
    {
        private const float Inset = 0.1f;

        public KwSiteFrame(KwDecorGrid grid, int cell, int dir, bool mirror)
        {
            (Cell, Dir, Mirror) = (cell, dir, mirror);
            (Origin, Front, Right, Yaw) = (grid.On(cell), grid.Dir(dir), grid.Dir((dir + 3) & 3), grid.YawOf(dir));
        }

        public int Cell { get; }
        public int Dir { get; }
        public bool Mirror { get; }
        public Vector3 Origin { get; }
        public Vector3 Front { get; }
        public Vector3 Right { get; }
        public float Yaw { get; }

        /// <summary>A point of the scene's frame in the world, at its middle's height.</summary>
        public Vector3 World(float x, float z) => Origin + Right * (Mirror ? -x : x) + Front * z;

        /// <summary>A piece's turn in the world: the scene's, plus its own, mirrored (or, for one whose ends differ, turned round).</summary>
        public float YawOf(KwSceneMember member) => Yaw + (Mirror ? (member.Ends ? 180f - member.Yaw : -member.Yaw) : member.Yaw);

        /// <summary>A rectangle of the scene's frame on the ground.</summary>
        public KwDecorBox Box(KwRect rect) =>
            new KwDecorBox(World((rect.MinX + rect.MaxX) / 2f, (rect.MinZ + rect.MaxZ) / 2f), Right, (rect.MaxX - rect.MinX) / 2f, (rect.MaxZ - rect.MinZ) / 2f);

        /// <summary>The cells a rectangle of the scene's frame covers (a tenth of a cell in from its sides), the ends not included.</summary>
        public (int I0, int J0, int I1, int J1) Cells(KwDecorGrid grid, KwRect rect)
        {
            Vector2 a = grid.Coords(World(rect.MinX, rect.MinZ)), b = grid.Coords(World(rect.MaxX, rect.MaxZ));
            int i0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) + Inset), i1 = Mathf.CeilToInt(Mathf.Max(a.x, b.x) - Inset);
            int j0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) + Inset), j1 = Mathf.CeilToInt(Mathf.Max(a.y, b.y) - Inset);
            return (i0, j0, i1, j1);
        }
    }
}
