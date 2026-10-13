using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Helgrind in the hammer (features/arena.md section 2): in the Misc tab, built at the Black Forge, while the `Arena`
    /// setting is on; out of it while it is off (gates already built stay, closed). An endgame build from the Deep North:
    /// Kall Fimbulbringer's Sacrificial Blood, two Frostcores, ten Bloodgold bars and five Memorial Coal, the parts the gate
    /// is made of. Put in every ObjectDB the game builds or copies, once the prefab exists, and again whenever the setting
    /// changes - the server's value arriving after a player joins included. Every peer; idempotent. As the Raiders Chest's
    /// (<see cref="Raids.ChestHammer"/>).
    /// </summary>
    [HarmonyPatch]
    internal static class GateHammer
    {
        private const string Hammer = "Hammer";
        private const string Forge = "blackforge";

        private static readonly (string Item, int Amount)[] Cost =
        {
            ("FrozenKingDrop", 1), ("FrostCore", 2), ("Gold", 10), ("MemorialCoal", 5),
        };

        private static bool _listening;

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void DatabaseBuilt(ObjectDB __instance) =>
            SafeCall.Run("Helgrind in the hammer", static db => Refresh(db), __instance);

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void DatabaseCopied(ObjectDB __instance) =>
            SafeCall.Run("Helgrind in the hammer", static db => Refresh(db), __instance);

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        [HarmonyPostfix]
        private static void SceneAwake() =>
            SafeCall.Run("Helgrind in the hammer", static db => Refresh(db), ObjectDB.instance);

        /// <summary>The gate in or out of the hammer, with its cost and the Black Forge, as the setting says now.</summary>
        public static void Refresh(ObjectDB? db)
        {
            Listen();
            GameObject? gate = GatePrefab.Prefab;
            List<GameObject>? pieces = HammerPieces(db);
            if (db == null || gate == null || pieces == null)
            {
                return;
            }
            Piece piece = gate.GetComponent<Piece>();
            piece.m_resources = Requirements(db);
            CraftingStation? forge = Station();
            if (forge != null)
            {
                piece.m_craftingStation = forge;
            }
            pieces.Remove(gate);
            if (ArenaSettings.Enabled)
            {
                pieces.Add(gate);
            }
        }

        private static void Listen()
        {
            if (_listening || ArenaSettings.Arena == null)
            {
                return;
            }
            _listening = true;
            ArenaSettings.Arena.SettingChanged += (_, _) => SafeCall.Run("Helgrind setting", Changed);
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
            GameObject? forge = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Forge) : null;
            return forge != null ? forge.GetComponent<CraftingStation>() : null;
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
                    Log.Warn($"Helgrind: the game has no {name}; it is left out of the cost.");
                    continue;
                }
                made.Add(new Piece.Requirement { m_resItem = item, m_amount = amount, m_recover = true });
            }
            return made.ToArray();
        }
    }
}
