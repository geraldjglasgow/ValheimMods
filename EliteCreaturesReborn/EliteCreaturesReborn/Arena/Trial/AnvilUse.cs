using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Using Hel's Anvil (features/arena.md section 3, "Hel's Anvil"), on the player's own machine: during the countdown
    /// or a break, a player of the trial with the souls for it and something worn asks the core's owner
    /// (<see cref="AnvilOwner"/>, which keeps the souls). On its yes, every worn item the player carries - worn or in the
    /// bags - is mended to full durability as the game mends at a station, with the game's repair effect and sound (a
    /// forge's) at the anvil; the core's owner has every player of the trial see cold soul-fire flare there.
    /// </summary>
    internal static class AnvilUse
    {
        private static readonly List<ItemDrop.ItemData> Worn = new List<ItemDrop.ItemData>();
        private static Vector3 _askedAt;

        /// <summary>The player presses E at an anvil; true when the press was taken.</summary>
        public static bool Ask(Vector3 anvil)
        {
            Player player = Player.m_localPlayer;
            ArenaCore? core = ArenaCore.Current;
            ZDO? zdo = core != null ? core.Zdo : null;
            if (player == null || zdo == null || !HallPlayers.LocalInHall())
            {
                return false;
            }
            long me = ZDOMan.GetSessionID();
            int cost = AnvilOwner.Cost(zdo, me, out bool open);
            if (!open)
            {
                return false;
            }
            string? refusal = Refusal(zdo, me, cost, player);
            if (refusal != null)
            {
                ArenaWords.Show(refusal, cost.ToString(), "", "");
                return true;
            }
            _askedAt = anvil;
            core!.View.InvokeRPC(ArenaKeys.RepairRpc, anvil);
            return true;
        }

        /// <summary>The owner's answer, routed to this player.</summary>
        public static void OnAnswer(long sender, bool paid, int cost) => Guard.Run("Hel's Anvil answer", () =>
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            if (!paid)
            {
                ArenaWords.Show(ArenaWords.AnvilPoor, cost.ToString(), "", "");
                return;
            }
            MendAll(player);
            RepairEffect(_askedAt);
            ArenaWords.Show(ArenaWords.AnvilDone, cost.ToString(), "", "");
        });

        // Why the anvil will not mend for this player now (too few souls, nothing worn); null when it will.
        private static string? Refusal(ZDO core, long me, int cost, Player player)
        {
            if (SoulWallets.Souls(core, me) < cost)
            {
                return ArenaWords.AnvilPoor;
            }
            return AnyWorn(player) ? null : ArenaWords.AnvilNothing;
        }

        private static bool AnyWorn(Player player)
        {
            Worn.Clear();
            player.GetInventory().GetWornItems(Worn);
            bool any = Worn.Count > 0;
            Worn.Clear();
            return any;
        }

        // Every worn item to full durability, as the game's repair at a station.
        private static void MendAll(Player player)
        {
            Worn.Clear();
            player.GetInventory().GetWornItems(Worn);
            foreach (ItemDrop.ItemData item in Worn)
            {
                item.m_durability = item.GetMaxDurability();
            }
            Worn.Clear();
        }

        // The game's own repair effect and sound, a forge's, at the anvil.
        private static void RepairEffect(Vector3 at)
        {
            GameObject? forge = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("forge") : null;
            CraftingStation? station = forge != null ? forge.GetComponent<CraftingStation>() : null;
            if (station != null)
            {
                station.m_repairItemDoneEffects.Create(at, Quaternion.identity);
            }
        }
    }
}
