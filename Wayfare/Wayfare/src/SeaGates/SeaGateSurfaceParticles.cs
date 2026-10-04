using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>One particle system of the game's portal swirl, refitted to a gate, with the emission it had once
    /// refitted so the surface's fade can scale it.</summary>
    internal sealed class SeaGateSurfaceParticles
    {
        private readonly ParticleSystem system;
        private readonly ParticleSystem.MinMaxCurve fullRate;

        private SeaGateSurfaceParticles(ParticleSystem system, ParticleSystem.MinMaxCurve fullRate)
        {
            this.system = system;
            this.fullRate = fullRate;
        }

        /// <summary>Scales the emission: 0 stops new particles (the live ones finish their short lives), 1 is full.</summary>
        internal void SetIntensity(float intensity)
        {
            if (system == null)
                return;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = SeaGateSurfaceFit.Scaled(fullRate, intensity);
        }

        /// <summary>Refits <paramref name="system"/> (already placed at the surface's centre, in the gate's frame) to a
        /// <paramref name="width"/> by <paramref name="height"/> rectangle, emitting nothing until the first
        /// <see cref="SetIntensity"/>.</summary>
        internal static SeaGateSurfaceParticles Fit(ParticleSystem system, float width, float height)
        {
            float rateScale = SeaGateSurfaceFit.Apply(system, width, height);
            ParticleSystem.MinMaxCurve rate = SeaGateSurfaceFit.Scaled(system.emission.rateOverTime, rateScale);
            SeaGateSurfaceParticles fitted = new SeaGateSurfaceParticles(system, rate);
            fitted.SetIntensity(0f);
            return fitted;
        }
    }
}
