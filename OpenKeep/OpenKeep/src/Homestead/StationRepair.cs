using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Everything the station just opened can repair, on the local player's client, through the game's own repair
    /// button logic: <c>InventoryGui.HaveRepairableItems</c> decides whether anything is due (a station, usable here:
    /// <c>CheckUsable</c>'s roof and fire rules) and <c>InventoryGui.RepairOneItem</c> is called once per item due, exactly
    /// what one press of the button does (the first worn item <c>CanRepair</c> accepts: repairable, made or repaired at a
    /// station of this name or from a lower world level, the station's level, capped at 4 as in the game, at least the
    /// recipe's; the Crafting skill raised by the durability restored; the durability set to full). The button is only
    /// shown where the station's <c>m_canRepair</c> is set, so nothing happens elsewhere. The game plays the station's
    /// repair effect and says "repaired" per press; the effect is kept to the first press (the station's effect list is
    /// swapped for an empty one for the rest and put back after), and one centre message with the count replaces the
    /// game's. Nothing is sent over the network: durability and skills live in the player's own inventory and profile,
    /// and the game's button sends nothing either.
    /// </summary>
    public static class StationRepair
    {
        private static readonly EffectList Silent = new EffectList();

        /// <summary>After the game opened <paramref name="station"/> for <paramref name="player"/>.</summary>
        public static void Run(Player player, CraftingStation station)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || !Applies(gui, player, station))
                return;
            int repaired = RepairEach(gui, player, station);
            if (repaired == 0)
                return;
            gui.UpdateCraftingPanel();
            Messages.Center(repaired == 1 ? StationRepairFeature.RepairedOne : string.Format(Language.Localize(StationRepairFeature.RepairedMany), repaired));
            Plugin.Log.LogInfo($"OpenKeep: opening {Language.Localize(station.m_name)} repaired {repaired} items");
        }

        /// <summary>The setting is on, the station is the local player's current one, the game shows its repair button there and something is due.</summary>
        private static bool Applies(InventoryGui gui, Player player, CraftingStation station)
        {
            if (!StationRepairSettings.AutoRepair.Value || player == null || player != Player.m_localPlayer)
                return false;
            if (station == null || player.GetCurrentCraftingStation() != station || !station.m_canRepair)
                return false;
            return gui.HaveRepairableItems();
        }

        /// <summary>One <c>RepairOneItem</c> per item due, stopping when a press repairs nothing. Returns the items repaired.</summary>
        private static int RepairEach(InventoryGui gui, Player player, CraftingStation station)
        {
            List<ItemDrop.ItemData> due = Due(gui, player);
            EffectList effect = station.m_repairItemDoneEffects;
            int repaired = 0;
            try
            {
                for (int i = 0; i < due.Count; i++)
                {
                    int worn = Worn(due);
                    gui.RepairOneItem();
                    if (Worn(due) >= worn)
                        break;
                    repaired++;
                    station.m_repairItemDoneEffects = Silent;
                }
            }
            finally
            {
                station.m_repairItemDoneEffects = effect;
            }
            return repaired;
        }

        /// <summary>The worn items of the inventory the game's <c>CanRepair</c> accepts here, in the game's order.</summary>
        private static List<ItemDrop.ItemData> Due(InventoryGui gui, Player player)
        {
            List<ItemDrop.ItemData> worn = new List<ItemDrop.ItemData>();
            player.GetInventory().GetWornItems(worn);
            worn.RemoveAll(item => !gui.CanRepair(item));
            return worn;
        }

        private static int Worn(List<ItemDrop.ItemData> items)
        {
            int count = 0;
            foreach (ItemDrop.ItemData item in items)
            {
                if (item.m_durability < item.GetMaxDurability())
                    count++;
            }
            return count;
        }
    }
}
