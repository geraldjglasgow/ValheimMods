using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// The ground scrape's shockwave, what hits (the scrape itself does not): while the edge drags through the floor
    /// (the scrape clip's window) a row starts every <see cref="Every"/> seconds from the floor under the edge, running
    /// straight out from the Executioner at <see cref="Speed"/> to <see cref="Reach"/>; a foe the front of any row passes
    /// within <see cref="Width"/> of takes the shockwave's blunt damage once per scrape. Decided on the creature's owner
    /// only; every peer draws the same rows as slabs (<see cref="HeadsmanRocks"/>) from its own clip time.
    /// </summary>
    public sealed class HeadsmanWave
    {
        public const float Speed = 8f, Reach = 3.5f, Every = 0.066f, Width = 0.8f;
        private static readonly int Floors = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");

        private readonly Character boss;
        private readonly List<(Vector3 from, Vector3 outward, float start)> rows = new List<(Vector3, Vector3, float)>();
        private readonly HashSet<Character> struck = new HashSet<Character>();
        private readonly List<Character> near = new List<Character>();
        private float lastRow = -1f;

        public HeadsmanWave(Character boss) => this.boss = boss;

        /// <summary>The floor under a point (the crypt's, or the ground's), or the point itself when there is none close.</summary>
        public static Vector3 Floor(Vector3 point) =>
            Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 3f, Floors) ? hit.point : point;

        /// <summary>A row's start: the floor under the edge, and straight out from the creature.</summary>
        public static (Vector3 from, Vector3 outward) RowAt(Vector3 edge, Vector3 centre)
        {
            Vector3 outward = Vector3.ProjectOnPlane(edge - centre, Vector3.up);
            return (Floor(edge), outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.forward);
        }

        /// <summary>OWNER, each frame of the scrape (`time` seconds into its clip); `edge` the blade's edge in the world.</summary>
        public void Step(HeadsmanMove? move, float time, Vector3 edge)
        {
            float now = Time.time;
            if (move == HeadsmanMoves.Scrape && time >= move.Scrape.x && time < move.Scrape.y && now - lastRow >= Every)
            {
                var (from, outward) = RowAt(edge, boss.transform.position);
                rows.Add((from, outward, now));
                lastRow = now;
            }
            rows.RemoveAll(row => (now - row.start) * Speed > Reach);
            foreach (var (from, outward, start) in rows)
            {
                Strike(from + outward * ((now - start) * Speed), outward);
            }
            if (rows.Count == 0 && move != HeadsmanMoves.Scrape)
            {
                struck.Clear();
            }
        }

        private void Strike(Vector3 front, Vector3 outward)
        {
            near.Clear();
            Character.GetCharactersInRange(front, Width, near);
            foreach (Character foe in near)
            {
                if (foe == boss || struck.Contains(foe) || !BaseAI.IsEnemy(boss, foe))
                {
                    continue;
                }
                struck.Add(foe);
                var hit = new HitData { m_point = foe.GetCenterPoint(), m_dir = outward, m_pushForce = 45f, m_staggerMultiplier = 1.5f };
                hit.m_damage.m_blunt = HeadsmanSettings.WaveDamage;
                hit.SetAttacker(boss);
                foe.Damage(hit);
            }
        }
    }
}
