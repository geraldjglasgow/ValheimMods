using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What stands along an edge a ladder may stand against (<see cref="KwLadderEdge"/>), as bounds along it for the
    /// ladder tool's centring (<see cref="KwLadderSpans"/>): each doorway and window, its frame with it, in a wall right
    /// under the edge and running along it (both its sides within <see cref="Near"/> of the edge across the ground, its sill
    /// under the edge's top and its top over the floor the ladder would stand on there); each cabin's corner post standing
    /// there; each ladder already standing on the edge (its top on it, facing out over the same drop). A part's kit points
    /// go to the world by a function and the floor's height along the edge comes as one, so the same numbers serve offline.
    /// Pure.
    /// </summary>
    public static class KwLadderFinds
    {
        /// <summary>How far across the ground from the edge a wall or a post may stand and be under it; how near a ladder's top must be.</summary>
        public const float Near = 0.35f, OnEdge = 0.3f;

        // How nearly a wall or a ladder must run the edge's way; how much of something must stand between the floor and the edge.
        private const float Square = 0.7f, Gap = 0.05f;

        /// <summary>The openings of a part's walls under the edge, as bounds (a doorway's, a window's).</summary>
        public static void Openings(KwLadderEdge edge, List<KwOpeningWall> walls, Func<Vector3, Vector3> world, Func<float, float> floor, List<KwLadderBound> into)
        {
            foreach (KwOpeningWall wall in walls)
            {
                foreach (KwOpening o in wall.Openings)
                {
                    if (Opening(edge, wall.Surface, o, world, floor) is KwLadderBound bound)
                        into.Add(bound);
                }
            }
        }

        // An opening as a bound along the edge, its frame with it; null when it is not under the edge or not along it.
        private static KwLadderBound? Opening(KwLadderEdge edge, KwOpeningSurface surface, KwOpening o, Func<Vector3, Vector3> world, Func<float, float> floor)
        {
            float wide = KwOpeningFrames.Width(o.Frame).Wide, z = o.Sill + o.Height / 2f;
            Vector3 a = world(surface.Point(o.U0 - wide, z, 0f)), b = world(surface.Point(o.U1 + wide, z, 0f));
            if (edge.Off(a) > Near || edge.Off(b) > Near)
                return null;
            float s0 = edge.Along(a), s1 = edge.Along(b), mid = (s0 + s1) / 2f;
            float sill = world(surface.Point(o.U, o.Sill, 0f)).y, top = world(surface.Point(o.U, o.Top, 0f)).y;
            if (!Lengthwise(edge, b - a, mid) || !Between(edge, mid, Mathf.Min(sill, top), Mathf.Max(sill, top), floor))
                return null;
            KwLadderBoundKind kind = o.IsDoorway ? KwLadderBoundKind.Doorway : KwLadderBoundKind.Window;
            return new KwLadderBound(Mathf.Min(s0, s1), Mathf.Max(s0, s1), kind, KwLadderBound.Word(kind));
        }

        /// <summary>A cabin's corner posts (their centre lines from the deck up, <see cref="KwCabinPosts.Of"/>) under the edge, as bounds.</summary>
        public static void Posts(KwLadderEdge edge, List<List<Vector3>> posts, Func<Vector3, Vector3> world, Func<float, float> floor, List<KwLadderBound> into)
        {
            const float half = KwCabinPosts.Size / 2f;
            foreach (List<Vector3> post in posts)
            {
                Vector3 foot = world(post[0]), head = world(post[post.Count - 1]);
                float s = edge.Along(head);
                if (edge.Off(head) <= Near && Between(edge, s, Mathf.Min(foot.y, head.y), Mathf.Max(foot.y, head.y), floor))
                    into.Add(new KwLadderBound(s - half, s + half, KwLadderBoundKind.Solid, KwLadderBound.Corner));
            }
        }

        /// <summary>A ladder standing with its top there and facing that way, as a bound when it stands on the edge.</summary>
        public static void Ladder(KwLadderEdge edge, Vector3 top, Vector3 facing, List<KwLadderBound> into)
        {
            float s = edge.Along(top);
            Vector3 way = new Vector3(facing.x, 0f, facing.z);
            if (edge.Off(top) > OnEdge || Mathf.Abs(top.y - edge.At(s).y) > OnEdge || way.sqrMagnitude < 1e-6f
                || Vector3.Dot(way.normalized, edge.OutwardAt(s)) < Square)
                return;
            KwLadderBoundKind kind = KwLadderBoundKind.Ladder;
            into.Add(new KwLadderBound(s - KwLadderSpots.Half, s + KwLadderSpots.Half, kind, KwLadderBound.Word(kind)));
        }

        // Whether a way across the ground runs along the edge at s (either way).
        private static bool Lengthwise(KwLadderEdge edge, Vector3 run, float s)
        {
            Vector3 flat = new Vector3(run.x, 0f, run.z);
            return flat.sqrMagnitude > 1e-6f && Mathf.Abs(Vector3.Dot(flat.normalized, edge.WayAt(s))) >= Square;
        }

        // Whether something from lo to hi high stands between the floor under the edge at s and the edge's top.
        private static bool Between(KwLadderEdge edge, float s, float lo, float hi, Func<float, float> floor) =>
            hi > floor(s) + Gap && lo < edge.At(s).y - Gap;
    }
}
