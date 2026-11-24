using BundlePrefabs;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The look of one burst of Moder's Breath (<see cref="BreathCast"/>): a copy of Moder's own. The Dragon's six attack
    /// items are <c>dragon_taunt</c>, <c>dragon_bite</c>, <c>dragon_claw_left</c>, <c>dragon_claw_right</c>,
    /// <c>dragon_spit_shotgun</c> and <c>dragon_coldbreath</c>; the breath (<c>attack_breath</c>, a melee sweep from the jaw:
    /// 30 m, 10 degrees, rays 2 m wide, through walls, frost 200, chop 200, pickaxe 200) has no projectile: its look is its
    /// trail start effect, <c>vfx_dragon_coldbreath</c>, with the sounds <c>sfx_dragon_coldbreath_trailon</c> and
    /// <c>_start</c>, all three networked objects, read live 2026-11-03. The breath is a networked object (a
    /// <c>ZSyncTransform</c>, a 10 s <c>TimedDestruction</c>) of three particle cones along +Z (clouds, pixel flakes, spray
    /// with a splat), each 1 s of 100 to 400 a second at 5 to 40 m/s, and a point light 8 m out. The copy
    /// (<see cref="Prefab"/>, registered in the scene on every peer, no game art shipped) is shrunk to a caster's cone: a
    /// fifth of the speed (about 7 m instead of 30), wider by half (about 30 degrees), smaller, a quarter second of half the
    /// particles (six bursts make one breath), the light nearer and smaller, gone after <see cref="Seconds"/> s; no camera
    /// shake (none found, any stripped as <c>WandRoots</c> does).
    /// </summary>
    public static class BreathLook
    {
        public const string Prefab = "EE_SpellBreath";
        public const string GameBreath = "vfx_dragon_coldbreath";
        public const string GameSound = "sfx_dragon_coldbreath_trailon";

        private const float Speed = 0.2f;
        private const float Wider = 1.5f;
        private const float Size = 0.4f;
        private const float Emitting = 0.25f;
        private const float Rate = 0.5f;
        private const float Seconds = 4.5f;
        private const float LightOut = 2.5f;
        private const float LightRange = 0.4f;

        public static GameObject Breath { get; private set; }

        public static void Build(GameObject game)
        {
            GameObject breath = PrefabBench.Copy(game, Prefab);
            foreach (CamShaker shaker in breath.GetComponentsInChildren<CamShaker>(true))
                Object.DestroyImmediate(shaker);
            foreach (ParticleSystem system in breath.GetComponentsInChildren<ParticleSystem>(true))
                Shrink(system);
            foreach (Light light in breath.GetComponentsInChildren<Light>(true))
                Nearer(light);
            TimedDestruction gone = breath.GetComponent<TimedDestruction>();
            if (gone != null)
                gone.m_timeout = Seconds;
            Breath = breath;
        }

        private static void Shrink(ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            main.duration = Emitting;
            main.startSpeed = Scaled(main.startSpeed, Speed);
            main.startSize = Scaled(main.startSize, Size);
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = Scaled(emission.rateOverTime, Rate);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.angle *= Wider;
            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            if (limit.enabled)
                limit.limitMultiplier *= Speed;
        }

        private static void Nearer(Light light)
        {
            Vector3 at = light.transform.localPosition;
            light.transform.localPosition = new Vector3(at.x, at.y * LightRange, LightOut);
            light.range *= LightRange;
        }

        /// <summary>A particle curve times <paramref name="by"/>, whatever its mode.</summary>
        private static ParticleSystem.MinMaxCurve Scaled(ParticleSystem.MinMaxCurve curve, float by)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    curve.constant *= by;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    curve.constantMin *= by;
                    curve.constantMax *= by;
                    break;
                default:
                    curve.curveMultiplier *= by;
                    break;
            }
            return curve;
        }
    }
}
