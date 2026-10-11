using System.Collections.Generic;
using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// Every peer, as the game attaches an item's model to a character (a weapon, a shield, a helmet, hair, a beard): one
    /// on a creature whose definition tints its items is drawn in that tint (<see cref="ItemTints"/>). Runs only when
    /// equipment changes, and ends at one count test while no tinted creature is loaded. A failure is reported and
    /// swallowed: the item keeps its own colours.
    /// </summary>
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachItem))]
    internal static class ItemTintAttachPatch
    {
        private static void Postfix(VisEquipment __instance, int itemHash, GameObject __result)
        {
            if (ItemTints.Empty || __result == null)
            {
                return;
            }
            SafeCall.Run("VisEquipment.AttachItem (custom creature item tint)",
                static (vis, hash, item) => ItemTints.Attached(vis, hash, item), __instance, itemHash, __result);
        }
    }

    /// <summary>Every peer, as the game attaches armour, a cape or a utility item: as <see cref="ItemTintAttachPatch"/>.</summary>
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachArmor))]
    internal static class ItemTintArmorPatch
    {
        private static void Postfix(VisEquipment __instance, List<GameObject> __result)
        {
            if (ItemTints.Empty || __result == null)
            {
                return;
            }
            SafeCall.Run("VisEquipment.AttachArmor (custom creature item tint)",
                static (vis, items) => ItemTints.Attached(vis, items), __instance, __result);
        }
    }
}
