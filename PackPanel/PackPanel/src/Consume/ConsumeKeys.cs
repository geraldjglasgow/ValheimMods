using Hotkeys;
using PackPanel.Core;
using PackPanel.Slots;

namespace PackPanel.Consume
{
    /// <summary>
    /// The Food Key and the Mead Key, read in the local player's frame (<see cref="PlayerTick"/>). They work where the game's
    /// hotbar keys work (<c>Player.TakeInput</c>: no inventory, map, menu, chat, console or text input open, not dead or
    /// teleporting) and nowhere else, so inside the inventory Z stays OpenKeep's Find Key. A press eats or drinks everything
    /// it can from its slots (<see cref="SlotMeals"/>); when nothing could be taken the centre message says so.
    /// </summary>
    public static class ConsumeKeys
    {
        public static void Tick(Player player)
        {
            if (!InventoryState.IsLocal(player) || !InventoryState.Active)
                return;
            bool food = Hotkey.Pressed(ConsumeSettings.FoodKey);
            bool mead = Hotkey.Pressed(ConsumeSettings.MeadKey);
            if ((!food && !mead) || !player.TakeInput())
                return;
            if (food && SlotMeals.TakeAll(player, SlotKind.Food) == 0)
                Messages.Center(ConsumeWords.NothingToEat);
            if (mead && SlotMeals.TakeAll(player, SlotKind.Mead) == 0)
                Messages.Center(ConsumeWords.NothingToDrink);
        }
    }
}
