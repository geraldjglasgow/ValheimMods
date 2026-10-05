using Hotkeys;
using PackPanel.Core;
using PackPanel.Slots;

namespace PackPanel.Consume
{
    /// <summary>
    /// The Food Key, the Mead Key and the Mead Slot keys (<see cref="MeadSlotKeys"/>), read in the local player's frame
    /// (<see cref="PlayerTick"/>). They work where the game's hotbar keys work (<c>Player.TakeInput</c>: no inventory, map,
    /// menu, chat, console or text input open, not dead or teleporting) and nowhere else, so inside the inventory Z stays
    /// OpenKeep's Find Key. Not with a hammer, hoe or cultivator in hand either (<c>Player.InPlaceMode</c>; the user's
    /// request, 2026-10-04): there B is OpenKeep's build camera and EarthWright's Select Value Key, Z EarthWright's Snap
    /// Hold Key. A Food or Mead Key press eats or drinks everything it can from its slots (<see cref="SlotMeals"/>); when
    /// nothing could be taken the centre message says so. A Mead Slot key drinks from its one slot.
    /// </summary>
    public static class ConsumeKeys
    {
        public static void Tick(Player player)
        {
            if (!Works(player))
                return;
            bool food = Hotkey.Pressed(ConsumeSettings.FoodKey);
            bool mead = Hotkey.Pressed(ConsumeSettings.MeadKey);
            int slot = MeadSlotKeys.Pressed();
            if ((!food && !mead && slot < 0) || !Ready(player))
                return;
            if (food && SlotMeals.TakeAll(player, SlotKind.Food) == 0)
                Messages.Center(ConsumeWords.NothingToEat);
            if (mead && SlotMeals.TakeAll(player, SlotKind.Mead) == 0)
                Messages.Center(ConsumeWords.NothingToDrink);
            if (slot >= 0)
                MeadSlotKeys.Drink(player, slot);
        }

        /// <summary>The local player, with the module on: the keys are read at all.</summary>
        public static bool Works(Player player) => InventoryState.IsLocal(player) && InventoryState.Active;

        /// <summary>A pressed key acts: where the hotbar keys work, with no build tool in hand.</summary>
        public static bool Ready(Player player) => player.TakeInput() && !player.InPlaceMode();
    }
}
