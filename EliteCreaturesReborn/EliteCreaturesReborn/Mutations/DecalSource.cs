using EliteCreaturesReborn.Util;
using EliteCreaturesReborn.Visuals;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Finds the ground decal a trail's patches are drawn with: the game's own splat system (the one its
    /// <c>ParticleDecal</c> script feeds when a blob's slime or a creature's blood lands, drawn lying on whatever
    /// surface it struck). It is taken from the vanilla effect the rule file names (`trail effect`, resolved like every
    /// other effect name); should that effect carry no decal, the nearest effect that does - by the kind's keywords,
    /// then any - stands in, logged once, so a mistyped name still draws a trail rather than leaving an invisible
    /// hazard. Nothing here throws.
    /// </summary>
    internal static class DecalSource
    {
        /// <summary>
        /// The decal system for <paramref name="effect"/>; null only when the game has no ground decal at all.
        /// </summary>
        public static ParticleSystem? Find(string effect, TrailKind kind)
        {
            GameObject? prefab = EffectResolver.Resolve(effect, kind.Keywords, kind.Setting);
            ParticleSystem? decal = DecalOf(prefab);
            if (decal != null)
            {
                return decal;
            }
            decal = Scan(kind.Keywords);
            Log.Warn(decal != null
                ? $"effect '{effect}' (from setting '{kind.Setting}') draws no ground decal; "
                    + $"{kind.Name} patches use '{decal.name}'"
                : $"no ground decal found in the game; {kind.Name} patches will not be drawn");
            return decal;
        }

        private static ParticleSystem? DecalOf(GameObject? prefab)
        {
            ParticleDecal? decal = prefab != null ? prefab.GetComponentInChildren<ParticleDecal>(true) : null;
            return decal != null ? decal.m_decalSystem : null;
        }

        private static ParticleSystem? Scan(string[] keywords)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
            {
                return null;
            }
            foreach (string keyword in keywords)
            {
                ParticleSystem? found = ScanFor(scene, keyword);
                if (found != null)
                {
                    return found;
                }
            }
            return ScanFor(scene, "");
        }

        private static ParticleSystem? ScanFor(ZNetScene scene, string keyword)
        {
            foreach (GameObject prefab in EffectResolver.Prefabs(scene))
            {
                if (EffectResolver.IsEffectName(prefab) && prefab.name.ToLowerInvariant().Contains(keyword))
                {
                    ParticleSystem? decal = DecalOf(prefab);
                    if (decal != null)
                    {
                        return decal;
                    }
                }
            }
            return null;
        }
    }
}
