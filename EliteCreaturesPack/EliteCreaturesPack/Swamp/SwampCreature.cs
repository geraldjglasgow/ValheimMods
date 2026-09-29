using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesPack.Swamp
{
    public static class SwampCreature
    {
        public static GameObject Build(SwampKind kind, GameObject source, AssetBundle bundle, ZNetScene scene)
        {
            GameObject creature = PrefabBench.Copy(source, kind.Creature);
            Humanoid humanoid = creature.GetComponent<Humanoid>();
            humanoid.m_name = "$" + kind.Word;
            humanoid.m_health = SwampSettings.For(kind).Health;
            humanoid.m_defaultItems = SwampAttacks.Build(kind, source.GetComponent<Humanoid>(), scene);
            humanoid.m_randomWeapon = new GameObject[0];
            humanoid.m_randomShield = new GameObject[0];
            humanoid.m_randomSets = new Humanoid.ItemSet[0];
            humanoid.m_randomItems = new Humanoid.RandomItem[0];
            SwampLook.Wear(creature, kind, bundle, scene.GetPrefab("Draugr"));
            if (kind.Id == "ReedStalker") SwampSpear.Animate(creature, scene);
            creature.transform.localScale *= kind.Scale;
            Role(kind, humanoid, creature.GetComponent<MonsterAI>(), scene);
            Loot(kind, creature.GetComponent<CharacterDrop>(), scene);
            return creature;
        }

        private static void Role(SwampKind kind, Humanoid humanoid, MonsterAI ai, ZNetScene scene)
        {
            humanoid.m_faction = scene.GetPrefab("Draugr").GetComponent<Character>().m_faction;
            if (kind.Jarl) { humanoid.m_runSpeed *= 0.8f; humanoid.m_staggerDamageFactor *= 1.5f; }
            if (kind.Id == "ReedStalker") { humanoid.m_runSpeed *= 1.15f; ai.m_circleTargetInterval = 5f; }
            if (kind.Id == "BogMaw")
            {
                ai.m_sleeping = true; ai.m_wakeupRange = 7f; ai.m_noiseWakeup = true;
                ai.m_wakeUpDelayMin = 0.8f; ai.m_wakeUpDelayMax = 1.5f;
            }
            if (kind.Id == "FenCrawler")
            {
                humanoid.m_damageModifiers.m_pierce = HitData.DamageModifier.Resistant;
                humanoid.m_damageModifiers.m_blunt = HitData.DamageModifier.Weak;
                humanoid.m_damageModifiers.m_poison = HitData.DamageModifier.Immune;
            }
        }

        private static void Loot(SwampKind kind, CharacterDrop drops, ZNetScene scene)
        {
            drops.m_drops = new List<CharacterDrop.Drop>();
            if (kind.Jarl) { Add(drops, scene, "Entrails", 4, 6); Add(drops, scene, "Chain", 1, 2); }
            if (kind.Id == "ReedStalker") { Add(drops, scene, "Entrails", 1, 2); Add(drops, scene, "Wood", 2, 4); }
            if (kind.Id == "BogMaw") { Add(drops, scene, "Ooze", 2, 4); Add(drops, scene, "BoneFragments", 1, 2); }
            if (kind.Id == "FenCrawler") { Add(drops, scene, "NeckTail", 1, 2); Add(drops, scene, "BoneFragments", 1, 2); }
            if (kind.Night) { Add(drops, scene, "Chain", 1, 1); Add(drops, scene, "Coal", 2, 3); }
        }

        private static void Add(CharacterDrop drops, ZNetScene scene, string name, int min, int max, float chance = 1f)
        {
            GameObject prefab = scene.GetPrefab(name);
            if (prefab != null) drops.m_drops.Add(new CharacterDrop.Drop
            {
                m_prefab = prefab, m_amountMin = min, m_amountMax = max, m_chance = chance, m_levelMultiplier = true,
            });
        }
    }
}
