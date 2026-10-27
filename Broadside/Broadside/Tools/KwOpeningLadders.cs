using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ladders standing against a wall, as stretches along it, for the centring snap (<see cref="KwOpeningSpans"/>): each
    /// of the ship's ladders (<see cref="KwLadder"/>: its top on an edge, its rails <see cref="Half"/> either side of its
    /// middle across it, hanging its saved length down) whose rails run along the wall, whose middle is within
    /// <see cref="Near"/> of the wall's face, and which reaches into the heights asked. Read on the player's game from the
    /// ladders of the ship last looked up (<see cref="KwOpeningReads.Ladders"/>); on a launched ship from its blueprint's
    /// ladders in its frame (<see cref="KwSeaOpeningShip"/>).
    /// </summary>
    public static class KwOpeningLadders
    {
        /// <summary>Half a ladder's width across its rails, and how near a wall's face it stands to stand against it.</summary>
        public const float Half = 0.25f, Near = 0.5f;

        /// <summary>The ladders against the site's wall that reach into the heights z0 to z1 (kit z), as stretches along it.</summary>
        public static List<(float Lo, float Hi)> Along(KwOpeningSite site, float z0, float z1)
        {
            List<(float Lo, float Hi)> found = new List<(float, float)>();
            Piece? part = site.Piece;
            if (part == null)
                return found;
            foreach (Piece ladder in KwOpeningReads.Ladders)
            {
                if (ladder == null || !KwLadder.TryLength(ladder.m_nview.GetZDO().GetString(KwGenerated.Key), out float length))
                    continue;
                Transform at = ladder.transform;
                (float Lo, float Hi)? stretch = Against(site, p => KwOpeningHull.Kit(part, p), w => KwMesh.ToKit(part.transform.InverseTransformDirection(w)),
                    new KwLadderPose(at.position, at.up, at.forward, at.right, length), z0, z1);
                if (stretch != null)
                    found.Add(stretch.Value);
            }
            return found;
        }

        /// <summary>
        /// A ladder's stretch along a site's wall, when it stands against it within the heights: the ladder's pose in some frame,
        /// and how a point and a way of that frame read in the site's part's kit axes. Pure.
        /// </summary>
        public static (float Lo, float Hi)? Against(KwOpeningSite site, System.Func<Vector3, Vector3> kit, System.Func<Vector3, Vector3> kitWay, KwLadderPose at, float z0, float z1)
        {
            Vector3 top = kit(at.Top), foot = kit(at.Top - at.Up * at.Length);
            if (Mathf.Max(foot.z, z0) >= Mathf.Min(top.z, z1))
                return null;
            Vector3 middle = kit(at.Top - at.Up * (at.Length / 2f) + at.Forward * 0.065f);
            float u = site.AlongOf(middle);
            (float from, float to) = site.Surface.Reach;
            if (u < from - Half || u > to + Half)
                return null;
            float on = Mathf.Clamp(u, from, to);
            (float lo, float hi) = site.Surface.Range(on);
            Vector3 face = site.Surface.Point(on, Mathf.Clamp(middle.z, lo, hi), 0f), across = kitWay(at.Right);
            if (Vector3.Distance(face, middle) > Near || Mathf.Abs(Vector3.Dot(across, site.Surface.Along(on, Mathf.Clamp(middle.z, lo, hi)))) < 0.8f)
                return null;
            return (u - Half, u + Half);
        }
    }

    /// <summary>A ladder as it stands: its top, its ways up, forward and right (a placed ladder's transform's), its length.</summary>
    public readonly struct KwLadderPose
    {
        public KwLadderPose(Vector3 top, Vector3 up, Vector3 forward, Vector3 right, float length) =>
            (Top, Up, Forward, Right, Length) = (top, up, forward, right, length);

        public Vector3 Top { get; }
        public Vector3 Up { get; }
        public Vector3 Forward { get; }
        public Vector3 Right { get; }
        public float Length { get; }
    }
}
