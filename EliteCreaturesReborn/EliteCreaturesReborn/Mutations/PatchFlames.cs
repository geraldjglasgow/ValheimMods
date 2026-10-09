using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Visuals;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The flames licking up from Flamebound's burning patches on this machine: one local copy of the game's own Burning
    /// look (the flames it draws on a burning body, <see cref="CosmeticClone"/>, never networked), its particle systems
    /// fed by hand in world space, each flame rising from a random point inside a live patch. One copy draws every
    /// creature's fire, so a long trail costs particles, never objects, and a patch stops burning the moment it is gone.
    /// Thinned by the player's effect density, and none at all at 0, as every effect of the mod; the ember patch under
    /// them still warns. Never on a dedicated server.
    /// </summary>
    internal sealed class PatchFlames
    {
        private const string Source = "vfx_Burning";
        private static readonly string[] Keywords = { "burning", "flame", "fire" };

        /// <summary>Flames a second rising from each patch at full density.</summary>
        private const float PerPatchPerSecond = 14f;

        /// <summary>How far from a patch's middle a flame may rise, as a share of its reach.</summary>
        private const float Spread = 0.75f;

        private readonly ParticleSystem[] _systems;
        private float _owed;

        private PatchFlames(ParticleSystem[] systems) => _systems = systems;

        /// <summary>The flames, copied under <paramref name="parent"/>; null when effects are off or the game has none.</summary>
        public static PatchFlames? Build(Transform parent)
        {
            GameObject? prefab = EffectResolver.Resolve(Source, Keywords, "Flamebound flames");
            GameObject? copy = CosmeticClone.Spawn(prefab, parent, parent.position, endless: true);
            if (copy == null)
            {
                return null;
            }
            Quiet(copy);
            ParticleSystem[] systems = copy.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in systems)
            {
                Settle(system);
            }
            return systems.Length > 0 ? new PatchFlames(systems) : null;
        }

        // Its light and crackle stay where the copy sits, far from any fire, so both go: the flames alone are drawn.
        private static void Quiet(GameObject copy)
        {
            foreach (Behaviour part in copy.GetComponentsInChildren<Behaviour>(true))
            {
                if (part is Light || part is AudioSource || part is ZSFX || part is LightLod || part is LightFlicker)
                {
                    part.enabled = false;
                }
            }
        }

        // Emptied, emitting nothing of its own, simulated in the world even out of sight, then fed by Tick alone.
        private static void Settle(ParticleSystem system)
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.stopAction = ParticleSystemStopAction.None; // emptied above, it must not remove or hide itself
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.maxParticles = Mathf.Max(main.maxParticles, 2000);
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false; // the game's shape follows a body; here each flame is placed by hand
            system.Play(false);
        }

        /// <summary>Lights the flames owed over the last <paramref name="elapsed"/> seconds, spread over the patches.</summary>
        public void Tick(List<GroundPatch> patches, float elapsed)
        {
            if (patches.Count == 0)
            {
                _owed = 0f;
                return;
            }
            _owed += patches.Count * PerPatchPerSecond * elapsed * Density();
            for (; _owed >= 1f; _owed -= 1f)
            {
                GroundPatch patch = patches[Random.Range(0, patches.Count)];
                Emit(_systems[Random.Range(0, _systems.Length)], Within(patch));
            }
        }

        private static void Emit(ParticleSystem system, Vector3 at)
        {
            if (system == null)
            {
                return;
            }
            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
            {
                position = at,
                velocity = Vector3.up * Random.Range(0.3f, 0.9f),
            };
            system.Emit(emit, 1);
        }

        private static Vector3 Within(GroundPatch patch)
        {
            Vector2 offset = Random.insideUnitCircle * Mathf.Sqrt(patch.RadiusSq) * Spread;
            return patch.Point + new Vector3(offset.x, 0.05f, offset.y);
        }

        private static float Density() => Mathf.Clamp01(Configuration.EffectDensity.Value);
    }
}
