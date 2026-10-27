using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One turning shaft of a steering gear (<see cref="KwGearParts"/>): where it starts, the way it runs from the wheel toward the rudder, its meshes by look.</summary>
    public sealed class KwGearRun
    {
        public KwGearRun(Vector3 from, Vector3 way, Dictionary<KwLook, MeshBuilder> parts) => (From, Way, Parts) = (from, way, parts);

        /// <summary>Where it starts, on its axis: the point it turns about, its meshes' origin.</summary>
        public Vector3 From { get; }

        /// <summary>The way it runs, from the wheel toward the rudder: it turns about this, right-handed, by <see cref="KwGearTrain.Spin"/>.</summary>
        public Vector3 Way { get; }

        public Dictionary<KwLook, MeshBuilder> Parts { get; }
    }

    /// <summary>
    /// A steering gear's parts made from its text (<see cref="KwRudderGear"/>), the same on every screen and in offline
    /// checks, in the rudder's kit frame: each shaft one turning part (<see cref="KwGearRun"/>: its stretch, the bevel wheels
    /// at its ends, or a coupling's flange where two stretches meet nearly in line, or the pinion at the rudder's end),
    /// and the parts that stay put (<see cref="Fixed"/>: the hangers, the brackets on a cabin's wall, the bushes where a
    /// climbing stretch goes up through a floor, and the bush under the wheel's deck or roof), in the rudder's
    /// wood and fittings. The shafts in order from the wheel: the short one down from inside its stand, then from corner to
    /// corner back to the pinion.
    /// </summary>
    public static class KwGearParts
    {
        // How far a bend must turn to take bevel wheels (less: a coupling); how far into the wheel's stand the short shaft rises.
        private const float Coupling = 20f, Stand = 0.3f;

        public static List<KwGearRun> Runs(KwRudderGear gear, KwRudderTier tier)
        {
            List<(Vector3 A, Vector3 B)> legs = Legs(gear);
            List<KwGearRun> runs = new List<KwGearRun>(legs.Count);
            for (int i = 0; i < legs.Count; i++)
                runs.Add(Run(legs, i, tier));
            return runs;
        }

        /// <summary>The hangers, brackets and bushes, by look.</summary>
        public static Dictionary<KwLook, MeshBuilder> Fixed(KwRudderGear gear, KwRudderTier tier)
        {
            Dictionary<KwLook, MeshBuilder> parts = Looks(tier, Vector3.zero);
            Vector3[] c = gear.Corners;
            foreach (Vector4 h in gear.Hangers)
            {
                Vector3 at = new Vector3(h.x, h.y, h.z);
                KwGearFittings.Hanger(parts[tier.Wood], parts[tier.Fitting], at, Way(c, at), h.w);
            }
            foreach (KwGearBracket b in gear.Brackets)
                KwGearFittings.Bracket(parts[tier.Wood], parts[tier.Fitting], b.At, new Vector3(b.Reach.x, b.Reach.y, 0f));
            Floors(parts[tier.Fitting], c);
            KwGearFittings.Bush(parts[tier.Fitting], c[c.Length - 1], gear.Wheel.z);
            return parts;
        }

        // A bush under and a collar on each floor a stretch climbs up through: the floor's top is the shaft's way under it
        // over the climb's foot (a climb starts under a deck or a roof, as a run under one hangs).
        private static void Floors(MeshBuilder fitting, Vector3[] c)
        {
            for (int k = 0; k + 1 < c.Length; k++)
            {
                Vector3 run = c[k + 1] - c[k];
                if (run.z < 0.3f || new Vector2(run.x, run.y).magnitude > 0.01f)
                    continue;
                float top = c[k].z + KwGearTrain.Below;
                KwGearFittings.Bush(fitting, c[k], top);
                KwGearFittings.Collar(fitting, new Vector3(c[k].x, c[k].y, top));
            }
        }

        // The shafts' stretches in order from the wheel: down from its stand, then corner to corner to the pinion.
        private static List<(Vector3 A, Vector3 B)> Legs(KwRudderGear gear)
        {
            Vector3[] c = gear.Corners;
            Vector3 under = c[c.Length - 1];
            List<(Vector3 A, Vector3 B)> legs = new List<(Vector3 A, Vector3 B)> { (new Vector3(under.x, under.y, gear.Wheel.z + Stand), under) };
            for (int k = c.Length - 1; k > 0; k--)
                legs.Add((c[k], c[k - 1]));
            return legs;
        }

        // One shaft: its stretch between what it carries at either end.
        private static KwGearRun Run(List<(Vector3 A, Vector3 B)> legs, int i, KwRudderTier tier)
        {
            (Vector3 a, Vector3 b) = legs[i];
            Vector3 way = (b - a).normalized;
            Dictionary<KwLook, MeshBuilder> parts = Looks(tier, a);
            MeshBuilder wood = parts[tier.Wood], fitting = parts[tier.Fitting];
            float start = i == 0 ? 0f : End(wood, fitting, a, way, -Way(legs, i - 1), false);
            float end = i == legs.Count - 1 ? Pinion(wood, fitting, b, way) : End(wood, fitting, b, -way, Way(legs, i + 1), true);
            KwGearFittings.Shaft(wood, fitting, a + way * start, b - way * end);
            return new KwGearRun(a, way, parts);
        }

        // A shaft's end at a corner: a bevel wheel (on the shaft going out of the corner a way, meeting the other toward
        // another), or a coupling's flange when the two run nearly in line. How far from the corner the shaft starts.
        private static float End(MeshBuilder wood, MeshBuilder fitting, Vector3 corner, Vector3 axis, Vector3 toward, bool lead)
        {
            float bend = 180f - Vector3.Angle(axis, toward);
            if (bend < Coupling)
            {
                KwGearShapes.Flange(wood, fitting, corner, axis);
                return 0.004f;
            }
            KwGearShapes.Bevel(wood, fitting, corner, axis, toward, lead);
            float half = Vector3.Angle(axis, toward) * Mathf.Deg2Rad / 2f;
            return KwGearTrain.Bevel / Mathf.Tan(half) - 0.02f * Mathf.Cos(half);
        }

        // The pinion at the shaft's after end, the shaft running on through it to a nut.
        private static float Pinion(MeshBuilder wood, MeshBuilder fitting, Vector3 at, Vector3 way)
        {
            KwGearShapes.Pinion(wood, fitting, at, way);
            return -0.06f;
        }

        private static Vector3 Way(List<(Vector3 A, Vector3 B)> legs, int i) => (legs[i].B - legs[i].A).normalized;

        // The way the shaft runs at a point on it: the corners' stretch nearest it.
        private static Vector3 Way(Vector3[] c, Vector3 at)
        {
            int best = 0;
            float bestSq = float.MaxValue;
            for (int k = 0; k + 1 < c.Length; k++)
            {
                Vector3 run = c[k + 1] - c[k];
                float t = Mathf.Clamp01(Vector3.Dot(at - c[k], run) / Mathf.Max(run.sqrMagnitude, 1e-6f));
                float sq = (c[k] + run * t - at).sqrMagnitude;
                if (sq < bestSq)
                    (best, bestSq) = (k, sq);
            }
            return (c[best + 1] - c[best]).normalized;
        }

        private static Dictionary<KwLook, MeshBuilder> Looks(KwRudderTier tier, Vector3 pivot) => new Dictionary<KwLook, MeshBuilder>
        {
            [tier.Wood] = new MeshBuilder(pivot),
            [tier.Fitting] = new MeshBuilder(pivot),
        };
    }
}
