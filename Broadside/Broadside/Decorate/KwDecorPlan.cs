using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where one decoration of a layout goes, as the cells are tested: its kind of place, its cell, its point (for a piece
    /// set against something, the line of the edge or wall behind it; else its middle on the surface), the way to its back
    /// (the edge, wall or water; none for a free piece), whether its back is put against that line, its floor, and how far
    /// in from that line a piece set against a dock's edge may reach into its lane (<see cref="KwDecorQuay"/>: the dock
    /// stays at least 1.6 m clear across with room for a post on the other side; 0 elsewhere).
    /// </summary>
    public readonly struct KwSlot
    {
        public KwSlot(KwDecorPlace place, int cell, Vector3 at, Vector3 back, bool pushed, short region, float reach = 0f)
        {
            (Place, Cell, At, Back, Pushed, Region, Reach) = (place, cell, at, back, pushed, region, reach);
        }

        public KwDecorPlace Place { get; }
        public int Cell { get; }
        public Vector3 At { get; }
        public Vector3 Back { get; }
        public bool Pushed { get; }
        public short Region { get; }
        public float Reach { get; }
    }

    /// <summary>
    /// One decoration in a layout: what, where, which way, the slot it took, its footprint on the ground, the density
    /// that first shows it (1 to <see cref="KwDecorLayout.Densities"/>) and its group (a scene's pieces share one, so
    /// they are not kept apart from each other; -1 for the quay's own pieces).
    /// </summary>
    public sealed class KwDecorPlan
    {
        public KwDecorPlan(KwDecorKind kind, Vector3 position, Quaternion rotation, KwSlot slot, int level, int group)
        {
            (Kind, Position, Rotation, Slot, Level, Group) = (kind, position, rotation, slot, level, group);
            Box = KwDecorFoot.Box(kind.Fit, position, rotation);
        }

        public KwDecorKind Kind { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public KwSlot Slot { get; }
        public KwDecorBox Box { get; }

        /// <summary>The density that first shows it.</summary>
        public int Level { get; set; }

        public int Group { get; }

        /// <summary>What it costs: its piece's recipe.</summary>
        public Piece.Requirement[] Cost => Kind.Piece.m_resources;

        /// <summary>The same plan as another decoration of the same footprint (a wood tier's copy, <see cref="KwDecorTiers"/>).</summary>
        public KwDecorPlan As(KwDecorKind kind) => new KwDecorPlan(kind, Position, Rotation, Slot, Level, Group);
    }

    /// <summary>
    /// A decoration's footprint at a pose (<see cref="KwDecorFit.Width"/> by <see cref="KwDecorFit.Depth"/>: about its origin
    /// for a standing piece, in front of it for a hanging one) and the points the reading's cells are tested at, every
    /// 0.25 m over it, 5 cm in from its sides (10 cm off a hanging piece's back, clear of the wall or dock side it hangs on).
    /// </summary>
    public static class KwDecorFoot
    {
        private const float Every = 0.25f, Inset = 0.05f, OffBack = 0.1f, Trusted = 0.2f;

        public static KwDecorBox Box(KwDecorFit fit, Vector3 origin, Quaternion turn)
        {
            (float minZ, float maxZ) = fit.Hangs ? (0f, fit.Depth) : (-fit.Depth / 2f, fit.Depth / 2f);
            return KwDecorBox.Of(origin, turn, -fit.Width / 2f, fit.Width / 2f, minZ, maxZ);
        }

        /// <summary>
        /// The test points, into a list the caller keeps (cleared first). For a piece set against a line (an edge or wall,
        /// found to 2 cm by rays) its strip along the back is left to that line and the solid test, not the half-metre cells.
        /// </summary>
        public static void Samples(KwDecorFit fit, Vector3 origin, Quaternion turn, bool backed, List<Vector3> points)
        {
            points.Clear();
            float back = fit.Hangs ? 0f : -fit.Depth / 2f, maxZ = fit.Hangs ? fit.Depth - Inset : fit.Depth / 2f - Inset;
            float minZ = Mathf.Min(maxZ, back + (backed ? Trusted : fit.Hangs ? OffBack : Inset));
            float minX = Mathf.Min(0f, -fit.Width / 2f + Inset), maxX = Mathf.Max(0f, fit.Width / 2f - Inset);
            Vector3 right = turn * Vector3.right, forward = turn * Vector3.forward;
            int across = Steps(maxX - minX), along = Steps(maxZ - minZ);
            for (int i = 0; i <= across; i++)
            {
                for (int j = 0; j <= along; j++)
                    points.Add(origin + right * Mathf.Lerp(minX, maxX, Share(i, across)) + forward * Mathf.Lerp(minZ, maxZ, Share(j, along)));
            }
        }

        // Even steps no longer than a quarter metre over a length, both ends included.
        private static int Steps(float length) => Mathf.Max(1, Mathf.CeilToInt(length / Every));

        private static float Share(int i, int steps) => i / (float)steps;
    }

    /// <summary>
    /// A decoration's pose (DECORATE.md, "Scenes" and "The quay"): set against a line (a dock's edge or water edge) its
    /// front faces away from it, its back 8 cm inside the line; hung on a wall or a dock's side it hangs at its lift over
    /// the floor, or its height under the deck's top, facing out; a scene's piece stands where the scene puts it, turned
    /// as the scene turns it. A degree or two off at random, none for a hung piece.
    /// </summary>
    public static class KwDecorPose
    {
        private const float Gap = 0.08f, Snug = 1.5f;

        /// <summary>Against the line behind the slot, facing away from it.</summary>
        public static (Vector3 Position, Quaternion Rotation) Pushed(KwSlot slot, KwDecorFit fit, System.Random dice)
        {
            float yaw = Facing(-slot.Back) + Jitter(dice, Snug);
            return (slot.At - slot.Back * (fit.Depth / 2f + Gap), Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>Hung on the wall or the dock's side behind the slot.</summary>
        public static (Vector3 Position, Quaternion Rotation) Hung(KwSlot slot, KwDecorFit fit)
        {
            Vector3 at = slot.At;
            at.y += slot.Place == KwDecorPlace.Wall ? fit.Lift : -fit.Height;
            return (at, Quaternion.Euler(0f, Facing(-slot.Back), 0f));
        }

        /// <summary>A scene's piece at its yaw, a little at random.</summary>
        public static Quaternion Free(float yaw, System.Random dice) => Quaternion.Euler(0f, yaw + Jitter(dice, Snug), 0f);

        public static float Facing(Vector3 front) => Mathf.Atan2(front.x, front.z) * Mathf.Rad2Deg;

        private static float Jitter(System.Random dice, float spread) => ((float)dice.NextDouble() * 2f - 1f) * spread;
    }
}
