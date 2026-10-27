using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Broadside's pieces at the end of the hammer's Misc tab, in build order (<see cref="Steps"/>), every one free
    /// (<see cref="KwPrefabs"/>: design mode); the generated parts the tools make are never in it. Then the ship cannons,
    /// at their own cost (<see cref="KwCannonHammer"/>). The saved ships come
    /// after them, put in by <see cref="KwBpHammer"/>, each costing what its parts do. Refreshed when the item database
    /// wakes and after the pieces are made, whichever comes last. Idempotent.
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    public static class KwHammer
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix() => Refresh();

        public static void Refresh() => Guard.Run("Broadside hammer", Apply);

        private static void Apply()
        {
            ObjectDB database = ObjectDB.instance;
            GameObject? hammer = database != null ? database.GetItemPrefab("Hammer") : null;
            PieceTable? table = hammer != null ? hammer.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces : null;
            if (table == null || KwPrefabs.All.Count == 0)
                return;
            List<GameObject> pieces = table.m_pieces;
            // the old post cap pieces stay registered for the caps already built, out of the hammer: the Post cap tool makes caps now;
            // so do the rudders past the Black Forest, for those built or saved: only Meadows and Black Forest are offered yet
            foreach (GameObject prefab in KwPrefabs.All.Where(p => !p.name.StartsWith(KwToolPieces.PartPrefix) && !p.name.StartsWith(KwCaps.Prefix) && !KwRudderTiers.Held(p.name)).OrderBy(p => Order(p.name)).ThenBy(p => p.name))
            {
                if (!pieces.Contains(prefab))
                    pieces.Add(prefab);
            }
            KwCannonHammer.Add(database!, pieces);
            Log.Info($"Broadside: {KwPrefabs.All.Count} pieces and {KwCannonHammer.Offered} of {KwCannonPrefabs.All.Count} cannons in the hammer's Misc tab.");
        }

        // The build order: the dock (keel block guide, keel block), the keel tool, the name board, the rib tool, the
        // planking, deck and railing tools, hatches and the ladder tool, the Cabin, Doorway and Window tools, the rudders biome by
        // biome, the captain's wheel, the sail, bowsprit and Post cap tools, the anchor windlass, the blueprint table, then the saved ships.
        private static readonly string[] Steps =
        {
            "KW_dockguide", "KW_keelcradle", "KW_tool_keel", KwNameBoard.Prefab, "KW_tool_rib", "KW_tool_planking", "KW_tool_deck", "KW_tool_rail",
            "KW_hatch_", "KW_tool_ladder", "KW_tool_cabin", "KW_tool_doorway", "KW_tool_window", "KW_rudder_meadows", "KW_rudder_blackforest", "KW_rudder_swamp", "KW_rudder_mountains",
            "KW_rudder_plains", "KW_rudder_mistlands", "KW_rudder_ashlands", KwWheel.Prefab, "KW_tool_sail", "KW_tool_bowsprit", "KW_tool_cap", KwAnchor.Prefab, KwBlueprintStation.Prefab, KwBlueprintStation.EntryPrefix,
        };

        private static int Order(string name)
        {
            int step = System.Array.FindIndex(Steps, s => name.StartsWith(s, System.StringComparison.Ordinal));
            return step < 0 ? Steps.Length : step;
        }
    }
}
