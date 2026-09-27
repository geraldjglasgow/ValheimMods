using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The stars of what a mill has queued, on its owner. The game queues item names in the mill's ZDO (item0..itemN,
    /// s_queued); beside them <see cref="Keys.MillStars"/> holds one digit per item, aligned to the end of the queue, so
    /// items queued before (without a digit) count as 0 stars.
    /// <list type="bullet">
    /// <item>Smelter.QueueOre (from RPC_AddOre) appends the stars being received (<see cref="MillReceive"/>).</item>
    /// <item>Smelter.RemoveOneOre takes the front item: its digit becomes <see cref="LastTaken"/>, for the product made
    /// from it or the item dropped when the mill breaks (<see cref="MillSpawn"/>).</item>
    /// </list>
    /// Only star mills (<see cref="CropStarItems.IsStarMill"/>: the windmill) keep the digits, always, so the queue stays
    /// aligned whatever the settings; stars are the items' own, like their stacking.
    /// </summary>
    internal static class MillQueue
    {
        private static readonly int StarsHash = Keys.MillStars.GetStableHashCode();

        /// <summary>The stars of the item RemoveOneOre took last; 0 for an item without a digit.</summary>
        public static int LastTaken { get; private set; }

        /// <summary>On the owner of a star mill: the mill's ZDO; null otherwise.</summary>
        public static ZDO Tracked(Smelter smelter)
        {
            ZNetView nview = smelter != null ? smelter.m_nview : null;
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || !CropStarItems.IsStarMill(Utils.GetPrefabName(smelter.gameObject)))
                return null;
            return nview.GetZDO();
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.QueueOre))]
        private static class Queued
        {
            [HarmonyPostfix]
            private static void Postfix(Smelter __instance)
            {
                ZDO zdo = Tracked(__instance);
                if (zdo != null)
                    Push(zdo, __instance.GetQueueSize(), MillReceive.Receiving);
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.RemoveOneOre))]
        private static class Taken
        {
            [HarmonyPrefix]
            private static void Prefix(Smelter __instance)
            {
                LastTaken = 0;
                ZDO zdo = Tracked(__instance);
                int size = zdo != null ? __instance.GetQueueSize() : 0;
                if (size > 0)
                    LastTaken = Pop(zdo, size);
            }
        }

        /// <summary>Appends the new last item's digit, keeping at most one digit per queued item.</summary>
        private static void Push(ZDO zdo, int size, int stars)
        {
            string digits = zdo.GetString(StarsHash, "");
            if (digits.Length > size - 1)
                digits = digits.Substring(digits.Length - Mathf.Max(0, size - 1));
            zdo.Set(StarsHash, digits + (char)('0' + Mathf.Clamp(stars, 0, Stars.Max)));
        }

        /// <summary>Removes the front item's digit, if it has one, and returns its stars.</summary>
        private static int Pop(ZDO zdo, int size)
        {
            string digits = zdo.GetString(StarsHash, "");
            int front = digits.Length - size;
            if (front < 0)
                return 0;
            int stars = Mathf.Clamp(digits[front] - '0', 0, Stars.Max);
            zdo.Set(StarsHash, digits.Substring(front + 1));
            return stars;
        }
    }
}
