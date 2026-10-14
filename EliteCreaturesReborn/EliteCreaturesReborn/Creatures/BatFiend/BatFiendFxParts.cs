using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Util;
using LocalEffects;
using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// How <see cref="BatFiendFx"/> makes and fits its copies of the game's own effects. Each is found by name in
    /// ZNetScene (a name the game lacks is warned about once and then simply not drawn) and copied through the
    /// LocalEffects library at the player's `Effect density`: seen on this machine only, never networked, never a damage
    /// source, nothing at all on a dedicated server. A copy is turned to face the attack and sized part by part, its
    /// camera shakes and sounds silenced (the Bat-fiend's voice is its own), then fitted to the Bat-fiend: parts switched
    /// off, recoloured to Hel's palette, emitting for a shorter time, its cone widened, its lights recoloured or put out.
    /// </summary>
    internal static class BatFiendFxParts
    {
        private static readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>();

        /// <summary>The player's `Effect density` (Display section): 0 draws nothing, 1 everything.</summary>
        public static float Density => Mathf.Clamp01(Configuration.EffectDensity.Value);

        /// <summary>The game's effect prefab of that name, or null (warned once).</summary>
        public static GameObject? Prefab(string name)
        {
            if (prefabs.TryGetValue(name, out GameObject? known) && known != null)
            {
                return known;
            }
            ZNetScene scene = ZNetScene.instance;
            GameObject? found = scene != null ? scene.GetPrefab(name) : null;
            if (found == null && scene != null && !prefabs.ContainsKey(name))
            {
                Log.Warn($"Bat-fiend: the game has no effect {name}; that part of its attacks is not drawn.");
            }
            prefabs[name] = found;
            return found;
        }

        /// <summary>
        /// A copy under <paramref name="maker"/> (it moves with it and goes with it) at a place, facing a way, at
        /// <paramref name="scale"/> times the game's size, silenced; null when the density is 0 or the effect is missing.
        /// </summary>
        public static GameObject? Held(string name, Transform maker, Vector3 at, Quaternion facing, float scale)
        {
            if (Machine.Headless)
            {
                return null;
            }
            GameObject? clone = LocalEffect.AttachScaled(Prefab(name), maker, at, endless: false, scale, Density);
            if (clone == null)
            {
                return null;
            }
            clone.transform.rotation = facing;
            Quiet(clone);
            return clone;
        }

        /// <summary><see cref="Held"/>, then let go: it stays where it was made while the Bat-fiend moves on.</summary>
        public static GameObject? Free(string name, Transform maker, Vector3 at, Quaternion facing, float scale)
        {
            GameObject? clone = Held(name, maker, at, facing, scale);
            if (clone != null)
            {
                clone.transform.SetParent(null, worldPositionStays: true);
            }
            return clone;
        }

        /// <summary>A one-shot burst that needs no turning or fitting, sized part by part; the library keeps and reuses it.</summary>
        public static void Burst(string name, Vector3 at, float scale)
        {
            if (!Machine.Headless)
            {
                // Radius 4 is the library's baseline: the copy's own transform is left at 1 and only `scale` sizes it.
                LocalEffect.FlashScaled(Prefab(name), at, radius: 4f, scale, Density);
            }
        }

        /// <summary>No camera shake and no sound: shakes start, and sounds play, only once the copy's first frame begins.</summary>
        private static void Quiet(GameObject clone)
        {
            foreach (CamShaker shaker in clone.GetComponentsInChildren<CamShaker>(true))
            {
                shaker.enabled = false;
            }
            foreach (ZSFX sound in clone.GetComponentsInChildren<ZSFX>(true))
            {
                sound.enabled = false;
            }
            foreach (AudioSource source in clone.GetComponentsInChildren<AudioSource>(true))
            {
                source.Stop();
                source.enabled = false;
            }
        }

        /// <summary>The named particle parts emit nothing (the root's own system too, by the prefab's name).</summary>
        public static void Off(GameObject clone, params string[] parts)
        {
            foreach (ParticleSystem system in Systems(clone, parts))
            {
                ParticleSystem.EmissionModule emission = system.emission;
                emission.enabled = false;
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>The named parts born in a colour picked between two (pass the same twice for one colour).</summary>
        public static void Tint(GameObject clone, string part, Color low, Color high)
        {
            foreach (ParticleSystem system in Systems(clone, part))
            {
                ParticleSystem.MainModule main = system.main;
                main.startColor = new ParticleSystem.MinMaxGradient(low, high);
            }
        }

        /// <summary>The named parts launched between two speeds, in metres a second.</summary>
        public static void Speed(GameObject clone, string part, float low, float high)
        {
            foreach (ParticleSystem system in Systems(clone, part))
            {
                ParticleSystem.MainModule main = system.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(low, high);
            }
        }

        /// <summary>Every cone-shaped emitter opened to <paramref name="degrees"/> either side of its axis.</summary>
        public static void Widen(GameObject clone, float degrees)
        {
            foreach (ParticleSystem system in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.ShapeModule shape = system.shape;
                if (shape.enabled && shape.shapeType == ParticleSystemShapeType.Cone)
                {
                    shape.angle = degrees;
                }
            }
        }

        /// <summary>Every ring- or circle-shaped emitter drawn over <paramref name="degrees"/> of its circle, from where its arc starts.</summary>
        public static void Arc(GameObject clone, float degrees)
        {
            foreach (ParticleSystem system in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.ShapeModule shape = system.shape;
                if (shape.enabled && (shape.shapeType == ParticleSystemShapeType.Donut || shape.shapeType == ParticleSystemShapeType.Circle))
                {
                    shape.arc = degrees;
                }
            }
        }

        /// <summary>
        /// Parts that stream over time stop after <paramref name="seconds"/> (a single beat, not the game's long breath):
        /// a system's length can only change while it is stopped, so all are stopped, shortened and started again.
        /// </summary>
        public static void Shorten(GameObject clone, float seconds)
        {
            ParticleSystem[] systems = clone.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in systems)
            {
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach (ParticleSystem system in systems)
            {
                ParticleSystem.MainModule main = system.main;
                if (!main.loop && main.duration > seconds && system.emission.enabled && system.emission.rateOverTimeMultiplier > 0f)
                {
                    main.duration = seconds;
                }
            }
            foreach (ParticleSystem system in systems)
            {
                system.Play(false);
            }
        }

        /// <summary>The named part moved back onto the copy's own origin (the game set it ahead of a bigger body).</summary>
        public static void Recentre(GameObject clone, string part)
        {
            foreach (Transform node in clone.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == part)
                {
                    node.localPosition = Vector3.zero;
                }
            }
        }

        /// <summary>Every light of the copy in this colour.</summary>
        public static void Glow(GameObject clone, Color colour)
        {
            foreach (Light light in clone.GetComponentsInChildren<Light>(true))
            {
                light.color = colour;
            }
        }

        /// <summary>Every light of the copy out, and the game's flicker that would light it again stopped.</summary>
        public static void Dark(GameObject clone)
        {
            foreach (LightFlicker flicker in clone.GetComponentsInChildren<LightFlicker>(true))
            {
                flicker.enabled = false;
            }
            foreach (Light light in clone.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }
        }

        private static IEnumerable<ParticleSystem> Systems(GameObject clone, params string[] parts)
        {
            foreach (ParticleSystem system in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                string name = system.transform == clone.transform ? system.name.Replace("(Clone)", "") : system.name;
                if (Array.IndexOf(parts, name) >= 0)
                {
                    yield return system;
                }
            }
        }
    }
}
