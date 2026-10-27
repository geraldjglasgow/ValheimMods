using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Lays a planking part's planks where the part stands along its ship, so the wood runs on from part to part as if
    /// from one long texture (<see cref="KwUv"/>, 64 m a round). Each part's mesh is built with u running along each strake
    /// from the part's start (<see cref="KwGirth"/>: at a rib the strakes come out at the part's own length); here u becomes
    /// the place along the ship's own axis: the part's start rib's distance along that axis, plus u the way the part runs
    /// (a part laid the other way round runs its u backwards), plus each strake's own start into the texture
    /// (<see cref="KwUv.RowStart"/>, its side counted on the ship: a part laid backwards has its port side on the ship's
    /// starboard). The ship's axis is the part's heading turned into one half of the compass (so parts laid either way
    /// share it) and rounded to <see cref="HeadingStep"/>, so parts of one keel whose headings differ by a hair, worked
    /// out from their ribs' places far from the world's middle, still share it exactly. Every peer works it out from the
    /// part's place alone; the dock's parts from where they stand, a preview from where its part will, a launched ship's
    /// and a ghost's from the blueprint's frame. Each vertex's strake and side come in the mesh's row channel
    /// (<see cref="MeshBuilder.RowChannel"/>), which is let go once used.
    /// </summary>
    public static class KwPlankShift
    {
        /// <summary>The step a part's heading is rounded to, in degrees (the game's 22.5 degree building turns are whole steps of it).</summary>
        public const float HeadingStep = 2.25f;

        // The heading (degrees from the world's +z towards +x) where the ship's axis turns round: half a building turn from
        // any heading the hammer gives, so no part laid on a dock sits on the turn.
        private const float Turn = 101.25f;

        public static void Apply(Mesh mesh, Transform part) => Apply(mesh, part.position, part.forward);

        public static void Apply(Mesh mesh, Vector3 position, Vector3 forward)
        {
            List<Vector2> uv = new List<Vector2>(), rows = new List<Vector2>();
            mesh.GetUVs(0, uv);
            mesh.GetUVs(MeshBuilder.RowChannel, rows);
            Apply(uv, rows, position, forward);
            mesh.SetUVs(0, uv);
            mesh.uv3 = null;
        }

        /// <summary>
        /// The planks of a part starting at position and running forward laid along the ship: each u (along its strake from
        /// the part's start) moved to the ship's axis, with its strake's own start; rows holds each vertex's (strake, side).
        /// </summary>
        public static void Apply(List<Vector2> uv, List<Vector2> rows, Vector3 position, Vector3 forward)
        {
            (Vector3 axis, float way) = Axis(forward);
            float start = KwUv.Plank(Mathf.Repeat(Vector3.Dot(position, axis), KwUv.PlankRepeat));
            for (int i = 0; i < uv.Count; i++)
            {
                Vector2 row = i < rows.Count ? rows[i] : Vector2.zero;
                float own = KwUv.RowStart(Mathf.RoundToInt(row.x), Mathf.RoundToInt(row.y * way));
                uv[i] = new Vector2(start + way * uv[i].x + own, uv[i].y);
            }
        }

        /// <summary>
        /// The ship's axis a part running forward lies on (level, its heading rounded to <see cref="HeadingStep"/>), and 1
        /// when the part runs along it, -1 when against it.
        /// </summary>
        public static (Vector3 Axis, float Way) Axis(Vector3 forward)
        {
            float heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            bool along = Mathf.Repeat(heading - (Turn - 180f), 360f) < 180f;
            float kept = Mathf.Round((along ? heading : heading + 180f) / HeadingStep) * HeadingStep * Mathf.Deg2Rad;
            return (new Vector3(Mathf.Sin(kept), 0f, Mathf.Cos(kept)), along ? 1f : -1f);
        }
    }
}
