using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>CraftingStation.Interact(Humanoid, bool, bool)</c>: on the local player's client, within use distance and with
    /// the station usable (<c>CheckUsable</c>), the game makes it the player's current station and opens the inventory on
    /// it; it always returns false. The postfix runs when the station is the local player's current one afterwards, so
    /// a press another mod took over (a prefix skipping the game's) or a refusal repairs nothing. Carts with a workbench
    /// open theirs through this same method (<c>Carts.CartInteractPatch</c>), so they repair like a workbench of their level.
    /// </summary>
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
    public static class StationRepairPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CraftingStation __instance, Humanoid user, bool repeat)
        {
            if (repeat || user == null || user != Player.m_localPlayer)
                return;
            StationRepair.Run((Player)user, __instance);
        }
    }
}
