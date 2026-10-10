using System.Linq;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// Particles in the booth's pictures: every system of a copy is simulated to the moment in its first two seconds when
    /// the most particles are alive (a burst early, a loop once it has filled) and paused there, in the copy's own space
    /// so it turns with the model. An effect without meshes is framed by its particles.
    /// </summary>
    internal static class BoothParticles
    {
        private const float Step = 0.1f;
        private const float Longest = 2f;

        internal static void Hold(GameObject copy)
        {
            ParticleSystem[] systems = copy.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem system in systems)
            {
                ParticleSystem.MainModule main = system.main;
                if (main.simulationSpace == ParticleSystemSimulationSpace.World) main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
            ParticleSystem[] outer = systems.Where(Outermost).ToArray();
            if (outer.Length == 0) return;
            float busiest = Busiest(systems, outer);
            foreach (ParticleSystem system in outer) system.Simulate(busiest, true, true);
        }

        // Stepped through from the start, counting the particles alive after each step.
        private static float Busiest(ParticleSystem[] systems, ParticleSystem[] outer)
        {
            (float best, int most) = (Step, -1);
            foreach (ParticleSystem system in outer) system.Simulate(0f, true, true);
            for (int step = 1; step * Step <= Longest + 0.001f; step++)
            {
                foreach (ParticleSystem system in outer) system.Simulate(Step, true, false);
                int alive = systems.Sum(system => system.particleCount);
                if (alive > most) (best, most) = (step * Step, alive);
            }
            return best;
        }

        internal static bool MeshesDrawn(GameObject copy) =>
            copy.GetComponentsInChildren<Renderer>().Any(r => r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer));

        /// <summary>The box round the particles alive now, or null when there are none.</summary>
        internal static Bounds? Bounds(GameObject copy)
        {
            Bounds? box = null;
            foreach (ParticleSystem system in copy.GetComponentsInChildren<ParticleSystem>())
            {
                var particles = new ParticleSystem.Particle[system.particleCount];
                int count = system.GetParticles(particles);
                for (int i = 0; i < count; i++) box = Grow(box, new Bounds(system.transform.TransformPoint(particles[i].position), particles[i].GetCurrentSize3D(system)));
            }
            return box;
        }

        // A system no other system holds: simulating it runs its children with it.
        private static bool Outermost(ParticleSystem system) =>
            !system.transform.parent || !system.transform.parent.GetComponentInParent<ParticleSystem>();

        private static Bounds Grow(Bounds? box, Bounds one)
        {
            if (!(box is Bounds grown)) return one;
            grown.Encapsulate(one);
            return grown;
        }
    }
}
