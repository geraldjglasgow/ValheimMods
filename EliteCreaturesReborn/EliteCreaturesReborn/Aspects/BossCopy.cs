using System;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Makes another of a boss's own prefab for Twin and Phantom, on the boss's owner at its first roll. The copy is born
    /// already resolved - its traits, biome and aspect link are written to its ZDO in the frame it is instantiated, before
    /// its own controller wakes - so its owner never rolls it, it never brings a twin or copies of its own, and every
    /// client reads the same thing. It wakes the way the altar wakes the boss: patrolling where it stands, alerted if the
    /// boss is.
    /// </summary>
    internal static class BossCopy
    {
        public static Character? Make(EliteController boss, Vector3 pos, CreatureTraits traits, Action<ZDO> link)
        {
            ZDO bossZdo = boss.View.GetZDO();
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(bossZdo.GetPrefab()) : null;
            if (prefab == null)
            {
                Log.Warn($"{boss.name}: no prefab to copy for its {traits.Aspect} aspect");
                return null;
            }
            GameObject copy = Object.Instantiate(prefab, pos, boss.transform.rotation);
            ZNetView nview = copy.GetComponent<ZNetView>();
            Character character = copy.GetComponent<Character>();
            if (nview == null || character == null || nview.GetZDO() == null)
            {
                return null;
            }
            Mark(nview.GetZDO(), bossZdo, traits, link);
            character.SetLevel(1);
            Wake(boss, copy);
            return character;
        }

        private static void Mark(ZDO zdo, ZDO bossZdo, CreatureTraits traits, Action<ZDO> link)
        {
            link(zdo);
            TraitStore.Save(zdo, traits);
            TraitStore.SetBiome(zdo, TraitStore.GetBiome(bossZdo));
        }

        private static void Wake(EliteController boss, GameObject copy)
        {
            BaseAI ai = copy.GetComponent<BaseAI>();
            if (ai == null)
            {
                return;
            }
            ai.SetPatrolPoint();
            BaseAI bossAi = boss.GetComponent<BaseAI>();
            if (bossAi != null && bossAi.IsAlerted())
            {
                ai.Alert();
            }
        }
    }
}
