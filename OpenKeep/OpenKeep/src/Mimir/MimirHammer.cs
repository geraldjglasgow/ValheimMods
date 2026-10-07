using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// Mímir's Chest in the hammer: its cost, and its place in the hammer's table while the switch is on. Refreshed when
    /// the item database or the scene wakes and when the switch changes. Idempotent; every peer.
    /// </summary>
    [HarmonyPatch]
    public static class MimirHammer
    {
        private const string Hammer = "Hammer";

        // Mountain materials: a chest that never fills comes after the reinforced chest, not with it.
        private static readonly (string Item, int Amount)[] Cost = { ("FineWood", 10), ("Silver", 4), ("IronNails", 10), ("LeatherScraps", 6) };

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void DatabaseAwake() => Refresh();

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        [HarmonyPostfix]
        private static void SceneAwake() => Refresh();

        public static void Refresh()
        {
            try
            {
                Apply(ObjectDB.instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"OpenKeep: Mímir's Chest could not be put in the hammer: {e.Message}");
            }
        }

        private static void Apply(ObjectDB database)
        {
            Piece piece = MimirPrefab.Piece;
            PieceTable table = HammerTable(database);
            if (piece == null || table == null)
                return;
            piece.m_resources = Requirements(database);
            bool wanted = MimirSettings.Enabled.Value;
            if (wanted && !table.m_pieces.Contains(piece.gameObject))
                table.m_pieces.Add(piece.gameObject);
            else if (!wanted)
                table.m_pieces.Remove(piece.gameObject);
        }

        private static PieceTable HammerTable(ObjectDB database)
        {
            GameObject hammer = database != null ? database.GetItemPrefab(Hammer) : null;
            ItemDrop item = hammer != null ? hammer.GetComponent<ItemDrop>() : null;
            return item != null ? item.m_itemData.m_shared.m_buildPieces : null;
        }

        private static Piece.Requirement[] Requirements(ObjectDB database)
        {
            List<Piece.Requirement> list = new List<Piece.Requirement>();
            foreach ((string item, int amount) in Cost)
            {
                GameObject prefab = database.GetItemPrefab(item);
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop != null)
                    list.Add(new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = true });
            }
            return list.ToArray();
        }
    }
}
