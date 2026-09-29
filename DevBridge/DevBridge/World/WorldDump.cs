using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.World
{
    /// <summary>JSON-ready summaries of networked objects and of everything a ZDO stores.</summary>
    internal static class WorldDump
    {
        internal static Dictionary<string, object> Summary(ZNetView view, float distance)
        {
            ZDO zdo = view.GetZDO();
            var info = new Dictionary<string, object>
            {
                ["prefab"] = Utils.GetPrefabName(view.gameObject),
                ["id"] = zdo.m_uid.ToString(),
                ["distance"] = Fmt.R(distance),
                ["position"] = Fmt.V3(view.transform.position),
                ["owner"] = zdo.IsOwner() ? "me" : zdo.GetOwner().ToString(),
            };
            AddCharacter(info, view.GetComponent<Character>());
            AddItem(info, view.GetComponent<ItemDrop>());
            AddContainer(info, view.GetComponent<Container>());
            AddPiece(info, view.GetComponent<Piece>());
            return info;
        }

        private static void AddCharacter(Dictionary<string, object> info, Character character)
        {
            if (!character) return;
            info["name"] = Localize(character.m_name);
            info["level"] = character.GetLevel();
            info["health"] = $"{character.GetHealth():0.#}/{character.GetMaxHealth():0.#}";
            info["tamed"] = character.IsTamed();
            info["faction"] = character.m_faction.ToString();
        }

        private static void AddItem(Dictionary<string, object> info, ItemDrop drop)
        {
            if (!drop || drop.m_itemData == null) return;
            info["item"] = Localize(drop.m_itemData.m_shared.m_name);
            info["stack"] = drop.m_itemData.m_stack;
            info["quality"] = drop.m_itemData.m_quality;
        }

        private static void AddContainer(Dictionary<string, object> info, Container container)
        {
            if (!container) return;
            info["container"] = Localize(container.m_name);
            info["items"] = container.GetInventory()?.NrOfItems() ?? 0;
        }

        private static void AddPiece(Dictionary<string, object> info, Piece piece)
        {
            if (!piece) return;
            info["piece"] = Localize(piece.m_name);
            info["creator"] = piece.GetCreator();
        }

        private static string Localize(string text) => Localization.instance != null ? Localization.instance.Localize(text) : text;

        internal static Dictionary<string, object> Zdo(ZDO zdo)
        {
            ZDOID id = zdo.m_uid;
            GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(zdo.GetPrefab()) : null;
            return new Dictionary<string, object>
            {
                ["id"] = id.ToString(),
                ["prefab"] = prefab ? prefab.name : "#" + zdo.GetPrefab(),
                ["owner"] = zdo.IsOwner() ? "me" : zdo.GetOwner().ToString(),
                ["position"] = Fmt.V3(zdo.GetPosition()),
                ["revision"] = zdo.DataRevision,
                ["floats"] = Named(ZDOExtraData.s_floats, id, v => Fmt.R(v)),
                ["ints"] = Named(ZDOExtraData.s_ints, id, v => v),
                ["longs"] = Named(ZDOExtraData.s_longs, id, v => v),
                ["strings"] = Named(ZDOExtraData.s_strings, id, v => Fmt.Clip(v, 300)),
                ["vec3"] = Named(ZDOExtraData.s_vec3, id, Fmt.V3),
                ["quaternions"] = Named(ZDOExtraData.s_quats, id, v => Fmt.V3(v.eulerAngles)),
                ["byteArrays"] = Named(ZDOExtraData.s_byteArrays, id, v => $"{v.Length} bytes"),
            };
        }

        private static SortedDictionary<string, object> Named<T>(Dictionary<ZDOID, BinarySearchDictionary<int, T>> store, ZDOID id, Func<T, object> show)
        {
            if (!store.TryGetValue(id, out BinarySearchDictionary<int, T> values)) return null;
            var named = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (KeyValuePair<int, T> pair in values) named[ZdoNames.Of(pair.Key)] = show(pair.Value);
            return named;
        }
    }
}
