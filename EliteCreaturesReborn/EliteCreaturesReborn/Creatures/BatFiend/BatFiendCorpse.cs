using System.Linq;
using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// What a dead Bat-fiend leaves, the way every creature's body goes (Old Gorehide's way): a network object of its
    /// own, never saved, made where it died by its owner from its death effects, so every peer sees it, at the creature's
    /// size (its root's <see cref="BatFiendNumbers.Scale"/> here, its star size from the death effect's
    /// <c>m_multiplyParentVisualScale</c>). It carries a copy of the model that plays the death clip and holds its last
    /// frame (<see cref="BatFiendCorpsePose"/>, no <see cref="CharacterAnimEvent"/>: the death clip calls nothing), and the
    /// game's own <see cref="Ragdoll"/> without physics bodies: the dying creature hands it its loot and star tint, and once
    /// the fall and the game Wolf corpse's own wait are over its owner plays the Wolf corpse's removal puff, drops the
    /// loot where it lies and removes it.
    /// </summary>
    internal static class BatFiendCorpse
    {
        private const float WolfWaitFallback = 5f;

        public static GameObject Build(GameObject wolf)
        {
            var corpse = new GameObject(BatFiendPrefabs.Corpse);
            corpse.transform.SetParent(PrefabBench.Root, false);
            corpse.transform.localScale = Vector3.one * BatFiendNumbers.Scale;
            var nview = corpse.AddComponent<ZNetView>();
            (nview.m_persistent, nview.m_type, nview.m_syncInitialScale) = (false, ZDO.ObjectType.Default, true);
            GameObject visual = BatFiendModel.Copy(corpse.transform);
            corpse.AddComponent<BatFiendCorpsePose>();
            Lie(corpse.AddComponent<Ragdoll>(), WolfRagdoll(wolf), BatFiendModel.Body(visual));
            return corpse;
        }

        /// <summary>The death effect that makes the corpse: facing as the creature did, sized by its Visual's star scale.</summary>
        public static EffectList.EffectData Effect(GameObject corpse) =>
            new EffectList.EffectData { m_prefab = corpse, m_inheritParentRotation = true, m_multiplyParentVisualScale = true };

        /// <summary>The game Wolf's own corpse, for its wait and its removal puff (null if it has none).</summary>
        public static Ragdoll? WolfRagdoll(GameObject wolf) =>
            wolf.GetComponent<Character>()?.m_deathEffects.m_effectPrefabs
                .Select(effect => effect.m_prefab != null ? effect.m_prefab.GetComponent<Ragdoll>() : null)
                .FirstOrDefault(ragdoll => ragdoll != null);

        private static void Lie(Ragdoll ragdoll, Ragdoll? wolf, SkinnedMeshRenderer? body)
        {
            ragdoll.m_ttl = BatFiendNumbers.DeathClip + (wolf != null ? wolf.m_ttl : WolfWaitFallback);
            ragdoll.m_removeEffect = wolf != null ? wolf.m_removeEffect : new EffectList();
            (ragdoll.m_dropItems, ragdoll.m_float, ragdoll.m_mainModel, ragdoll.m_lootSpawnJoint) = (true, false, body, null);
        }
    }

    /// <summary>On every peer that shows the corpse: straight into the death clip, held on its last frame until it goes.</summary>
    public sealed class BatFiendCorpsePose : MonoBehaviour
    {
        private void Start()
        {
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.SetBool("onGround", true);
                animator.SetBool("dead", true);
            }
        }
    }
}
