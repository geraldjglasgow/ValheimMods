using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>Finds the game's portal effect to copy, and copies it locally. Read from the game's assets
    /// (2026-10-04): <c>portal_wood</c> (Piece, ZNetView, WearNTear, TeleportWorld) has a child
    /// <c>_target_found_red</c> with an <see cref="EffectFade"/>, which is <see cref="TeleportWorld.m_target_found"/>;
    /// under it a point light (orange, range 10, LightLod), an AudioSource "SFX" (loop, 0.8; not copied) and a particle system with
    /// trails holding three more: "Black_suck" (dark smoke on a 1.3 m circle edge, drawn inwards and round),
    /// "blue flames" (stretched, orange in fact, <c>portal_flame</c> material) and "suck particles" (sparks with
    /// <see cref="VortexParticles"/>). The stone portal has the same plus ground fog and runes. Found by component,
    /// not by those names: the portal prefab by name from <see cref="ZNetScene"/>, then its
    /// <see cref="TeleportWorld.m_target_found"/>, then any <see cref="EffectFade"/> with particles under it.</summary>
    internal static class SeaGateSurfaceTemplate
    {
        private static readonly string[] PortalPrefabs = { "portal_wood", "portal_stone", "portal" };

        private static GameObject source;
        private static bool searched;

        /// <summary>The effect object inside a portal prefab (never instantiated itself); null when none was found,
        /// and then gates show the plain sheet. Searched once per world.</summary>
        internal static GameObject Source
        {
            get
            {
                if (!searched && ZNetScene.instance != null)
                {
                    searched = true;
                    source = Find();
                }
                return source;
            }
        }

        /// <summary>Stops using the effect for the rest of this world, after a copy failed.</summary>
        internal static void Disable()
        {
            source = null;
            searched = true;
        }

        internal static void Forget()
        {
            source = null;
            searched = false;
        }

        /// <summary>A local copy of <paramref name="effect"/> under <paramref name="parent"/>, which must be inactive so
        /// nothing in the copy wakes before it is stripped: no ZNetView (so no ZDO, nothing networked), no fade, no
        /// timed destruction, no sound. <see cref="ZNetView.m_forceDisableInit"/> is set as well, for a view that wakes
        /// anyway.</summary>
        internal static GameObject CopyLocal(GameObject effect, Transform parent)
        {
            bool before = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            GameObject copy;
            try
            {
                copy = Object.Instantiate(effect, parent, false);
            }
            finally
            {
                ZNetView.m_forceDisableInit = before;
            }
            Strip<ZSyncTransform>(copy);
            Strip<ZNetView>(copy);
            Strip<TimedDestruction>(copy);
            Strip<EffectFade>(copy);
            Strip<ZSFX>(copy);
            StripSound(copy);
            return copy;
        }

        /// <summary>The portal's hum would play at full volume from the first frame with no fade or dimming; the
        /// project does not reference the audio module, so it is found by type name.</summary>
        private static void StripSound(GameObject copy)
        {
            foreach (Behaviour behaviour in copy.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Name == "AudioSource")
                    Object.DestroyImmediate(behaviour);
            }
        }

        private static void Strip<T>(GameObject copy) where T : Component
        {
            foreach (T component in copy.GetComponentsInChildren<T>(true))
                Object.DestroyImmediate(component);
        }

        private static GameObject Find()
        {
            foreach (string name in PortalPrefabs)
            {
                GameObject effect = EffectOf(ZNetScene.instance.GetPrefab(name));
                if (effect != null)
                {
                    Plugin.Log.LogDebug($"Sea gate surface: using the portal effect of {name}.");
                    return effect;
                }
            }
            Plugin.Log.LogWarning("Sea gate surface: the game's portal effect was not found; gates show a plain surface.");
            return null;
        }

        private static GameObject EffectOf(GameObject prefab)
        {
            if (prefab == null)
                return null;
            TeleportWorld portal = prefab.GetComponent<TeleportWorld>();
            if (portal != null && portal.m_target_found != null && HasParticles(portal.m_target_found.gameObject))
                return portal.m_target_found.gameObject;
            EffectFade fade = prefab.GetComponentInChildren<EffectFade>(true);
            return fade != null && HasParticles(fade.gameObject) ? fade.gameObject : null;
        }

        private static bool HasParticles(GameObject effect) => effect.GetComponentInChildren<ParticleSystem>(true) != null;
    }
}
