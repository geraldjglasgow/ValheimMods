using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The loops of a footprint laid on the current ground: every point takes the terrain height there (buildings
    /// ignored, since the brush edits the terrain beneath them). Reading heights is the costly part, so the result is
    /// kept until the footprint changes or half a second has passed (the ground may have been edited meanwhile). The
    /// point arrays are rewritten in place while the point count stays, so moving the brush allocates nothing.
    /// </summary>
    internal sealed class GroundLoops
    {
        private const float RefreshSeconds = 0.5f;

        /// <summary>The brush footprint's loops, shared by the outline and the volume so the ground is read once.</summary>
        public static readonly GroundLoops Brush = new GroundLoops();

        private readonly List<Vector3[]> loops = new List<Vector3[]>();
        private readonly List<Vector2[]> flat = new List<Vector2[]>();
        private FootprintSpec last;
        private float builtAt = -10f;

        /// <summary>Counts the rebuilds, so a second reader can tell the loops changed since it last looked.</summary>
        public int Version { get; private set; }

        /// <summary>The loops (outer first) with ground heights; true when they were rebuilt this call.</summary>
        public bool Get(FootprintSpec spec, out List<Vector3[]> result)
        {
            result = loops;
            if (spec.SameAs(last) && Time.time - builtAt < RefreshSeconds)
                return false;
            last = spec;
            builtAt = Time.time;
            Version++;
            OutlineShape.Loops(spec, flat);
            for (int n = 0; n < flat.Count; n++)
                OnGround(flat[n], spec.CenterY, OutlineShape.Slot(loops, n, flat[n].Length));
            loops.RemoveRange(flat.Count, loops.Count - flat.Count);
            return true;
        }

        private static void OnGround(Vector2[] flat, float fallback, Vector3[] points)
        {
            for (int i = 0; i < flat.Length; i++)
            {
                Vector3 p = new Vector3(flat[i].x, fallback, flat[i].y);
                p.y = GroundSampler.Height(p, fallback);
                points[i] = p;
            }
        }
    }
}
