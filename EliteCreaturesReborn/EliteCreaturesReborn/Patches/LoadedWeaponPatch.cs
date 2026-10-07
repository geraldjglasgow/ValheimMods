using EliteCreaturesReborn.Runtime;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Humanoid.UnequipItem forgets a player's loaded weapon (it calls ResetLoadedWeapon); the prefix notes the weapon
    /// first, while the game still names it as loaded. A failure is reported and the game unequips as before.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    public static class LoadedWeaponUnequipPatch
    {
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (item != null)
            {
                SafeCall.Run("loaded crossbow (unequip)", LoadedWeapons.Unequipping, __instance, item);
            }
        }
    }

    /// <summary>
    /// Humanoid.EquipItem has drawn a weapon: one put away loaded is loaded again, before the game's next update would
    /// queue a reload for it. A failure is reported and the weapon reloads as the game would.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    public static class LoadedWeaponEquipPatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result)
        {
            if (__result && item != null)
            {
                SafeCall.Run("loaded crossbow (equip)", LoadedWeapons.Equipped, __instance, item);
            }
        }
    }
}
