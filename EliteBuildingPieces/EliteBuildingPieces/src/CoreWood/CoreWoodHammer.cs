using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteBuildingPieces.CoreWood
{
    /// <summary>
    /// The core wood pieces in the hammer: their cost in core wood and their place in the Building tab, right after the
    /// game's wood gate (at the end when it is missing), while the switch is on. Refreshed when the item database or the
    /// scene wakes and when the switch changes. Idempotent; every peer.
    /// </summary>
    [HarmonyPatch]
    public static class CoreWoodHammer
    {
        private const string Hammer = "Hammer";
        private const string CoreWood = "RoundLog";

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
                Plugin.Log.LogError($"EliteBuildingPieces: the core wood pieces could not be put in the hammer: {e.Message}");
            }
        }

        private static void Apply(ObjectDB database)
        {
            PieceTable table = HammerTable(database);
            if (table == null)
                return;
            GameObject wood = database.GetItemPrefab(CoreWood);
            foreach (CoreWoodPiece def in CoreWoodPieces.All)
            {
                if (def.Built == null)
                    continue;
                table.m_pieces.Remove(def.Built);
                Configure(def.Built.GetComponent<Piece>(), def.Cost, wood != null ? wood.GetComponent<ItemDrop>() : null);
            }
            if (CoreWoodSettings.On)
                Insert(table.m_pieces);
        }

        private static void Configure(Piece piece, int cost, ItemDrop wood)
        {
            piece.m_category = Piece.PieceCategory.BuildingWorkbench;
            if (wood != null)
                piece.m_resources = new[] { new Piece.Requirement { m_resItem = wood, m_amount = cost, m_recover = true } };
        }

        private static void Insert(List<GameObject> pieces)
        {
            int at = pieces.FindIndex(p => p != null && p.name == CoreWoodPieces.Gate);
            at = at < 0 ? pieces.Count : at + 1;
            foreach (CoreWoodPiece def in CoreWoodPieces.All)
            {
                if (def.Built != null)
                    pieces.Insert(at++, def.Built);
            }
        }

        private static PieceTable HammerTable(ObjectDB database)
        {
            GameObject hammer = database != null ? database.GetItemPrefab(Hammer) : null;
            ItemDrop item = hammer != null ? hammer.GetComponent<ItemDrop>() : null;
            return item != null ? item.m_itemData.m_shared.m_buildPieces : null;
        }
    }
}
