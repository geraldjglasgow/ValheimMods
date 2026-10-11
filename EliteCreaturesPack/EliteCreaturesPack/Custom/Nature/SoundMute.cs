using System;
using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// `sounds: mute` takes the sounds out of the shell's effect lists and keeps everything that draws: alert is the
    /// mind's <c>BaseAI.m_alertedEffects</c>, idle its <c>m_idleSound</c>, hurt the body's <c>Character.m_hitEffects</c>
    /// (the game's critical and backstab hit effects are the player's feedback, not the creature's voice, and stay),
    /// death its <c>m_deathEffects</c> (the ragdoll and the blood stay). An effect is a sound when it is one of the game's
    /// <c>sfx_</c> prefabs, or plays audio and draws nothing. One that plays audio and also draws stays, named in a warning.
    /// Each list gets a new array, never a change to one it may share. The look step runs after this one and replaces whole
    /// lists (`effects:`), so a list a definition replaces is exactly what it lists.
    /// </summary>
    internal static class SoundMute
    {
        private const string Field = "sounds.mute";

        public static void Apply(CreatureBuild build, Character character)
        {
            SoundsBlock? sounds = build.Definition.Sounds;
            if (sounds == null)
            {
                return;
            }
            BaseAI ai = build.Shell.GetComponent<BaseAI>();
            foreach (SoundKind kind in sounds.Mute)
            {
                EffectList? list = ListOf(kind, character, ai);
                if (list == null)
                {
                    build.Report.Warn($"it has no mind of the game's (BaseAI), so it makes no {Word(kind)} sound to mute", Field);
                    continue;
                }
                list.m_effectPrefabs = Quiet(build, list.m_effectPrefabs, kind);
            }
        }

        private static EffectList? ListOf(SoundKind kind, Character character, BaseAI ai)
        {
            switch (kind)
            {
                case SoundKind.Alert: return ai != null ? ai.m_alertedEffects : null;
                case SoundKind.Idle: return ai != null ? ai.m_idleSound : null;
                case SoundKind.Hurt: return character.m_hitEffects;
                default: return character.m_deathEffects;
            }
        }

        private static EffectList.EffectData[] Quiet(CreatureBuild build, EffectList.EffectData[]? effects, SoundKind kind)
        {
            List<EffectList.EffectData> kept = new List<EffectList.EffectData>();
            foreach (EffectList.EffectData effect in effects ?? Array.Empty<EffectList.EffectData>())
            {
                GameObject? prefab = effect != null ? effect.m_prefab : null;
                if (prefab != null && IsSound(prefab))
                {
                    continue;
                }
                if (prefab != null && Plays(prefab))
                {
                    build.Report.Warn($"its {Word(kind)} effect '{prefab.name}' plays a sound and draws too, so it stays", Field);
                }
                if (effect != null)
                {
                    kept.Add(effect);
                }
            }
            return kept.ToArray();
        }

        private static bool IsSound(GameObject prefab) =>
            prefab.name.StartsWith("sfx_", StringComparison.OrdinalIgnoreCase) || (Plays(prefab) && !Draws(prefab));

        private static bool Plays(GameObject prefab) =>
            prefab.GetComponentInChildren<ZSFX>(true) != null || prefab.GetComponentInChildren<AudioSource>(true) != null;

        private static bool Draws(GameObject prefab) =>
            prefab.GetComponentInChildren<Renderer>(true) != null || prefab.GetComponentInChildren<ParticleSystem>(true) != null
            || prefab.GetComponentInChildren<Light>(true) != null;

        private static string Word(SoundKind kind) => kind.ToString().ToLowerInvariant();
    }
}
