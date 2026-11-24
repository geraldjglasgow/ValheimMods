using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// Where a fake player walks: a loop round a centre at a steady speed (a loop under half a metre, or no speed, and it
    /// stands), its direction and starting point its own, its angle counted from when it joined so it stays exact however
    /// long the server runs. The height comes from the ground at <see cref="Samples"/> points round the loop: the terrain
    /// collider where this game has the zone loaded, else the world generator's height (the ground as first made, without
    /// players' digging and levelling), never below the water. The points are read again one at a time
    /// (<see cref="Refresh"/>, one a frame across every fake, from <see cref="FakeTicker"/>), so a loop settles onto the
    /// real ground once its zone loads here.
    /// </summary>
    internal sealed class FakePath
    {
        internal const int Samples = 24;
        private const float MinRadius = 0.5f;
        private const float Tau = Mathf.PI * 2f;
        private readonly float[] heights = new float[Samples];
        private readonly float angular;
        private readonly float phase;
        private readonly float start;

        internal FakePath(Vector3 centre, float radius, float speed, float phase, bool clockwise, float start)
        {
            Centre = new Vector3(centre.x, 0f, centre.z);
            Radius = Mathf.Max(0f, radius);
            Speed = Radius < MinRadius ? 0f : Mathf.Max(0f, speed);
            angular = Radius < MinRadius ? 0f : Speed / Radius * (clockwise ? -1f : 1f);
            this.phase = phase;
            this.start = start;
            for (int k = 0; k < Samples; k++) Refresh(k);
        }

        /// <summary>The loop's centre on the ground plane (y is 0).</summary>
        internal Vector3 Centre { get; }

        internal float Radius { get; }

        internal float Speed { get; }

        internal Vector3 Position(float time)
        {
            float angle = Angle(time);
            Vector3 p = Centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Radius;
            p.y = HeightAt(angle);
            return p;
        }

        /// <summary>Along the loop, the way it turns, at the path's speed.</summary>
        internal Vector3 Velocity(float time)
        {
            float angle = Angle(time);
            return new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (Radius * angular);
        }

        /// <summary>Facing the way it walks; standing, facing out of its loop.</summary>
        internal Quaternion Facing(float time)
        {
            Vector3 way = Velocity(time);
            if (way.sqrMagnitude < 1e-4f) way = Position(time) - Centre;
            way.y = 0f;
            return way.sqrMagnitude < 1e-4f ? Quaternion.identity : Quaternion.LookRotation(way);
        }

        /// <summary>Reads the ground under point k of the loop again.</summary>
        internal void Refresh(int k)
        {
            float angle = Tau * k / Samples;
            heights[k] = Ground(Centre.x + Mathf.Cos(angle) * Radius, Centre.z + Mathf.Sin(angle) * Radius);
        }

        /// <summary>The terrain here where its zone is loaded, else the world generator's ground; never under water.</summary>
        internal static float Ground(float x, float z)
        {
            ZoneSystem zones = ZoneSystem.instance;
            float water = zones ? zones.m_waterLevel : 30f;
            if (zones && zones.GetGroundHeight(new Vector3(x, 0f, z), out float hit)) return Mathf.Max(hit, water);
            float made = WorldGenerator.instance != null ? WorldGenerator.instance.GetHeight(x, z) : water;
            return Mathf.Max(made, water);
        }

        private float Angle(float time) => phase + angular * (time - start);

        private float HeightAt(float angle)
        {
            float at = Mathf.Repeat(angle / Tau, 1f) * Samples;
            int k = Mathf.FloorToInt(at) % Samples;
            return Mathf.Lerp(heights[k], heights[(k + 1) % Samples], at - Mathf.Floor(at));
        }
    }
}
