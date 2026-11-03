using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cargo crane and the cargo crate in the hammer's Misc tab, built at a workbench, Meadows tier: the crane of
    /// wood, stone, deer hide and leather scraps, the crate of wood and leather scraps. Refreshed when the item database
    /// wakes and after the pieces are made, whichever comes last. Idempotent.
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    public static class KwCargoHammer
    {
        private static readonly (string Item, int Amount)[] CraneCost = { ("Wood", 30), ("Stone", 10), ("DeerHide", 4), ("LeatherScraps", 6) };
        private static readonly (string Item, int Amount)[] CrateCost = { ("Wood", 10), ("LeatherScraps", 4) };

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix() => Refresh();

        public static void Refresh() => Guard.Run("Broadside cargo hammer", Apply);

        private static void Apply()
        {
            ObjectDB database = ObjectDB.instance;
            GameObject? hammer = database != null ? database.GetItemPrefab("Hammer") : null;
            PieceTable? table = hammer != null ? hammer.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces : null;
            if (table == null || database == null)
                return;
            CraftingStation? workbench = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("piece_workbench")?.GetComponent<CraftingStation>() : null;
            Add(table, KwCargoPrefabs.Crane, CraneCost, database, workbench);
            Add(table, KwCargoPrefabs.Crate, CrateCost, database, workbench);
        }

        private static void Add(PieceTable table, GameObject? prefab, (string Item, int Amount)[] cost, ObjectDB database, CraftingStation? workbench)
        {
            if (prefab == null)
                return;
            Piece piece = prefab.GetComponent<Piece>();
            piece.m_resources = Requirements(cost, database) ?? piece.m_resources;
            if (workbench != null)
                piece.m_craftingStation = workbench;
            if (!table.m_pieces.Contains(prefab))
                table.m_pieces.Add(prefab);
        }

        // The cost as the game's requirements, or null while the database lacks any of the items.
        private static Piece.Requirement[]? Requirements((string Item, int Amount)[] cost, ObjectDB database)
        {
            Piece.Requirement[] made = new Piece.Requirement[cost.Length];
            for (int i = 0; i < cost.Length; i++)
            {
                ItemDrop? item = database.GetItemPrefab(cost[i].Item)?.GetComponent<ItemDrop>();
                if (item == null)
                    return null;
                made[i] = new Piece.Requirement { m_resItem = item, m_amount = cost[i].Amount, m_recover = true };
            }
            return made;
        }
    }
}
