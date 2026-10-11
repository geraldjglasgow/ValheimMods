using System;
using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// One custom creature of the current world, as built and registered: its definition (and its chain of base
    /// definitions), its prefab and the parts made for it. What runtime code and commands ask about a custom creature.
    /// </summary>
    public sealed class CustomCreature
    {
        internal CustomCreature(ShellRecord record)
        {
            Definition = record.Chain.Creature;
            Chain = record.Chain.Links;
            Prefab = record.Shell;
            PrefabHash = record.Shell.name.GetStableHashCode();
            IsHuman = record.Chain.Human;
            Parts = record.Parts.ConvertAll(part => part.Key);
            PartOrigins = record.PartOrigins;
        }

        /// <summary>The creature's own definition.</summary>
        public CreatureDefinition Definition { get; }

        /// <summary>Its definitions, base-most first, <see cref="Definition"/> last.</summary>
        public IReadOnlyList<CreatureDefinition> Chain { get; }

        /// <summary>The registered prefab.</summary>
        public GameObject Prefab { get; }

        /// <summary>The prefab's name hash, as ZDOs carry it.</summary>
        public int PrefabHash { get; }

        /// <summary>Whether it is a person on the player's body.</summary>
        public bool IsHuman { get; }

        /// <summary>The prefabs made for it (attack items, projectiles, effects).</summary>
        public IReadOnlyList<GameObject> Parts { get; }

        /// <summary>Each part's original by the part's name: the prefab it was first copied from (what `ecp export` names).</summary>
        public IReadOnlyDictionary<string, string> PartOrigins { get; }
    }

    /// <summary>
    /// The custom creatures of the current world, on this peer, by prefab name and hash: filled by each build, emptied
    /// (and their prefabs destroyed) when the next world builds its own. The same on the server and every client, since
    /// every peer builds from the server's definitions.
    /// </summary>
    public static class CustomPrefabs
    {
        private static readonly Dictionary<string, CustomCreature> byName = new Dictionary<string, CustomCreature>(StringComparer.Ordinal);
        private static readonly Dictionary<int, CustomCreature> byHash = new Dictionary<int, CustomCreature>();

        /// <summary>Every custom creature of the current world.</summary>
        public static IReadOnlyCollection<CustomCreature> All => byName.Values;

        public static bool TryGet(string prefabName, out CustomCreature creature) => byName.TryGetValue(prefabName, out creature);

        /// <summary>By the prefab hash a ZDO carries (<c>zdo.GetPrefab()</c>): cheap enough for a creature's Awake.</summary>
        public static bool TryGet(int prefabHash, out CustomCreature creature) => byHash.TryGetValue(prefabHash, out creature);

        internal static void Add(CustomCreature creature)
        {
            byName[creature.Definition.Name] = creature;
            byHash[creature.PrefabHash] = creature;
        }

        /// <summary>Forgets the last world's creatures and destroys their prefabs (their scene, and every copy, is gone).</summary>
        internal static void Clear()
        {
            foreach (CustomCreature creature in byName.Values)
            {
                foreach (GameObject part in creature.Parts)
                {
                    Destroy(part);
                }
                Destroy(creature.Prefab);
            }
            byName.Clear();
            byHash.Clear();
        }

        private static void Destroy(GameObject? prefab)
        {
            if (prefab != null)
            {
                Object.Destroy(prefab);
            }
        }
    }
}
