using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a cannon's ball would go if the captain fired it now, for the shot arcs (<see cref="KwShotArcs"/>): flown as the
    /// game flies the projectile the called shot launches (<see cref="KwShooter"/>, <see cref="KwShots"/>): from the muzzle
    /// along the barrel as drawn, at the shot's speed plus the ship's own motion at the muzzle, each physics step its
    /// gravity added and then the move, and each step's path tested for hits from half a step behind as the game's
    /// projectile tests it (on the layers it hits, passing through the ship and the cannon it was fired from, and through
    /// the characters it would spare: the captain, his friends); into the sea it goes on under as
    /// <see cref="KwShotWater"/> flies it, still striking what it meets there, until the water stops it. To keep it cheap
    /// the path is first tested <see cref="Span"/> steps at a time along their chord (the arc lies within millimetres of
    /// it, under the water within centimetres) and only a span with a hit is tested again step by step; the sea is looked
    /// for at every step (the game's own surface, waves and all, only within <see cref="KwBall.Waves"/> of it); it ends at
    /// the shot's life. The result: the path's points (every <see cref="Every"/> steps, above the water) and the strike, a
    /// target (a creature it would hurt, a ship, a piece), the ground (terrain, a rock, a tree), the sea, or nothing.
    /// </summary>
    public static class KwShotPath
    {
        /// <summary>Physics steps tested at once along their chord; steps between the path's points.</summary>
        public const int Span = 5, Every = 2;

        private static readonly RaycastHit[] hits = new RaycastHit[64];
        private static readonly KwBall ahead = new KwBall(Vector3.zero, Vector3.zero, 0f, 0.02f);
        private static int mask;

        /// <summary>The ball's flight from the cannon now, its points into `points`, and where it strikes.</summary>
        public static KwStrike Fly(KwArm arm, KwShot shot, Player captain, List<Vector3> points)
        {
            Rigidbody? carrier = arm.Carrier;
            Vector3 at = arm.Muzzle.position;
            Vector3 carried = carrier != null ? carrier.GetPointVelocity(at) : Vector3.zero;
            KwBall ball = new KwBall(at, arm.Muzzle.forward * shot.Speed + carried, shot.Gravity, Time.fixedDeltaTime);
            KwShotPass pass = new KwShotPass(carrier, arm.transform, captain);
            return Fly(ball, shot.Life, pass, points);
        }

        /// <summary>A ball's flight for so many seconds, its points into `points`, and where it strikes.</summary>
        public static KwStrike Fly(KwBall ball, float life, KwShotPass pass, List<Vector3> points)
        {
            points.Clear();
            points.Add(ball.At);
            Vector3 start = ball.At;
            int steps = Mathf.CeilToInt(life / ball.Dt);
            for (int done = 0; done < steps; done += Span)
            {
                KwStrike? strike = Stretch(ball, Mathf.Min(Span, steps - done), pass, points);
                if (strike is KwStrike found)
                    return found.From(start);
            }
            KwStrike end = ball.Under ? new KwStrike(ball.Entered, Vector3.up, KwStrikeKind.Sea) : new KwStrike(ball.At, Vector3.up, KwStrikeKind.None);
            return end.From(start);
        }

        // A span of steps: its chord looked along first (a copy of the ball flown ahead over it), each step then tested only
        // when something is on it; the sea looked for at every step.
        private static KwStrike? Stretch(KwBall ball, int count, KwShotPass pass, List<Vector3> points)
        {
            bool clear = !Chord(ball, count, pass);
            for (int i = 0; i < count; i++)
            {
                ball.Step();
                KwStrike? strike = clear ? null : Hit(ball, pass);
                if (strike == null && ball.Water() && ball.Done)
                    strike = new KwStrike(ball.Entered, Vector3.up, KwStrikeKind.Sea);
                if (strike != null)
                    return strike;
                if (!ball.Under && ++ball.Count % Every == 0)
                    points.Add(ball.At);
            }
            return null;
        }

        // One step's hit test as the game's projectile makes it: from half a step behind, a step and a half long.
        private static KwStrike? Hit(KwBall ball, KwShotPass pass)
        {
            Vector3 step = ball.At - ball.Last;
            return Ray(ball.Last - step * 0.5f, step, step.magnitude * 1.5f, pass, out KwStrike hit) ? hit : (KwStrike?)null;
        }

        // Whether anything the ball could strike lies on the chord of the coming span, from half its first step behind it to
        // its end (a copy of the ball flown over the span: in the air its path lies within millimetres of it, under the water
        // within a couple of centimetres).
        private static bool Chord(KwBall ball, int count, KwShotPass pass)
        {
            ahead.Copy(ball);
            Vector3 first = ahead.Fly() - ball.At, from = ball.At - first * 0.5f;
            for (int i = 1; i < count && !ahead.Done; i++)
                ahead.Fly();
            Vector3 run = ahead.At - from;
            return Ray(from, run, run.magnitude, pass, out _);
        }

        // The nearest thing along a ray the ball would strike (as the game's projectile sorts its hits and takes the first it does not pass).
        private static bool Ray(Vector3 from, Vector3 way, float length, KwShotPass pass, out KwStrike strike)
        {
            mask = mask != 0 ? mask : LayerMask.GetMask(Layers);
            int count = length > 1e-4f ? Physics.RaycastNonAlloc(from, way.normalized, hits, length, mask) : 0;
            strike = default;
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                KwStrikeKind kind = hit.distance < nearest ? pass.Kind(hit.collider) : KwStrikeKind.None;
                if (kind == KwStrikeKind.None)
                    continue;
                nearest = hit.distance;
                strike = new KwStrike(hit.distance == 0f ? from : hit.point, hit.normal, kind);
            }
            return nearest < float.MaxValue;
        }

        /// <summary>The layers the game's projectile tests (its <c>s_rayMaskSolids</c>).</summary>
        private static readonly string[] Layers =
            { "Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle" };
    }

    /// <summary>What a predicted ball strikes: something it would hurt (a creature, a ship, a piece), the ground (terrain, a rock, a tree), the sea, or nothing.</summary>
    public enum KwStrikeKind
    {
        None,
        Target,
        Ground,
        Sea,
    }

    /// <summary>Where a predicted ball strikes, the surface's normal there, what it strikes, and how far that is from the muzzle.</summary>
    public readonly struct KwStrike
    {
        public KwStrike(Vector3 point, Vector3 normal, KwStrikeKind kind, float range = 0f) => (Point, Normal, Kind, Range) = (point, normal, kind, range);

        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public KwStrikeKind Kind { get; }
        public float Range { get; }

        /// <summary>The same strike with its range from the muzzle.</summary>
        public KwStrike From(Vector3 muzzle) => new KwStrike(Point, Normal, Kind, Vector3.Distance(muzzle, Point));
    }
}
