using System.Collections.Generic;
using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>The game's own portal swirl on a gate: a local copy of the effect the wooden portal shows when it has a
    /// target (<see cref="TeleportWorld.m_target_found"/>: dark smoke drawn inwards, a ring of flames, sparks pulled to
    /// the centre, trails and an orange light). The copy is made under the surface's inactive root, so nothing in it
    /// wakes before it is stripped and refitted: its <see cref="EffectFade"/> (which would switch the emission off),
    /// its hum and anything networked are removed, every particle system is moved to the surface's centre and refitted
    /// to the gate (<see cref="SeaGateSurfaceFit"/>), the light is moved there too and widened. The copy shares the
    /// prefab's materials; it never instantiates one.</summary>
    internal sealed class SeaGateSurfaceEffect
    {
        private const float MinLightRange = 10f;
        private const float MaxLightRange = 25f;
        private const float LightLodDistance = 80f;

        private readonly List<SeaGateSurfaceParticles> particles = new List<SeaGateSurfaceParticles>();
        private Light light;
        private float lightIntensity;
        private float applied = -1f;

        private SeaGateSurfaceEffect()
        {
        }

        /// <summary>The effect fitted to <paramref name="frame"/> under <paramref name="root"/> (which must be
        /// inactive); null when the game's portal effect was not found or could not be copied.</summary>
        internal static SeaGateSurfaceEffect Build(Transform root, SeaGateSurfaceFrame frame)
        {
            GameObject source = SeaGateSurfaceTemplate.Source;
            if (source == null)
                return null;
            GameObject copy = SeaGateSurfaceTemplate.CopyLocal(source, root);
            try
            {
                SeaGateSurfaceEffect effect = new SeaGateSurfaceEffect();
                effect.Adopt(copy, root, frame);
                return effect;
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        /// <summary>0 to 1: emission and light. Writes only when the value changed, so a surface that is not
        /// fading costs nothing here.</summary>
        internal void SetIntensity(float intensity)
        {
            if (intensity == applied)
                return;
            applied = intensity;
            foreach (SeaGateSurfaceParticles system in particles)
                system.SetIntensity(intensity);
            if (light != null)
                light.intensity = lightIntensity * intensity;
        }

        /// <summary>Takes the copy's particle systems and light out to <paramref name="root"/>; whatever is left in the
        /// copy (empty transforms) is destroyed with it.</summary>
        private void Adopt(GameObject copy, Transform root, SeaGateSurfaceFrame frame)
        {
            Light found = copy.GetComponentInChildren<Light>(true);  // before the systems move, in case it sits under one
            foreach (ParticleSystem system in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                Place(system.transform, root);
                CentreVortex(system);
                particles.Add(SeaGateSurfaceParticles.Fit(system, frame.Width, frame.Height));
            }
            AdoptLight(found, root, frame);
        }

        private void AdoptLight(Light source, Transform root, SeaGateSurfaceFrame frame)
        {
            if (source == null)
                return;
            Place(source.transform, root);
            source.range = Mathf.Clamp(frame.Width, MinLightRange, MaxLightRange);  // before LightLod wakes and reads it
            source.shadows = LightShadows.None;
            LightLod lod = source.GetComponent<LightLod>();
            if (lod != null)
                lod.m_lightDistance = Mathf.Max(lod.m_lightDistance, LightLodDistance);
            lightIntensity = source.intensity;
            source.intensity = 0f;
            light = source;
        }

        /// <summary>The sparks' vortex pulls towards its own position plus a world offset made for the small portal's
        /// depth; at the gate's centre the offset only pulls them off the surface.</summary>
        private static void CentreVortex(ParticleSystem system)
        {
            VortexParticles vortex = system.GetComponent<VortexParticles>();
            if (vortex != null)
                vortex.centerOffset = Vector3.zero;
        }

        private static void Place(Transform part, Transform root)
        {
            part.SetParent(root, false);
            part.localPosition = Vector3.zero;
            part.localRotation = Quaternion.identity;
            part.localScale = Vector3.one;
            part.gameObject.SetActive(true);
        }
    }
}
