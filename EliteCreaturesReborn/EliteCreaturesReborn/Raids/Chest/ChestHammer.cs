using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The Raiders Chest in the hammer (features/raids.md sections 2 and 6): in the Misc tab, built at a workbench, while
    /// the `Raiders Chest` setting is on; out of it while it is off (chests already built stay, as coin chests). Its cost is
    /// early: raids unlock after Eikthyr, so the horn is his hard antler, with ten wood for the chest and four leather scraps
    /// for the horn's lashings - in spirit the reinforced chest's ten wood and two metal, without metal a player in leather
    /// has not got yet. Put in every ObjectDB the game builds or copies, once the prefab exists, and again whenever the
    /// setting changes - the server's value arriving after a player joins included. Every peer; idempotent.
    /// </summary>
    [HarmonyPatch]
    internal static class ChestHammer
    {
        private const string Hammer = "Hammer";
        private const string Workbench = "piece_workbench";

        private static readonly (string Item, int Amount)[] Cost = { ("Wood", 10), ("LeatherScraps", 4), ("HardAntler", 1) };

        private static bool _listening;

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void DatabaseBuilt(ObjectDB __instance) =>
            SafeCall.Run("Raiders Chest in the hammer", static db => Refresh(db), __instance);

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void DatabaseCopied(ObjectDB __instance) =>
            SafeCall.Run("Raiders Chest in the hammer", static db => Refresh(db), __instance);

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        [HarmonyPostfix]
        private static void SceneAwake() =>
            SafeCall.Run("Raiders Chest in the hammer", static db => Refresh(db), ObjectDB.instance);

        /// <summary>The chest in or out of the hammer, with its cost and workbench, as the setting says now.</summary>
        public static void Refresh(ObjectDB? db)
        {
            Listen();
            GameObject? chest = ChestPrefab.Prefab;
            List<GameObject>? pieces = HammerPieces(db);
            if (db == null || chest == null || pieces == null)
            {
                return;
            }
            Piece piece = chest.GetComponent<Piece>();
            piece.m_resources = Requirements(db);
            CraftingStation? bench = Station();
            if (bench != null)
            {
                piece.m_craftingStation = bench;
            }
            pieces.Remove(chest);
            if (RaidSettings.ChestEnabled)
            {
                pieces.Add(chest);
            }
        }

        private static void Listen()
        {
            if (_listening || RaidSettings.RaidersChest == null)
            {
                return;
            }
            _listening = true;
            RaidSettings.RaidersChest.SettingChanged += (_, _) => SafeCall.Run("Raiders Chest setting", Changed);
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

        private static CraftingStation? Station()
        {
            GameObject? bench = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Workbench) : null;
            return bench != null ? bench.GetComponent<CraftingStation>() : null;
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
                    Log.Warn($"Raiders Chest: the game has no {name}; it is left out of the cost.");
                    continue;
                }
                made.Add(new Piece.Requirement { m_resItem = item, m_amount = amount, m_recover = true });
            }
            return made.ToArray();
        }
    }
}
