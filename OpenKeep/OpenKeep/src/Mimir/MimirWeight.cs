using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// A Mímir's Chest has no weight box (user, 2026-10-07: no weight, and then no stack count in its place either): the
    /// container panel's weight box (the "Weight" parent of InventoryGui.m_containerWeight, its icon and number) is hidden
    /// while one is open and shown again for any other container. InventoryGui.UpdateContainerWeight runs every frame
    /// while a container is open, so the box is switched only when it has to change.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "UpdateContainerWeight")]
    public static class MimirWeight
    {
        [HarmonyPostfix]
        private static void Postfix(InventoryGui __instance)
        {
            Transform box = __instance.m_containerWeight != null ? __instance.m_containerWeight.transform.parent : null;
            Container open = __instance.m_currentContainer;
            if (box == null || open == null)
                return;
            bool show = open.m_name != MimirPrefab.ContainerName;
            if (box.gameObject.activeSelf != show)
                box.gameObject.SetActive(show);
        }
    }
}
