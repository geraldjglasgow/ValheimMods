using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock section's timbers from its saved shape (<see cref="KwPierSpec"/>) in its material (<see cref="KwPierKind"/>),
    /// in the section's frame (x across, y up from the deck's top, z along from its start): rows of deck boards across the
    /// full width, their butt joints staggered row by row and each board a hair higher or lower than the next; joists along
    /// under them; a cap beam across at each station (none at a joined start: the dock it meets has its own there); a pair
    /// of pilings at each station under the deck's edges, down to their saved feet; and a low curb along each side edge on
    /// the deck, left out where another section meets that side (<see cref="KwPierGaps"/>: a branch's deck runs on from
    /// this one's with no lip), each run of it ended square or, a log, capped. Core wood's joists, caps, pilings and curbs
    /// are round logs. The seed decides every joint and texture place, so every peer builds the same. Plain wood is the
    /// rough, cheap tier and is built its own way (<see cref="KwPierRough"/>: uneven, crooked boards, pole pilings, rope
    /// lashings, an odd curb); core wood and fine wood are built here. Plain geometry; the colliders are
    /// <see cref="KwPierBody"/>'s.
    /// </summary>
    public static class KwPierShape
    {
        /// <summary>The shortest piling drawn (under it the ground is at the cap beam already).</summary>
        public const float MinPiling = 0.05f;

        private const float Joint = 0.004f, MinBoard = 0.3f, JoistSpacing = 0.9f;

        /// <summary>A section's timbers into the mesh, its curbs left out where the gaps say (none given: curbs all along).</summary>
        public static void Build(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierGaps? gaps = null)
        {
            if (kind == KwPierKinds.Wood)
            {
                KwPierRough.Build(mesh, spec, kind, gaps ?? KwPierGaps.None);
                return;
            }
            KwPierUv uv = KwPierUv.For(kind, spec.Seed);
            System.Random random = new System.Random(spec.Seed);
            Boards(mesh, spec, kind, uv, random);
            Joists(mesh, spec, kind, uv, random);
            for (int i = 0; i < spec.Stations; i++)
            {
                if (!spec.Joined || i > 0)
                    Cap(mesh, spec, kind, uv, random, i);
                Pilings(mesh, spec, kind, uv, random, i);
            }
            Curbs(mesh, spec, kind, uv, random, gaps ?? KwPierGaps.None);
        }

        private static void Boards(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierUv uv, System.Random random)
        {
            float pitch = kind.Board + kind.Gap, half = spec.Width * 0.5f;
            for (float z = 0f; z < spec.Length - 0.02f; z += pitch)
                Row(mesh, kind, uv, random, half, z, Mathf.Min(z + kind.Board, spec.Length));
        }

        // One row of boards across the deck from z0 to z1, cut every board's run from a place of its own.
        private static void Row(KwPierMesh mesh, KwPierKind kind, KwPierUv uv, System.Random random, float half, float z0, float z1)
        {
            List<float> edges = new List<float> { -half };
            for (float cut = -half + MinBoard + (float)random.NextDouble() * (kind.BoardRun - MinBoard); cut < half - MinBoard; cut += kind.BoardRun)
                edges.Add(cut);
            edges.Add(half);
            for (int i = 0; i + 1 < edges.Count; i++)
            {
                float a = edges[i] + (i > 0 ? Joint : 0f), b = edges[i + 1] - (i + 2 < edges.Count ? Joint : 0f);
                float lift = ((float)random.NextDouble() - 0.5f) * 0.006f;
                mesh.Box(new Vector3(a, lift - kind.Thick, z0), new Vector3(b, lift, z1), 0, uv, uv.Board(b - a));
            }
        }

        private static void Joists(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierUv uv, System.Random random)
        {
            int count = Mathf.Max(2, Mathf.RoundToInt(spec.Width / JoistSpacing) + 1);
            float edge = spec.Width * 0.5f - kind.Joist.x * 0.5f - 0.04f;
            for (int i = 0; i < count; i++)
                Along(mesh, kind, uv, random, new Vector2(Mathf.Lerp(-edge, edge, i / (count - 1f)), -kind.Thick - kind.Joist.y * 0.5f), kind.Joist, new Vector2(0f, spec.Length));
        }

        // A timber along the section from span.x to span.y (a joist, a run of curb) centred at (x, y): cut every run, a log
        // for core wood, capped at both ends (a joint's two caps meet inside the logs; a branch's shows its end grain).
        private static void Along(KwPierMesh mesh, KwPierKind kind, KwPierUv uv, System.Random random, Vector2 at, Vector2 size, Vector2 span)
        {
            if (kind.Round)
            {
                mesh.Log(new Vector3(at.x, at.y, span.x), new Vector3(at.x, at.y, span.y), size.x * 0.5f, uv, true, true);
                return;
            }
            Vector3 half = new Vector3(size.x, size.y, 0f) * 0.5f;
            for (float z = span.x; z < span.y - 0.001f; z += uv.Run)
                mesh.Box(new Vector3(at.x, at.y, z) - half, new Vector3(at.x, at.y, Mathf.Min(span.y, z + uv.Run)) + half, 2, uv, uv.Timber(uv.Run));
        }

        private static void Cap(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierUv uv, System.Random random, int station)
        {
            float half = spec.Width * 0.5f, z = Mathf.Clamp(spec.StationZ(station), kind.Cap.x * 0.5f, spec.Length - kind.Cap.x * 0.5f);
            if (kind.Round)
            {
                float y = kind.JoistFoot - kind.Cap.y * 0.5f;
                mesh.Log(new Vector3(-half, y, z), new Vector3(half, y, z), kind.Cap.y * 0.5f, uv, true, true);
                return;
            }
            mesh.Box(new Vector3(-half, kind.CapFoot, z - kind.Cap.x * 0.5f), new Vector3(half, kind.JoistFoot, z + kind.Cap.x * 0.5f), 0, uv, uv.Timber(spec.Width));
        }

        private static void Pilings(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierUv uv, System.Random random, int station)
        {
            Vector2 depth = spec.Depths[station];
            foreach ((float x, float d) in new[] { (-PostX(spec, kind), depth.x), (PostX(spec, kind), depth.y) })
                Piling(mesh, kind, uv, random, new Vector2(x, PostZ(spec, kind, station)), d);
        }

        // One piling from under the cap beam down to its foot (none when that is no length): a log, or a post cut every run.
        private static void Piling(KwPierMesh mesh, KwPierKind kind, KwPierUv uv, System.Random random, Vector2 at, float depth)
        {
            float top = kind.CapFoot, foot = -depth, half = kind.Post * 0.5f;
            if (top - foot < MinPiling)
                return;
            if (kind.Round)
            {
                mesh.Log(new Vector3(at.x, top, at.y), new Vector3(at.x, foot, at.y), half, uv, true, false);
                return;
            }
            for (float y = top; y > foot + 0.001f; y -= uv.Run)
                mesh.Box(new Vector3(at.x - half, Mathf.Max(foot, y - uv.Run), at.y - half), new Vector3(at.x + half, y, at.y + half), 1, uv, uv.Timber(uv.Run));
        }

        // Each side's curb in the runs no other section meets.
        private static void Curbs(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierUv uv, System.Random random, KwPierGaps gaps)
        {
            float x = spec.Width * 0.5f - kind.Curb.x * 0.5f, y = kind.Curb.y * 0.5f;
            foreach (Vector2 run in gaps.Runs(KwPierGaps.Left, spec.Length))
                Along(mesh, kind, uv, random, new Vector2(-x, y), kind.Curb, run);
            foreach (Vector2 run in gaps.Runs(KwPierGaps.Right, spec.Length))
                Along(mesh, kind, uv, random, new Vector2(x, y), kind.Curb, run);
        }

        /// <summary>How far each side of the centre line the pilings stand (under the deck's edges).</summary>
        public static float PostX(KwPierSpec spec, KwPierKind kind) => spec.Width * 0.5f - kind.PostIn;

        /// <summary>How far along the section a station's pilings stand (kept within its ends).</summary>
        public static float PostZ(KwPierSpec spec, KwPierKind kind, int station) =>
            Mathf.Clamp(spec.StationZ(station), kind.Post * 0.5f, spec.Length - kind.Post * 0.5f);
    }
}
