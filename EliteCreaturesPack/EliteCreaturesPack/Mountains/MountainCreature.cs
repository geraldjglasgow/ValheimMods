using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesPack.Mountains
{
    public static class MountainCreature
    {
        public static GameObject Build(MountainKind kind, GameObject source, GameObject[] attacks, GameObject art, ZNetScene scene)
        {
            GameObject creature = PrefabBench.Copy(source, kind.Creature);
            creature.transform.localScale *= kind.Scale;
            Configure(creature.GetComponent<Humanoid>(), kind, attacks);
            Wild(creature, kind);
            MountainLook.Dress(creature, kind, art);
            MountainCorpses.Build(creature, kind);
            if (kind.Boss) creature.AddComponent<FrostfangRage>();
            Loot(creature.GetComponent<CharacterDrop>(), kind, scene);
            return creature;
        }
        private static void Configure(Humanoid humanoid, MountainKind kind, GameObject[] attacks)
        {
            humanoid.m_name = "$" + kind.Word;
            humanoid.m_health = MountainSettings.For(kind).Health;
            humanoid.m_faction = Character.Faction.MountainMonsters;
            humanoid.m_defaultItems = attacks;
            humanoid.m_randomWeapon = new GameObject[0];
            humanoid.m_randomSets = new Humanoid.ItemSet[0];
            humanoid.m_randomItems = new Humanoid.RandomItem[0];
            humanoid.m_damageModifiers.m_frost = HitData.DamageModifier.Immune;
            humanoid.m_damageModifiers.m_fire = HitData.DamageModifier.Weak;
        }
        private static void Wild(GameObject creature, MountainKind kind)
        {
            // A miniature boss has ordinary enemy progression: no forsaken key, boss altar, or global event.
            Remove<Tameable>(creature);
            Remove<Procreation>(creature);
            foreach (Sadle saddle in creature.GetComponentsInChildren<Sadle>(true)) Object.DestroyImmediate(saddle.gameObject);
            MonsterAI ai = creature.GetComponent<MonsterAI>();
            ai.m_consumeItems = new List<ItemDrop>();
            if (kind.Boss) ai.m_circleTargetInterval = 6f;
            if (kind.Id == "Rimeback") ai.m_circleTargetInterval = 0f;
        }
        private static void Remove<T>(GameObject creature) where T : Component
        {
            foreach (T component in creature.GetComponentsInChildren<T>(true)) Object.DestroyImmediate(component);
        }
        private static void Loot(CharacterDrop drops, MountainKind kind, ZNetScene scene)
        {
            drops.m_drops = new List<CharacterDrop.Drop>();
            if (kind.Boss)
            {
                Add(drops, scene, "WolfFang", 4, 7); Add(drops, scene, "WolfPelt", 3, 5);
                Add(drops, scene, "TrophyWolf", 1, 1, .35f);
            }
            else if (kind.Id == "Rimeback")
            {
                Add(drops, scene, "Stone", 5, 10); Add(drops, scene, "Crystal", 1, 3); Add(drops, scene, "LoxMeat", 1, 2);
            }
            else SmallLoot(drops, kind, scene);
        }
        private static void SmallLoot(CharacterDrop drops, MountainKind kind, ZNetScene scene)
        {
            if (kind.Flying)
            {
                Add(drops, scene, "FreezeGland", 1, 2); Add(drops, scene, "Stone", 2, 4);
            }
            else if (kind.Night)
            {
                Add(drops, scene, "BoneFragments", 3, 6); Add(drops, scene, "Crystal", 1, 2);
            }
            else
            {
                Add(drops, scene, "NeckTail", 1, 2); Add(drops, scene, "FreezeGland", 1, 1, .35f);
            }
        }
        private static void Add(CharacterDrop drops, ZNetScene scene, string name, int min, int max, float chance = 1f)
        {
            GameObject item = scene.GetPrefab(name);
            if (item != null) drops.m_drops.Add(new CharacterDrop.Drop
            {
                m_prefab = item, m_amountMin = min, m_amountMax = max, m_chance = chance, m_levelMultiplier = true,
            });
        }
    }
}
