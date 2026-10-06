using HarmonyLib;
using PackPanel.Backpacks;
using PackPanel.Consume;
using PackPanel.Tackle;
using PackPanel.Worn;

namespace PackPanel.Core
{
    /// <summary>
    /// The local player's frame (<c>Player.Update</c> postfix): a layout change waiting from this frame's settings is
    /// applied once (several keys changed together, as a server's reloaded file does, give one layout, not one per key),
    /// a backpack's slots follow it into and out of its slot (<see cref="BackpackWear"/>), a tacklebox's cells follow it
    /// (<see cref="TackleboxWear"/>), the Food Key and the Mead Slot keys are read (<see cref="ConsumeKeys"/>), keys never carried before are noticed (<see cref="Ring.KeyRingNews"/>), extra utilities
    /// that left without being taken off are forgotten, and with Auto Equip the gear slots are kept worn (<see cref="GearKeep"/>).
    /// The backpack, tacklebox and Auto Equip checks run when something may have changed (<see cref="WearGate"/>).
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class PlayerTick
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;
            InventoryModule.ApplyPending(__instance);
            bool wear = WearGate.Due(__instance);
            if (wear)
            {
                BackpackWear.Tick(__instance);
                TackleboxWear.Tick(__instance);
            }
            ConsumeKeys.Tick(__instance);
            Ring.KeyRingNews.Tick(__instance);
            if (!InventoryState.IsLocal(__instance))
                return;
            ExtraUtilities.Prune(__instance);
            if (wear)
                GearKeep.Tick(__instance);
        }
    }
}
