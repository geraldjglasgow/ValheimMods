using System.Collections.Generic;
using HarmonyLib;
using ItemCopies;
using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// The 80/20 split of the leggings' stats (user, 2026-10-07: "take the stats and split them between the boots and
    /// legs, 80% legs 20% boots"): armour, armour per quality level, eitr regen and every equipment modifier the game sums
    /// (<c>Player.s_equipmentModifierSources</c>: movement, heat resistance, the stamina modifiers, max adrenaline).
    /// Weight is taken where the game reads it (<see cref="BootsWeight"/>), never written. Resistances,
    /// equip effects, durability and body paint are not split: the leggings keep them (durability stays whole on both).
    /// The boots join the leggings' set, so every piece of that set needs one more (set size + 1). Every value starts
    /// from the one remembered at first sight, so switching off gives the game's numbers back exactly; written into the
    /// prefabs and every live copy (ItemCopies).
    /// </summary>
    public static class BootsStats
    {
        public const float LegsShare = 0.8f;
        public const float BootsShare = 0.2f;

        /// <summary>Every pair's movement modifier, whatever its leggings' (+5%).</summary>
        public const float BootsSpeed = 0.05f;

        private static readonly Dictionary<string, Original> legs = new Dictionary<string, Original>();
        private static readonly Dictionary<string, int> setSizes = new Dictionary<string, int>();
        private static AccessTools.FieldRef<SharedData, float>[] modifiers;

        /// <summary>Remembers a leggings' stats the first time it is seen (before anything of this mod wrote them).</summary>
        public static void Remember(GameObject legsPrefab)
        {
            if (!legs.ContainsKey(legsPrefab.name))
                legs[legsPrefab.name] = new Original(legsPrefab.GetComponent<ItemDrop>().m_itemData.m_shared, Modifiers());
        }

        /// <summary>
        /// A new boots item's stats: a fifth of its leggings', in their set with one more piece, except movement: every pair
        /// gives a flat <see cref="BootsSpeed"/> (user, 2026-10-07: "all boots should give a flat 5% movement speed, instead
        /// of taking away movement speed"; the leggings keep their 80%).
        /// </summary>
        public static void WriteBoots(SharedData boots, string legsName)
        {
            if (legs.TryGetValue(legsName, out Original original))
                original.Write(boots, BootsShare, Modifiers(), original.SetName.Length > 0 ? original.SetSize + 1 : original.SetSize);
            boots.m_movementModifier = BootsSpeed;
        }

        /// <summary>The leggings at 80% (or their own numbers) and their sets' pieces one bigger (or their own size).</summary>
        public static void Apply(ObjectDB db, bool on)
        {
            HashSet<string> sets = RememberSets(db);
            var names = new HashSet<string>(legs.Keys);
            names.UnionWith(setSizes.Keys);
            Copies.Apply(names, (name, shared) => Write(name, shared, on, sets));
        }

        private static void Write(string name, SharedData shared, bool on, HashSet<string> sets)
        {
            if (legs.TryGetValue(name, out Original original))
                original.Write(shared, on ? LegsShare : 1f, Modifiers(), original.SetSize);
            if (setSizes.TryGetValue(name, out int size))
                shared.m_setSize = on && sets.Contains(shared.m_setName) ? size + 1 : size;
        }

        /// <summary>The split leggings' set names; every item of those sets (boots aside) has its own size remembered.</summary>
        private static HashSet<string> RememberSets(ObjectDB db)
        {
            var sets = new HashSet<string>();
            foreach (Original original in legs.Values)
            {
                if (original.SetName.Length > 0)
                    sets.Add(original.SetName);
            }
            if (db == null)
                return sets;
            foreach (GameObject prefab in db.m_items)
            {
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                SharedData shared = drop != null ? drop.m_itemData.m_shared : null;
                if (shared != null && sets.Contains(shared.m_setName) && !BootSets.Is(drop.m_itemData) && !setSizes.ContainsKey(prefab.name))
                    setSizes[prefab.name] = legs.TryGetValue(prefab.name, out Original own) ? own.SetSize : shared.m_setSize;
            }
            return sets;
        }

        /// <summary>Typed getters for the game's modifier fields, made once (the game's list is a constant).</summary>
        private static AccessTools.FieldRef<SharedData, float>[] Modifiers()
        {
            if (modifiers != null)
                return modifiers;
            var made = new List<AccessTools.FieldRef<SharedData, float>>();
            foreach (string field in Player.s_equipmentModifierSources)
            {
                if (AccessTools.Field(typeof(SharedData), field)?.FieldType == typeof(float))
                    made.Add(AccessTools.FieldRefAccess<SharedData, float>(field));
            }
            return modifiers = made.ToArray();
        }

        /// <summary>A leggings' own splittable numbers and set.</summary>
        private sealed class Original
        {
            private readonly float armor;
            private readonly float armorPerLevel;
            private readonly float eitrRegen;
            private readonly float[] values;

            public Original(SharedData shared, AccessTools.FieldRef<SharedData, float>[] fields)
            {
                armor = shared.m_armor;
                armorPerLevel = shared.m_armorPerLevel;
                eitrRegen = shared.m_eitrRegenModifier;
                SetName = shared.m_setName ?? "";
                SetSize = shared.m_setSize;
                values = new float[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                    values[i] = fields[i](shared);
            }

            public string SetName { get; }

            public int SetSize { get; }

            public void Write(SharedData shared, float share, AccessTools.FieldRef<SharedData, float>[] fields, int setSize)
            {
                shared.m_armor = armor * share;
                shared.m_armorPerLevel = armorPerLevel * share;
                shared.m_eitrRegenModifier = eitrRegen * share;
                for (int i = 0; i < fields.Length && i < values.Length; i++)
                    fields[i](shared) = values[i] * share;
                shared.m_setSize = setSize;
            }
        }
    }
}
