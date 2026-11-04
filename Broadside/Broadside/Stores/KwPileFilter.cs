using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An ammunition pile takes only its own sort of item (<see cref="KwPileDef.Accepts"/>), by every way the game puts an
    /// item into an inventory: the item-data <c>Inventory.AddItem</c> overloads (shift-click and the game's other quick moves,
    /// placing stacks, <c>MoveAll</c>, a drag into a slot, and everything another mod does through them) answer false, and
    /// <c>Inventory.CanAddItem</c> says no, so another mod's quick stack that asks first passes it by. A drag in the
    /// container window is stopped before the game's swap (<c>InventoryGrid.DropItem</c>, which takes the dragged item out
    /// before it puts it in): an item dragged into a pile, or dragged out of one onto an item the pile would not take, stays
    /// on the cursor. Refused in the window, the game's own words say so ("$msg_wrongitem" in the middle of the screen, at
    /// most twice a second). Loading the pile's inventory from its ZDO is never refused (the Container's own load), so
    /// nothing already in it is lost. Each patch costs one count read while no pile is loaded, one dictionary look-up
    /// otherwise; nothing runs per frame. The game's cart's own storage takes nothing at all since carts carry crates
    /// (2026-11-03, <see cref="KwCartBed.Refuses"/>, <c>Cart/</c>): the same patches ask it, with the cart's own words.
    /// </summary>
    [HarmonyPatch]
    public static class KwPileFilter
    {
        /// <summary>The game's own words for an item a thing does not take.</summary>
        public const string WrongItem = "$msg_wrongitem";

        private const float Quiet = 0.5f;
        private static readonly Dictionary<Inventory, KwPile> piles = new Dictionary<Inventory, KwPile>();
        private static float said = -99f;

        /// <summary>A loaded pile's inventory, filtered from now on.</summary>
        public static void Track(Inventory inventory, KwPile pile) => piles[inventory] = pile;

        public static void Untrack(Inventory inventory) => piles.Remove(inventory);

        /// <summary>Whether the inventory is a loaded pile's that does not take the item (never while it loads from its ZDO).</summary>
        public static bool Refuses(Inventory inventory, ItemDrop.ItemData? item)
        {
            if (item != null && KwCartBed.Stores > 0 && KwCartBed.Refuses(inventory))
                return true;
            if (piles.Count == 0 || item == null || !piles.TryGetValue(inventory, out KwPile pile) || pile == null)
                return false;
            return !pile.Box.m_loading && !pile.Accepts(item);
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData)), HarmonyPrefix]
        private static bool AddItem(Inventory __instance, ItemDrop.ItemData item, ref bool __result) => Allow(__instance, item, ref __result);

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(Vector2i)), HarmonyPrefix]
        private static bool AddItemAt(Inventory __instance, ItemDrop.ItemData item, ref bool __result) => Allow(__instance, item, ref __result);

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool)), HarmonyPrefix]
        private static bool AddItemInto(Inventory __instance, ItemDrop.ItemData item, ref bool __result) => Allow(__instance, item, ref __result);

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int)), HarmonyPrefix]
        private static bool CanAddItem(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!Refuses(__instance, item))
                return true;
            __result = false;
            return false;
        }

        // The drag's drop, before the game's swap takes the dragged item out of its inventory: refused, it stays on the cursor.
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem)), HarmonyPrefix]
        private static bool DropItem(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, Vector2i pos, ref bool __result)
        {
            Inventory into = __instance.m_inventory;
            if (piles.Count == 0 && KwCartBed.Stores == 0 || into == null || fromInventory == null || fromInventory == into)
                return true;
            Inventory? pile = Refuses(into, item) ? into : Swaps(fromInventory, into, item, pos) ? fromInventory : null;
            if (pile == null)
                return true;
            Say(pile);
            __result = false;
            return false;
        }

        // Dragged out of a pile onto an item there the pile would not take in its place (out of a cart's storage: only where
        // the game would swap them, so goods still stack onto the same goods).
        private static bool Swaps(Inventory pile, Inventory into, ItemDrop.ItemData item, Vector2i pos)
        {
            bool cart = KwCartBed.Stores > 0 && KwCartBed.Refuses(pile);
            if (!cart && !piles.ContainsKey(pile))
                return false;
            ItemDrop.ItemData? there = into.GetItemAt(pos.x, pos.y);
            return there != null && there != item && (!cart || Trades(there, item)) && Refuses(pile, there);
        }

        // The game's swap: a different item there, another quality, or one that does not stack.
        private static bool Trades(ItemDrop.ItemData there, ItemDrop.ItemData item) =>
            there.m_shared.m_name != item.m_shared.m_name || item.m_shared.m_maxQuality > 1 && there.m_quality != item.m_quality || there.m_shared.m_maxStackSize == 1;

        private static bool Allow(Inventory inventory, ItemDrop.ItemData item, ref bool result)
        {
            if (!Refuses(inventory, item))
                return true;
            Say(inventory);
            result = false;
            return false;
        }

        // The game's words, only for the pile open in the local player's container window (another mod's work elsewhere is silent).
        private static void Say(Inventory pile)
        {
            InventoryGui? gui = InventoryGui.instance;
            Player? player = Player.m_localPlayer;
            if (gui == null || player == null || gui.m_currentContainer == null || gui.m_currentContainer.GetInventory() != pile || Time.time < said + Quiet)
                return;
            said = Time.time;
            player.Message(MessageHud.MessageType.Center, KwCartBed.Stores > 0 && KwCartBed.Refuses(pile) ? KwCartText.NothingIn : WrongItem);
        }
    }
}
