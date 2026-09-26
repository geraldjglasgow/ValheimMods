using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// One Summoner wave, on the boss's owner: `count` creatures drawn from the boss's own summon list, each forced to
    /// `stars` stars and no mutations, placed a few metres from the boss on the floor beneath it and alerted. They are
    /// ordinary creatures from then on - owned here, replicated like any spawn, dropping their own loot. A name the game
    /// does not know is skipped and logged once, so one typo in the rule file costs a creature, not the fight.
    /// </summary>
    internal static class SummonWave
    {
        private static readonly HashSet<string> Warned = new HashSet<string>();

        public static void Call(Character boss, EliteController controller)
        {
            List<string> names = RuleState.Active.Boss.Aspects.SummonsFor(Utils.GetPrefabName(boss.gameObject));
            int count = Mathf.Clamp(Mathf.RoundToInt(AspectMath.Power(Aspect.Summoner, Fields.Count)), 0, 16);
            int stars = Mathf.Max(0, Mathf.RoundToInt(AspectMath.Power(Aspect.Summoner, Fields.Stars)));
            for (int i = 0; i < count && names.Count > 0; i++)
            {
                Spawn(boss, controller, names[Random.Range(0, names.Count)], stars);
            }
        }

        private static void Spawn(Character boss, EliteController controller, string name, int stars)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null || prefab.GetComponent<Character>() == null)
            {
                if (Warned.Add(name))
                {
                    Log.Warn($"Summoner: '{name}' is not a creature prefab this game knows - skipped");
                }
                return;
            }
            Vector3 pos = SpawnPlace.Random(boss.transform.position, 3f, 6f);
            Arrive(Object.Instantiate(prefab, pos, Quaternion.identity), stars, pos);
            CreatureRpc.FireFlash(controller.View, pos + Vector3.up, 1.5f, "summon");
        }

        // Same frame as the Instantiate, before the creature's controller wakes: the forced stars are then written as its
        // roll, exactly as `elite spawn` does it.
        private static void Arrive(GameObject creature, int stars, Vector3 pos)
        {
            EliteController elite = creature.GetComponent<EliteController>();
            if (elite != null)
            {
                elite.ForceTraits(new CreatureTraits(stars, 0), Heightmap.FindBiome(pos));
            }
            BaseAI ai = creature.GetComponent<BaseAI>();
            if (ai != null)
            {
                ai.Alert();
            }
        }
    }
}
