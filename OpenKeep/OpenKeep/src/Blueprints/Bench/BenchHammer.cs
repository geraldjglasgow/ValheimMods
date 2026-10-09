using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The Blueprint Bench in the hammer's Crafting tab: its cost, and its place in the hammer's table while blueprints are
    /// on (<see cref="BlueprintSettings.Enabled"/>). Refreshed when the item database or the scene wakes and when the switch
    /// changes (the server's value arriving included). Idempotent; every peer. Benches already built stay when the switch
    /// goes off; they only refuse to open.
    /// </summary>
    [HarmonyPatch]
    public static class BenchHammer
    {
        private const string Hammer = "Hammer";

        // Half the workbench's 10 Wood, as it is half the size; built near a workbench.
        private static readonly (string Item, int Amount)[] Cost = { ("Wood", 5) };

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
                Plugin.Log.LogError($"OpenKeep: the Blueprint Bench could not be put in the hammer: {e.Message}");
            }
        }

        private static void Apply(ObjectDB database)
        {
            Piece piece = BenchPrefab.Piece;
            PieceTable table = HammerTable(database);
            if (piece == null || table == null)
                return;
            piece.m_resources = Requirements(database);
            bool wanted = BlueprintSettings.Enabled;
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
