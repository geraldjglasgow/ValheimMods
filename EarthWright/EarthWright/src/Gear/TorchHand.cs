using EarthWright.Core;
using HarmonyLib;

namespace EarthWright.Gear
{
    /// <summary>
    /// Keeps a torch in the left hand while a terrain tool is in the right one ("Torch In Left Hand"). The game treats a
    /// tool as two-handed: <c>Humanoid.EquipItem</c> unequips both hands for a tool, and a torch equipped next to a tool
    /// replaces it. Here equipping a terrain tool puts the torch that was held back into the left hand, and equipping a
    /// torch while a terrain tool is out puts the torch into the left hand and keeps the tool. Equipment is set up by the
    /// player's own client and replicated through the player's ZDO, so every player sees both hands.
    /// </summary>
    public static class TorchHand
    {
        public static bool Applies(Humanoid humanoid)
        {
            return humanoid is Player && GearSettings.TorchInLeftHand != null && GearSettings.TorchInLeftHand.Value && GeneralSettings.Active;
        }

        public static bool IsTorch(ItemDrop.ItemData item) => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch;

        /// <summary>The torch in either hand, or null.</summary>
        public static ItemDrop.ItemData HeldTorch(Humanoid humanoid)
        {
            if (IsTorch(humanoid.m_leftItem))
                return humanoid.m_leftItem;
            return IsTorch(humanoid.m_rightItem) ? humanoid.m_rightItem : null;
        }

        /// <summary>Equips the torch into the left hand next to the terrain tool. False when the game should decide instead.</summary>
        public static bool EquipLeft(Humanoid humanoid, ItemDrop.ItemData torch, bool effects)
        {
            if (!CanEquipNow(humanoid, torch))
                return false;
            if (humanoid.m_leftItem != null)
                humanoid.UnequipItem(humanoid.m_leftItem, effects);
            humanoid.m_leftItem = torch;
            torch.m_equipped = true;
            humanoid.m_hiddenLeftItem = null;
            humanoid.m_hiddenRightItem = null;
            VisEquipment vis = humanoid.m_visEquipment;
            if (vis != null && vis.m_isPlayer && FejdStartup.instance == null)
                torch.m_shared.m_equipEffect.Create(vis.m_leftHand.position, vis.m_leftHand.rotation, null, 1f, -1, humanoid.GetZDOID());
            humanoid.SetupEquipment();
            if (effects)
                humanoid.TriggerEquipEffect(torch);
            return true;
        }

        /// <summary>After a terrain tool was equipped: the torch it pushed out goes back into the left hand.</summary>
        public static void Restore(Humanoid humanoid, ItemDrop.ItemData torch)
        {
            if (humanoid.m_leftItem != null || !HeldTool.IsTool(humanoid.m_rightItem) || !humanoid.m_inventory.ContainsItem(torch))
                return;
            if (torch.m_shared.m_useDurability && torch.m_durability <= 0f)
                return;
            humanoid.m_leftItem = torch;
            torch.m_equipped = true;
            humanoid.SetupEquipment();
        }

        /// <summary>The game's own checks before equipping, so a refused equip is left to the game and refused the same way.</summary>
        private static bool CanEquipNow(Humanoid humanoid, ItemDrop.ItemData item)
        {
            if (humanoid.IsItemEquiped(item) || !humanoid.m_inventory.ContainsItem(item) || humanoid.InAttack() || humanoid.InDodge())
                return false;
            if (humanoid.IsSwimming() && !humanoid.IsOnGround())
                return false;
            return !(item.m_shared.m_useDurability && item.m_durability <= 0f);
        }
    }

    /// <summary>
    /// Prefix: a torch equipped while a terrain tool is out goes to the left hand instead of replacing the tool; a terrain
    /// tool about to be equipped notes the torch in hand. Postfix: that torch goes back into the left hand.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    public static class TorchEquipPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, ref bool __result, out ItemDrop.ItemData __state)
        {
            __state = null;
            if (item == null || !TorchHand.Applies(__instance))
                return true;
            if (TorchHand.IsTorch(item) && HeldTool.IsTool(__instance.m_rightItem))
            {
                bool handled = Safe.Call("EarthWright torch", () => TorchHand.EquipLeft(__instance, item, triggerEquipEffects), false);
                if (handled)
                    __result = true;
                return !handled;
            }
            if (HeldTool.IsTool(item))
                __state = TorchHand.HeldTorch(__instance);
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(Humanoid __instance, bool __result, ItemDrop.ItemData __state)
        {
            if (__result && __state != null)
                Safe.Run("EarthWright torch", () => TorchHand.Restore(__instance, __state));
        }
    }
}
