using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// One entry of a replaced effect list. The game sets each entry's own way of playing (attached to the body, scaled to
    /// it, on a named bone, turned with it); a definition names only prefabs, so an entry for a prefab the list already had
    /// (on the creature as built so far, or on its base) keeps those settings, and any other plays where the game plays the
    /// list: at the hit point, or at the creature, on its own. Always a new entry, switched on.
    /// </summary>
    internal static class EffectEntries
    {
        public static EffectList.EffectData For(GameObject prefab, EffectList current, EffectList? original)
        {
            EffectList.EffectData? known = Find(current, prefab) ?? (original != null ? Find(original, prefab) : null);
            EffectList.EffectData entry = known != null ? Copy(known) : new EffectList.EffectData { m_prefab = prefab };
            entry.m_enabled = true;
            return entry;
        }

        /// <summary>A new entry with the same settings (the base's entries are never shared or changed).</summary>
        public static EffectList.EffectData Copy(EffectList.EffectData entry) => new EffectList.EffectData
        {
            m_prefab = entry.m_prefab,
            m_enabled = entry.m_enabled,
            m_variant = entry.m_variant,
            m_attach = entry.m_attach,
            m_follow = entry.m_follow,
            m_inheritParentRotation = entry.m_inheritParentRotation,
            m_inheritParentScale = entry.m_inheritParentScale,
            m_multiplyParentVisualScale = entry.m_multiplyParentVisualScale,
            m_randomRotation = entry.m_randomRotation,
            m_scale = entry.m_scale,
            m_childTransform = entry.m_childTransform,
        };

        private static EffectList.EffectData? Find(EffectList list, GameObject prefab)
        {
            foreach (EffectList.EffectData entry in list.m_effectPrefabs ?? new EffectList.EffectData[0])
            {
                if (entry != null && entry.m_prefab == prefab)
                {
                    return entry;
                }
            }
            return null;
        }
    }
}
