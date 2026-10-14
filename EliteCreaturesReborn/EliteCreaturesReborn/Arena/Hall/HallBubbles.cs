using LocalEffects;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Bubbles rising in Hel's river (features/arena.md section 1), clients only, on the arena: 28 local copies of the
    /// river bundle's one-shot <c>ecr_helarena_bubble</c> (<see cref="HallRiverFx"/>), one near each of the land's
    /// <c>mist_00</c>..<c>27</c> (the 16 round the plateau's foot and the 12 at r 200, which lie on open liquid; the 8 at
    /// r 330 are too far to see a 2 m cluster): 2 to 8 m off the point, turned at most 80 degrees from straight away
    /// from the arena so none lands on the plateau's foot, on the surface (y -8). Each plays again every 4 to 15 s at
    /// random, and only while the local player is in the arena's zone. The systems are gathered once; a frame walks 28
    /// timers and allocates nothing.
    /// </summary>
    internal sealed class HallBubbles : MonoBehaviour
    {
        private const int Spots = 28;
        private const float Surface = -8f;
        private const float MinGap = 4f;
        private const float MaxGap = 15f;
        private const float MinOff = 2f;
        private const float MaxOff = 8f;
        private const float Spread = 80f;

        private ParticleSystem[][] _systems = new ParticleSystem[0][];
        private float[] _next = new float[0];
        private int _count;

        private void Awake()
        {
            GameObject? bubble = HallRiverFx.Bubble;
            _systems = new ParticleSystem[Spots][];
            _next = new float[Spots];
            for (int k = 0; k < Spots && bubble != null; k++)
            {
                if (HallPoints.TryGet("mist_" + k.ToString("00"), out Transform at))
                {
                    Add(bubble, Spot(at.position));
                }
            }
            enabled = _count > 0;
        }

        private void Update()
        {
            if (!HallEnvironment.PlayerInZone)
            {
                return;
            }
            float now = Time.time;
            for (int i = 0; i < _count; i++)
            {
                if (now >= _next[i])
                {
                    Play(_systems[i]);
                    _next[i] = now + Random.Range(MinGap, MaxGap);
                }
            }
        }

        // A copy at the spot, still until its first turn comes.
        private void Add(GameObject bubble, Vector3 spot)
        {
            GameObject? copy = LocalEffect.Attach(bubble, transform, spot, endless: true);
            ParticleSystem[] systems = copy == null ? new ParticleSystem[0] : copy.GetComponentsInChildren<ParticleSystem>(true);
            if (systems.Length == 0)
            {
                return;
            }
            foreach (ParticleSystem system in systems)
            {
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            _systems[_count] = systems;
            _next[_count++] = Time.time + Random.Range(0f, MaxGap);
        }

        private static void Play(ParticleSystem[] systems)
        {
            foreach (ParticleSystem system in systems)
            {
                system.Play(false);
            }
        }

        private static Vector3 Spot(Vector3 anchor)
        {
            Vector3 away = anchor - ArenaHall.Origin;
            away.y = 0f;
            away = away.sqrMagnitude > 1f ? away.normalized : Vector3.forward;
            Vector3 spot = anchor + Quaternion.Euler(0f, Random.Range(-Spread, Spread), 0f) * away * Random.Range(MinOff, MaxOff);
            spot.y = ArenaHall.Origin.y + Surface;
            return spot;
        }
    }
}
