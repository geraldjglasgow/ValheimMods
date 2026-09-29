using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>The game's own prefabs by name: networked ones, local ones and items, with what kind of thing each is.</summary>
    internal static class GamePrefabs
    {
        internal static GameObject Find(string name)
        {
            GameObject found = Scene().GetPrefab(name);
            if (!found && ObjectDB.instance) found = ObjectDB.instance.GetItemPrefab(name);
            return found ? found : All().FirstOrDefault(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));
        }

        internal static GameObject Require(string name) =>
            Find(name) ?? throw new BridgeException($"the game has no prefab {name}{Suggest(name)} (search with /prefabs?filter=)");

        internal static IEnumerable<GameObject> All()
        {
            ZNetScene scene = Scene();
            IEnumerable<GameObject> items = ObjectDB.instance ? ObjectDB.instance.m_items : Enumerable.Empty<GameObject>();
            return scene.m_prefabs.Concat(scene.m_nonNetViewPrefabs).Concat(items).Where(p => p).Distinct();
        }

        private static ZNetScene Scene() => ZNetScene.instance ? ZNetScene.instance : throw new BridgeException("no world loaded");

        /// <summary>creature, piece, item, sfx, vfx or other.</summary>
        internal static string Kind(GameObject prefab)
        {
            if (prefab.GetComponent<Character>()) return "creature";
            if (prefab.GetComponent<Piece>()) return "piece";
            if (prefab.GetComponent<ItemDrop>()) return "item";
            if (prefab.GetComponentInChildren<ZSFX>(true)) return "sfx";
            if (prefab.GetComponentInChildren<ParticleSystem>(true)) return "vfx";
            return "other";
        }

        private static string Suggest(string name)
        {
            List<string> close = All().Select(p => p.name).Where(n => n.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(n => n.Length).Take(8).ToList();
            return close.Count == 0 ? "" : $" (did you mean {string.Join(", ", close)}?)";
        }
    }
}
