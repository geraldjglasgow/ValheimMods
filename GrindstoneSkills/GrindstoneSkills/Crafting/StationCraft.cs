using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Station crafts for Maker's Mark and Thrifty, on the crafting player's client, where their skills live.
    /// InventoryGui.DoCrafting adds the crafted item (an upgrade removes the old item and adds a fresh one in its place;
    /// a multi-craft of an unstackable item adds several), then pays through Inventory.RemoveItem(name, amount, quality,
    /// worldLevel) on the player's inventory: Player.ConsumeResources per requirement, or directly for a single-ingredient
    /// recipe (OpenKeep takes a shortfall from chests inside that call; the call itself still happens).
    /// While the module is on, the local player has some Crafting and the station they use trains Crafting, the prefix
    /// notes which items of the recipe's kind are already in the inventory. The postfix then marks each one that is new
    /// (<see cref="MakersMark"/>) and rolls Thrifty for each batch over the materials the RemoveItem postfix saw paid
    /// (<see cref="Thrifty"/>). It runs late, after other mods' postfixes, so a mod that rebuilds the new item's custom
    /// data (Elite Crafting carries its own keys over an upgrade) keeps the mark. A craft another mod cancels, a failed
    /// craft and the no-cost cheat add or pay nothing, so they get nothing. The finalizer forgets it all, also when the
    /// craft threw.
    /// </summary>
    public static class StationCraft
    {
        private static readonly HashSet<ItemDrop.ItemData> before = new HashSet<ItemDrop.ItemData>();
        private static readonly List<Thrifty.Material> paid = new List<Thrifty.Material>();

        private static Player crafter;
        private static Recipe recipe;
        private static string markName;
        private static float level;
        private static int batches;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static class Crafting
        {
            [HarmonyPrefix]
            private static void Prefix(InventoryGui __instance, Player player) =>
                HookGuard.Run("crafting perks", static craft => Begin(craft.gui, craft.player), (gui: __instance, player));

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Low)]
            private static void Postfix() => HookGuard.Run("crafting perks", static () => Finish());

            [HarmonyFinalizer]
            private static void Finalizer() => Clear();
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
        private static class Paid
        {
            [HarmonyPostfix]
            private static void Postfix(Inventory __instance, string name, int itemQuality)
            {
                if (crafter != null && __instance == crafter.GetInventory())
                    HookGuard.Run("Thrifty payment", static payment => Note(payment.name, payment.itemQuality), (name, itemQuality));
            }
        }

        private static void Begin(InventoryGui gui, Player player)
        {
            Clear();
            Recipe craft = gui.m_craftRecipe;
            if (!CraftingPerks.Enabled || !LocalSkill.IsLocal(player) || craft == null || craft.m_item == null || CraftingPerks.Station(player) == null)
                return;
            float crafting = LocalSkill.Level(Skills.SkillType.Crafting);
            if (crafting <= 0f)
                return;
            crafter = player;
            recipe = craft;
            level = crafting;
            batches = gui.m_multiCrafting ? gui.m_multiCraftAmount : 1;
            if (MakersMark.Qualifies(craft.m_item.m_itemData))
                NoteBefore(player.GetInventory(), craft.m_item.m_itemData.m_shared.m_name);
        }

        /// <summary>Remembers the items of the recipe's kind already in the inventory, so the postfix finds the new ones.</summary>
        private static void NoteBefore(Inventory inventory, string sharedName)
        {
            markName = sharedName;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.m_shared.m_name == sharedName)
                    before.Add(item);
            }
        }

        /// <summary>A payment on the crafter's inventory: noted when it is one of the recipe's materials.</summary>
        private static void Note(string name, int quality)
        {
            foreach (Piece.Requirement requirement in recipe.m_resources)
            {
                ItemDrop prefab = requirement.m_resItem;
                if (prefab != null && prefab.m_itemData.m_shared.m_name == name)
                {
                    paid.Add(new Thrifty.Material(prefab, quality));
                    return;
                }
            }
        }

        private static void Finish()
        {
            Player player = crafter;
            if (player == null)
                return;
            crafter = null;
            if (markName != null)
                MarkNew(player.GetInventory());
            Thrifty.Roll(player, batches, level, paid);
        }

        private static void MarkNew(Inventory inventory)
        {
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.m_shared.m_name == markName && !before.Contains(item))
                    MakersMark.Stamp(item, level);
            }
        }

        private static void Clear()
        {
            crafter = null;
            recipe = null;
            markName = null;
            level = 0f;
            batches = 0;
            before.Clear();
            paid.Clear();
        }
    }
}
