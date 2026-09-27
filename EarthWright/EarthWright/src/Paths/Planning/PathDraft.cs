using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// What the player asked for, before it is planned against the terrain: the points (a ramp's start and end, or a
    /// road's waypoints, with their heights in Y), the profile, the cross-section and the surface paint.
    /// </summary>
    public sealed class PathDraft
    {
        public bool IsRoad;

        /// <summary>Ramp: start and end. Road: the waypoints in order.</summary>
        public readonly List<Vector3> Points = new List<Vector3>();

        /// <summary>Ramp only.</summary>
        public RampProfile Profile;

        /// <summary>Ramp only: the soft join length in metres (profile SoftJoins).</summary>
        public float JoinLength;

        public PathShape Shape;

        /// <summary>The paint of the surface; <see cref="PaintOp.None"/> keeps the ground's paint.</summary>
        public PaintOp Paint;

        /// <summary>The centre line this draft plans.</summary>
        public CentreLine BuildLine()
        {
            if (IsRoad)
                return LineBuilder.Road(Points);
            if (Points.Count < 2)
                return new CentreLine();
            return LineBuilder.Ramp(Points[0], Points[1], Profile, JoinLength);
        }
    }

    /// <summary>One terrain vertex of a plan: the target it gets and the terrain it has now (as this machine sees it).</summary>
    public struct PlannedVertex
    {
        /// <summary>World vertex coordinates, whole metres.</summary>
        public int X;
        public int Z;

        /// <summary>The planned surface height here (absolute).</summary>
        public float Target;

        /// <summary>0..1: how far the vertex moves from its current height to <see cref="Target"/>.</summary>
        public float Weight;

        public PaintOp Paint;

        /// <summary>A loaded heightmap covers the vertex, so <see cref="Current"/> and <see cref="Base"/> are known.</summary>
        public bool Loaded;

        public float Current;

        /// <summary>The world's generated height here.</summary>
        public float Base;

        /// <summary>The height would end past the raise or dig limit.</summary>
        public bool PastLimit;

        /// <summary>The height the vertex will have.</summary>
        public float Final => Current + (Target - Current) * Weight;
    }
}
