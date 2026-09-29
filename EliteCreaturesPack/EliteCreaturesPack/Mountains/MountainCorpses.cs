using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesPack.Mountains
{
    /// <summary>Private, correctly sized ragdolls; decorative frost plates are shed on death.</summary>
    public static class MountainCorpses
    {
        public static readonly List<GameObject> Prefabs = new List<GameObject>();
        public static void Build(GameObject creature, MountainKind kind)
        {
            Humanoid humanoid = creature.GetComponent<Humanoid>();
            EffectList.EffectData[] effects = humanoid.m_deathEffects.m_effectPrefabs;
            var replacement = new EffectList.EffectData[effects.Length];
            for (int i = 0; i < effects.Length; ++i)
            {
                EffectList.EffectData effect = effects[i];
                replacement[i] = effect;
                if (effect.m_prefab == null || effect.m_prefab.GetComponent<Ragdoll>() == null) continue;
                GameObject corpse = PrefabBench.Copy(effect.m_prefab, kind.Creature + "_ragdoll_" + i);
                corpse.transform.localScale *= kind.Scale;
                MountainLook.Tint(corpse, kind);
                Prefabs.Add(corpse);
                replacement[i] = DeathEffect(effect, corpse);
            }
            humanoid.m_deathEffects = new EffectList { m_effectPrefabs = replacement };
        }
        private static EffectList.EffectData DeathEffect(EffectList.EffectData source, GameObject corpse) => new EffectList.EffectData
        {
            m_prefab = corpse, m_enabled = source.m_enabled, m_variant = source.m_variant,
            m_attach = source.m_attach, m_follow = source.m_follow,
            m_inheritParentRotation = source.m_inheritParentRotation,
            m_inheritParentScale = false, m_scale = false, m_multiplyParentVisualScale = true,
            m_randomRotation = source.m_randomRotation, m_childTransform = source.m_childTransform,
        };
    }
}
