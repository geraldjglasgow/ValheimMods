using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>Where the world grid is drawn: its origin and turn, and the point it is centred on.</summary>
    internal struct GridFrame
    {
        public Vector3 Origin;
        public float Yaw;
        public Vector3 Aim;
    }

    /// <summary>
    /// Builds the world grid as one mesh of flat ribbons lying on the ground: lines every spacing metres in the grid's
    /// own frame (through its origin, turned by its yaw), clipped to a circle around the aimed point and sampled along
    /// their length so they follow the terrain. Line count and samples are capped so a large radius with a fine
    /// spacing stays cheap (the spacing widens instead).
    /// </summary>
    internal static class GridMesh
    {
        private const float Lift = 0.05f;
        private const int MaxLinesPerAxis = 120;
        private const int MaxSamplesPerLine = 60;

        public static void Build(MeshData data, GridFrame frame)
        {
            data.Clear(frame.Aim);
            float radius = HudSettings.GridRadius.Value;
            float spacing = Mathf.Max(HudSettings.GridSpacing.Value, 2f * radius / MaxLinesPerAxis);
            Vector2 local = OutlineShape.Turn(new Vector2(frame.Aim.x - frame.Origin.x, frame.Aim.z - frame.Origin.z), -frame.Yaw);
            Axis(data, frame, local.x, local.y, radius, spacing, false);
            Axis(data, frame, local.y, local.x, radius, spacing, true);
        }

        /// <summary>All lines of one direction: constant across-coordinate k·spacing, running along the other axis.</summary>
        private static void Axis(MeshData data, GridFrame frame, float across, float along, float radius, float spacing, bool swapped)
        {
            int every = HudSettings.GridMajorEvery.Value;
            int first = Mathf.CeilToInt((across - radius) / spacing);
            int last = Mathf.FloorToInt((across + radius) / spacing);
            for (int k = first; k <= last; k++)
            {
                float offset = k * spacing;
                float half = Mathf.Sqrt(Mathf.Max(0f, radius * radius - (offset - across) * (offset - across)));
                if (half < 0.05f)
                    continue;
                Color colour = every > 0 && k % every == 0 ? HudSettings.GridMajorColour.Value : HudSettings.GridColour.Value;
                Line(data, frame, offset, along - half, along + half, swapped, colour);
            }
        }

        private static void Line(MeshData data, GridFrame frame, float offset, float from, float to, bool swapped, Color colour)
        {
            int samples = Mathf.Clamp(Mathf.CeilToInt(to - from), 1, MaxSamplesPerLine);
            float halfWidth = HudSettings.GridLineWidth.Value * 0.5f;
            Vector3 previous = Point(frame, offset, from, swapped);
            for (int i = 1; i <= samples; i++)
            {
                Vector3 next = Point(frame, offset, Mathf.Lerp(from, to, (float)i / samples), swapped);
                Vector3 direction = next - previous;
                direction.y = 0f;
                Vector3 side = new Vector3(-direction.z, 0f, direction.x).normalized * halfWidth;
                data.Quad(previous - side, previous + side, next + side, next - side, colour);
                previous = next;
            }
        }

        /// <summary>A grid point in the world, on the ground.</summary>
        private static Vector3 Point(GridFrame frame, float offset, float along, bool swapped)
        {
            Vector2 local = swapped ? new Vector2(along, offset) : new Vector2(offset, along);
            Vector2 turned = OutlineShape.Turn(local, frame.Yaw);
            Vector3 world = new Vector3(frame.Origin.x + turned.x, frame.Aim.y, frame.Origin.z + turned.y);
            world.y = GroundSampler.Height(world, frame.Aim.y) + Lift;
            return world;
        }
    }
}
