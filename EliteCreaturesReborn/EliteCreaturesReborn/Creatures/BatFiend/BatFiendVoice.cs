using System;
using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Bat-fiend's voice is the game Bat's, pitched down for a throat many times its size
    /// (<see cref="BatFiendNumbers.VoicePitch"/>): copies of the Bat's alert, idle, hurt, death and attack sounds, read from
    /// the Bat prefab's own effect lists (<see cref="SfxPrefabs.Variant"/>: the game's clips, mix and reach at a lower
    /// pitch, registered on every peer), put in place of the Wolf's sounds in the creature's effect lists, which the game
    /// plays and networks itself. Nothing of the game's audio is shipped; a sound the Bat lacks keeps the Wolf's.
    /// </summary>
    internal static class BatFiendVoice
    {
        /// <summary>The game Wraith's alert, a ghost's shriek: the screech's top layer, a little deeper than the Wraith's.</summary>
        private const string WraithShriek = "sfx_wraith_alerted";
        private const float WraithPitch = 0.8f;

        private static readonly Dictionary<string, GameObject> voices = new Dictionary<string, GameObject>();

        /// <summary>Every voice made, for registering on each scene wake.</summary>
        public static IEnumerable<GameObject> Prefabs => voices.Values;

        /// <summary>The voice for a key (alert, idle, hurt, death, attack), or null when the Bat had none.</summary>
        public static GameObject? Of(string key) => voices.TryGetValue(key, out GameObject voice) ? voice : null;

        /// <summary>Makes the voices once, from this scene's game Bat.</summary>
        public static void Build(ZNetScene scene, GameObject? bat)
        {
            if (voices.Count > 0)
            {
                return;
            }
            if (bat == null)
            {
                Log.Warn("Bat-fiend: the game has no Bat prefab; it keeps the Wolf's voice.");
                return;
            }
            MonsterAI? ai = bat.GetComponent<MonsterAI>();
            Character? body = bat.GetComponent<Character>();
            Make(scene, "alert", ai != null ? ai.m_alertedEffects : null);
            Make(scene, "idle", ai != null ? ai.m_idleSound : null);
            Make(scene, "hurt", body != null ? body.m_hitEffects : null);
            Make(scene, "death", body != null ? body.m_deathEffects : null);
            Make(scene, "attack", AttackSounds(bat));
            Make(scene, "scream_low", ai != null ? ai.m_alertedEffects : null, BatFiendNumbers.VoicePitch * 0.8f);
            Variant(scene, "scream_wraith", scene.GetPrefab(WraithShriek), WraithPitch);
        }

        /// <summary>The screech's scream, every layer played at once: the bat's, a deeper copy of it and a wraith's shriek.</summary>
        public static GameObject?[] Scream => new[] { Of("alert"), Of("scream_low"), Of("scream_wraith") };

        /// <summary>The Bat's attack item's start and trigger effects, as one list to search.</summary>
        private static EffectList? AttackSounds(GameObject bat)
        {
            GameObject? item = BatFiendAttacks.WolfAttack(bat);
            ItemDrop.ItemData.SharedData? shared = item != null ? item.GetComponent<ItemDrop>().m_itemData.m_shared : null;
            if (shared == null)
            {
                return null;
            }
            EffectList[] lists = { shared.m_attack.m_startEffect, shared.m_startEffect, shared.m_attack.m_triggerEffect, shared.m_triggerEffect };
            return new EffectList { m_effectPrefabs = lists.Where(l => l != null).SelectMany(l => l.m_effectPrefabs).ToArray() };
        }

        private static void Make(ZNetScene scene, string key, EffectList? list, float pitch = BatFiendNumbers.VoicePitch) =>
            Variant(scene, key, list?.m_effectPrefabs.Select(e => e.m_prefab).FirstOrDefault(IsSound), pitch);

        /// <summary>A copy of the game sound at <paramref name="pitch"/> times its own, as the voice for the key.</summary>
        private static void Variant(ZNetScene scene, string key, GameObject? sound, float pitch)
        {
            ZSFX? original = sound != null ? sound.GetComponentInChildren<ZSFX>(true) : null;
            if (sound == null || original == null)
            {
                return;
            }
            try
            {
                var settings = new SfxSettings
                {
                    MinPitch = original.m_minPitch * pitch, MaxPitch = original.m_maxPitch * pitch,
                    MinVolume = Loud(key), MaxVolume = Loud(key), Caption = "$enemy_ecr_batfiend",
                };
                voices[key] = SfxPrefabs.Variant(scene, sound.name, BatFiendPrefabs.Creature + "_sfx_" + key, settings);
            }
            catch (Exception e)
            {
                Log.Warn($"Bat-fiend: no {key} voice ({e.Message}); the Wolf's is kept.");
            }
        }

        /// <summary>The screech's layers play at full volume; every other voice keeps the game sound's own.</summary>
        private static float? Loud(string key) => key.StartsWith("scream") ? 1f : (float?)null;

        private static bool IsSound(GameObject? prefab) => prefab != null && prefab.GetComponentInChildren<ZSFX>(true) != null;

        /// <summary>Its hurt sound, alert and idle calls are the pitched-down Bat's instead of the Wolf's (death: <see cref="BatFiendCreature"/>).</summary>
        public static void Dress(Humanoid humanoid, MonsterAI ai)
        {
            Swap(humanoid.m_hitEffects, Of("hurt"));
            Swap(ai.m_alertedEffects, Of("alert"));
            Swap(ai.m_idleSound, Of("idle"));
        }

        /// <summary>The list's sounds replaced by the voice (its other effects kept); unchanged without a voice.</summary>
        public static void Swap(EffectList list, GameObject? voice)
        {
            if (voice == null)
            {
                return;
            }
            List<EffectList.EffectData> kept = list.m_effectPrefabs.Where(e => e.m_prefab != null && !IsSound(e.m_prefab)).ToList();
            kept.Add(new EffectList.EffectData { m_prefab = voice });
            list.m_effectPrefabs = kept.ToArray();
        }
    }
}
