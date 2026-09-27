using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// The shovel item prefab: a copy of the hoe made once per process under an inactive holder (so it never wakes up as
    /// a world object and gets no ZDO), with its own name, icon, build table and durability. Built identically on every
    /// machine - server, host, clients and the main menu - from code alone, so the prefab name and hash always agree.
    /// The object database and the net scene each keep their own list and lookup, so it is registered into both.
    /// </summary>
    public static class ShovelPrefab
    {
        private static GameObject holder;

        public static GameObject Prefab { get; private set; }

        public static ItemDrop Drop => Prefab != null ? Prefab.GetComponent<ItemDrop>() : null;

        public static int Hash => ToolNames.Shovel.GetStableHashCode();

        /// <summary>Builds the prefab the first time the hoe is found in either list. False while there is no hoe yet.</summary>
        public static bool EnsureBuilt(IEnumerable<GameObject> first, IEnumerable<GameObject> second)
        {
            if (Prefab != null)
                return true;
            GameObject hoe = FindHoe(first) ?? FindHoe(second);
            if (hoe == null || hoe.GetComponent<ItemDrop>() == null)
                return false;
            Build(hoe);
            Plugin.Log.LogInfo("EarthWright: shovel item built from the hoe.");
            return true;
        }

        private static GameObject FindHoe(IEnumerable<GameObject> list)
        {
            if (list == null)
                return null;
            foreach (GameObject prefab in list)
            {
                if (prefab != null && prefab.name == ToolNames.Hoe)
                    return prefab;
            }
            return null;
        }

        private static void Build(GameObject hoe)
        {
            holder = new GameObject("EarthWright_Gear");
            holder.SetActive(false);
            Object.DontDestroyOnLoad(holder);
            // The parent is inactive, so the copy's ItemDrop and ZNetView never run Awake: no ZDO, no world item.
            GameObject clone = Object.Instantiate(hoe, holder.transform, false);
            clone.name = ToolNames.Shovel;
            ItemDrop.ItemData data = clone.GetComponent<ItemDrop>().m_itemData;
            data.m_dropPrefab = clone;
            data.m_customData = new Dictionary<string, string>();
            data.m_stack = 1;
            data.m_quality = 1;
            Dress(data.m_shared, hoe.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces);
            Prefab = clone;
        }

        /// <summary>The shovel's own words, icon and build table; durability and levels are written by <see cref="ToolLevels"/>.</summary>
        private static void Dress(ItemDrop.ItemData.SharedData shared, PieceTable hoeTable)
        {
            shared.m_name = GearWords.ShovelName;
            shared.m_description = GearWords.ShovelDescription;
            shared.m_canBeReparied = true;
            shared.m_useDurability = true;
            Sprite icon = ShovelIcon.Get();
            if (icon != null)
                shared.m_icons = new[] { icon };
            shared.m_buildPieces = ShovelTable.Create(hoeTable, holder.transform);
        }

        /// <summary>Adds the shovel to an object database's item list and lookups when it is not there yet.</summary>
        public static void RegisterIn(ObjectDB db)
        {
            if (Prefab == null || db == null || db.m_items.Contains(Prefab))
                return;
            db.m_items.Add(Prefab);
            db.UpdateRegisters();
        }

        /// <summary>Adds the shovel to the net scene's prefab list and lookup, so dropped shovels exist on every machine.</summary>
        public static void RegisterIn(ZNetScene scene)
        {
            if (Prefab == null || scene == null)
                return;
            if (!scene.m_prefabs.Contains(Prefab))
                scene.m_prefabs.Add(Prefab);
            scene.m_namedPrefabs[Hash] = Prefab;
        }
    }
}
