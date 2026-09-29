using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// A crossbowman creature: a copy of one of the game's archer skeletons (<see cref="XbowKind"/>), so it keeps that
    /// skeleton's health, resistances, faction,
    /// sounds, senses, AI, star looks and loot, with these changes: its only weapon is the crossbow shot (the archer's
    /// bow, restrung; no sword, no shield), it wears the kit (the bone crossbow in the left fist, the quiver of bone
    /// bolts on the right hip)
    /// and plays the bundle's clips in place of the Skeleton's idle, walk, run and bow shot, and it drops a few bone bolts
    /// too. Its string and bolts follow the clips (<see cref="XbowRig"/>).
    /// </summary>
    public static class XbowCreature
    {
        /// <summary>The Skeleton controller's clips, by name, and the bundle's clips that take their place.</summary>
        private static readonly (string game, string ours)[] Clips =
        {
            ("Idle", "ecp_xbow_idle"), ("Shield-Walk-Injured", "ecp_xbow_walk"), ("Shield-Run-Forward", "ecp_xbow_run"),
            ("Bow Aim Idle 01", "ecp_xbow_aim"), ("Bow Aim Recoil", "ecp_xbow_fire"),
        };

        public static GameObject Build(XbowKind kind, GameObject skeleton, GameObject shot, AssetBundle bundle, GameObject kit, GameObject? boltItem)
        {
            GameObject creature = PrefabBench.Copy(skeleton, kind.Creature);
            Arm(creature.GetComponent<Humanoid>(), shot);
            MoreBolts(creature.GetComponent<CharacterDrop>(), boltItem);
            Transform visual = creature.transform.Find("Visual");
            XbowKit.Wear(visual, kit, XbowKit.Skin(visual));
            Animate(visual.GetComponentInChildren<Animator>(true), bundle);
            creature.AddComponent<XbowRig>();
            return creature;
        }

        /// <summary>The crossbow is its only weapon, as the bow is the archer's: the Skeleton's random sword and shield go.</summary>
        private static void Arm(Humanoid humanoid, GameObject shot)
        {
            humanoid.m_name = "$enemy_ecp_skeletoncrossbowman";
            humanoid.m_defaultItems = new[] { shot };
            humanoid.m_randomWeapon = new GameObject[0];
            humanoid.m_randomShield = new GameObject[0];
            humanoid.m_randomSets = new Humanoid.ItemSet[0];
        }

        /// <summary>One to three bone bolts, half the time, on top of the Skeleton's own drops: the quiver's.</summary>
        private static void MoreBolts(CharacterDrop? drops, GameObject? boltItem)
        {
            if (drops == null || boltItem == null)
            {
                return;
            }
            drops.m_drops.Add(new CharacterDrop.Drop
            {
                m_prefab = boltItem, m_amountMin = 1, m_amountMax = 3, m_chance = 0.5f, m_levelMultiplier = true,
            });
        }

        /// <summary>The bundle's clips play wherever the Skeleton's controller would play its own, on every peer.</summary>
        private static void Animate(Animator animator, AssetBundle bundle)
        {
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            var crossbow = new AnimatorOverrideController(controller) { name = "ecp_crossbowman_animator" };
            foreach (var (game, ours) in Clips)
            {
                AnimationClip? original = controller.animationClips.FirstOrDefault(c => c.name == game);
                AnimationClip? clip = bundle.LoadAsset<AnimationClip>(ours);
                if (original == null || clip == null)
                {
                    Log.Warn($"Skeleton crossbowman: no {(clip == null ? ours + " in the bundle" : game + " clip in the Skeleton's animator")}; it plays the Skeleton's.");
                    continue;
                }
                crossbow[original] = clip;
            }
            animator.runtimeAnimatorController = crossbow;
        }
    }
}
