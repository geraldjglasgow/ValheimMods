using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Rendering
{
    /// <summary>
    /// The Rendering Table in the hammer: the Crafting tab right after the workbench, built at a workbench, while the
    /// `Rendering Table` setting is on; out of it while it is off (tables already built stay and render nothing). Its cost
    /// is early, since every biome's trophies render: ten wood like the workbench, five flint for the scrapers and two
    /// leather scraps. Put in every ObjectDB the game builds or copies, once the prefab exists, and again whenever the
    /// setting changes - the server's value arriving after a player joins included. Every peer; idempotent.
    /// </summary>
    [HarmonyPatch]
    internal static class RenderHammer
    {
        private const string Hammer = "Hammer";
        private const string Workbench = "piece_workbench";

        private static readonly (string Item, int Amount)[] Cost = { ("Wood", 10), ("Flint", 5), ("LeatherScraps", 2) };

        private static bool _listening;

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void DatabaseBuilt(ObjectDB __instance) =>
            SafeCall.Run("Rendering Table in the hammer", static db => Refresh(db), __instance);

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void DatabaseCopied(ObjectDB __instance) =>
            SafeCall.Run("Rendering Table in the hammer", static db => Refresh(db), __instance);

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        [HarmonyPostfix]
        private static void SceneAwake() =>
            SafeCall.Run("Rendering Table in the hammer", static db => Refresh(db), ObjectDB.instance);

        /// <summary>The table in or out of the hammer, with its cost and workbench, as the setting says now.</summary>
        public static void Refresh(ObjectDB? db)
        {
            Listen();
            GameObject? table = RenderPrefab.Prefab;
            List<GameObject>? pieces = HammerPieces(db);
            if (db == null || table == null || pieces == null)
            {
                return;
            }
            Piece piece = table.GetComponent<Piece>();
            piece.m_resources = Requirements(db);
            GameObject? bench = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Workbench) : null;
            if (bench != null)
            {
                piece.m_craftingStation = bench.GetComponent<CraftingStation>();
            }
            pieces.Remove(table);
            if (TableSettings.TableEnabled)
            {
                pieces.Insert(bench != null && pieces.Contains(bench) ? pieces.IndexOf(bench) + 1 : pieces.Count, table);
            }
        }

        private static void Listen()
        {
            if (_listening || TableSettings.RenderingTable == null)
            {
                return;
            }
            _listening = true;
            TableSettings.RenderingTable.SettingChanged += (_, _) => SafeCall.Run("Rendering Table setting", Changed);
        }

        // The local player's build menu lists the hammer's pieces as it last read them: read them again.
        private static void Changed()
        {
            Refresh(ObjectDB.instance);
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                player.UpdateAvailablePiecesList();
            }
        }

        private static List<GameObject>? HammerPieces(ObjectDB? db)
        {
            GameObject? hammer = db != null ? db.GetItemPrefab(Hammer) : null;
            ItemDrop? item = hammer != null ? hammer.GetComponent<ItemDrop>() : null;
            return item != null ? item.m_itemData?.m_shared?.m_buildPieces?.m_pieces : null;
        }

        private static Piece.Requirement[] Requirements(ObjectDB db)
        {
            List<Piece.Requirement> made = new List<Piece.Requirement>();
            foreach ((string name, int amount) in Cost)
            {
                GameObject? prefab = db.GetItemPrefab(name);
                ItemDrop? item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (item == null)
                {
                    Log.Warn($"Rendering Table: the game has no {name}; it is left out of the cost.");
                    continue;
                }
                made.Add(new Piece.Requirement { m_resItem = item, m_amount = amount, m_recover = true });
            }
            return made.ToArray();
        }
    }
}
