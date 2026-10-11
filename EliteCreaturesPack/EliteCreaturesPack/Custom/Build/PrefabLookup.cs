using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// Finds what a definition names, for the build steps: prefabs of the game and every mod (networked or not, as
    /// ZNetScene holds them), the custom creatures being built (by shell), items (ZNetScene, ObjectDB, and the attack
    /// items creatures carry, which are in neither), and status effects (ObjectDB, by asset name). One lookup serves one
    /// build pass; the item and status effect tables are made the first time they are asked for. Names match exactly,
    /// except status effects, which ignore case. Every answer is null when nothing has the name: the step reports it.
    /// </summary>
    public sealed class PrefabLookup
    {
        private readonly ZNetScene scene;
        private readonly Dictionary<string, GameObject> customs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly HashSet<string> parts = new HashSet<string>(StringComparer.Ordinal);
        private Dictionary<string, GameObject>? carried;
        private Dictionary<string, StatusEffect>? statusEffects;

        internal PrefabLookup(ZNetScene scene)
        {
            this.scene = scene;
        }

        /// <summary>Every custom creature's shell by name. All of them exist before any step runs; one may still fail later
        /// (call <see cref="CreatureBuild.Needs"/> when this creature cannot do without it).</summary>
        public IReadOnlyDictionary<string, GameObject> Customs => customs;

        /// <summary>A custom creature's shell, or null.</summary>
        public GameObject? Custom(string name) => customs.TryGetValue(name, out GameObject shell) ? shell : null;

        /// <summary>Any prefab: a custom creature's shell, or one ZNetScene knows (effects, creatures, items, pieces).</summary>
        public GameObject? Prefab(string name) => Custom(name) ?? scene.GetPrefab(name);

        /// <summary>A creature prefab (one with a <see cref="Character"/>, never the player), custom ones included.</summary>
        public GameObject? Creature(string name)
        {
            GameObject? prefab = Prefab(name);
            Character? character = prefab != null ? prefab.GetComponent<Character>() : null;
            return character != null && !(character is Player) ? prefab : null;
        }

        /// <summary>An item prefab (one with an <see cref="ItemDrop"/>): a game or mod item, or a creature's attack item.</summary>
        public GameObject? Item(string name)
        {
            GameObject? prefab = scene.GetPrefab(name);
            if (prefab == null && ObjectDB.instance != null)
            {
                prefab = ObjectDB.instance.GetItemPrefab(name);
            }
            if (prefab == null)
            {
                Carried().TryGetValue(name, out prefab);
            }
            return prefab != null && prefab.GetComponent<ItemDrop>() != null ? prefab : null;
        }

        /// <summary>A status effect by its asset name (like <c>Poison</c> or <c>Burning</c>), case ignored.</summary>
        public StatusEffect? StatusEffect(string name)
        {
            if (statusEffects == null)
            {
                statusEffects = new Dictionary<string, StatusEffect>(StringComparer.OrdinalIgnoreCase);
                foreach (StatusEffect effect in ObjectDB.instance != null ? ObjectDB.instance.m_StatusEffects : new List<StatusEffect>())
                {
                    if (effect != null && !statusEffects.ContainsKey(effect.name))
                    {
                        statusEffects.Add(effect.name, effect);
                    }
                }
            }
            return statusEffects.TryGetValue(name, out StatusEffect found) ? found : null;
        }

        internal void AddCustom(string name, GameObject shell) => customs[name] = shell;

        /// <summary>Whether a part of any creature in this pass already has the name (parts are registered only at the end).</summary>
        internal bool PartTaken(string name) => parts.Contains(name);

        internal void TakePart(string name) => parts.Add(name);

        /// <summary>The items creatures carry (default items, random weapons, armour, shields, sets and items), by name.</summary>
        private Dictionary<string, GameObject> Carried()
        {
            if (carried != null)
            {
                return carried;
            }
            carried = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (GameObject prefab in scene.m_prefabs)
            {
                Humanoid? humanoid = prefab != null ? prefab.GetComponent<Humanoid>() : null;
                if (humanoid != null)
                {
                    AddCarried(humanoid);
                }
            }
            return carried;
        }

        private void AddCarried(Humanoid humanoid)
        {
            AddAll(humanoid.m_defaultItems);
            AddAll(humanoid.m_randomWeapon);
            AddAll(humanoid.m_randomArmor);
            AddAll(humanoid.m_randomShield);
            foreach (Humanoid.ItemSet set in humanoid.m_randomSets ?? Array.Empty<Humanoid.ItemSet>())
            {
                AddAll(set.m_items);
            }
            foreach (Humanoid.RandomItem item in humanoid.m_randomItems ?? Array.Empty<Humanoid.RandomItem>())
            {
                AddAll(new[] { item.m_prefab });
            }
        }

        private void AddAll(GameObject[]? items)
        {
            foreach (GameObject item in items ?? Array.Empty<GameObject>())
            {
                if (item != null && !carried!.ContainsKey(item.name))
                {
                    carried.Add(item.name, item);
                }
            }
        }
    }
}
