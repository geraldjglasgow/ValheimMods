using System;
using System.Collections.Generic;
using System.Text;
using OpenKeep.Core;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// The public face of the module, used by the Salvage tab, the hotkey and other modules (Stow's
    /// "Trash Uses Salvage"): can an item be salvaged, what does it return, salvage it, or ask first.
    /// </summary>
    public static class SalvageActions
    {
        /// <summary>True when the local player could salvage this stack right now.</summary>
        public static bool CanSalvage(ItemDrop.ItemData item) => SalvageRules.Blocker(Player.m_localPlayer, item) == null;

        /// <summary>The "$ok_..." word that says why the local player cannot salvage the item, or null when they can.</summary>
        public static string WhyNot(ItemDrop.ItemData item) => SalvageRules.Blocker(Player.m_localPlayer, item);

        /// <summary>The materials the whole stack returns; empty when the item has no usable recipe.</summary>
        public static List<SalvageReturn> Returns(ItemDrop.ItemData item)
        {
            return SalvageReturns.Compute(SalvageRules.FindRecipe(item), item);
        }

        /// <summary>
        /// Salvages the whole stack: removes it from the player's inventory and adds the returns. False, with a
        /// centre message saying why, when the item cannot be salvaged or the returns would not fit; then nothing
        /// changed. The fit is checked before anything is removed; should an add still fail, the adds are rolled
        /// back and the stack is put back into its slot.
        /// </summary>
        public static bool Salvage(Player player, ItemDrop.ItemData item)
        {
            if (player == null)
                return false;
            string blocker = SalvageRules.Blocker(player, item);
            if (blocker != null)
                return Refuse(blocker);
            Inventory inventory = player.GetInventory();
            if (!inventory.ContainsItem(item))
                return Refuse(SalvageWords.Cannot);
            List<SalvageReturn> returns = Returns(item);
            if (returns.Count == 0)
                return Refuse(SalvageWords.Nothing);
            if (!SalvageInventory.Fits(inventory, item, returns))
                return Refuse(SalvageWords.NoFit);
            return Exchange(inventory, item, returns);
        }

        private static bool Exchange(Inventory inventory, ItemDrop.ItemData item, List<SalvageReturn> returns)
        {
            Vector2i slot = item.m_gridPos;
            string summary = Summary(item, returns);
            inventory.RemoveItem(item);
            if (!SalvageInventory.Add(inventory, returns))
            {
                inventory.AddItem(item, slot);
                return Refuse(SalvageWords.NoFit);
            }
            Messages.TopLeft(summary);
            return true;
        }

        private static bool Refuse(string word)
        {
            Messages.Center(word);
            return false;
        }

        /// <summary>"Salvaged Iron sword x2: 6 Iron, 2 Wood" with $tokens left for the message to localize.</summary>
        public static string Summary(ItemDrop.ItemData item, List<SalvageReturn> returns)
        {
            StringBuilder text = new StringBuilder();
            text.Append(SalvageWords.Done).Append(' ').Append(item.m_shared.m_name);
            if (item.m_stack > 1)
                text.Append(" x").Append(item.m_stack);
            text.Append(": ").Append(ReturnList(returns));
            return text.ToString();
        }

        /// <summary>"6 $item_iron, 2 $item_wood".</summary>
        public static string ReturnList(List<SalvageReturn> returns)
        {
            StringBuilder text = new StringBuilder();
            foreach (SalvageReturn entry in returns)
            {
                if (text.Length > 0)
                    text.Append(", ");
                text.Append(entry.Amount).Append(' ').Append(entry.Name);
            }
            return text.ToString();
        }

        /// <summary>
        /// Asks with the game's yes/no popup (gamepad works), salvages on yes and calls <paramref name="afterwards"/>
        /// when the salvage succeeded. Without the popup (no UI yet) it salvages at once.
        /// </summary>
        public static void Confirm(Player player, ItemDrop.ItemData item, Action afterwards)
        {
            string blocker = SalvageRules.Blocker(player, item);
            if (blocker != null)
            {
                Messages.Center(blocker);
                return;
            }
            if (!UnifiedPopup.IsAvailable())
            {
                if (Salvage(player, item))
                    afterwards?.Invoke();
                return;
            }
            string text = AskText(item);
            UnifiedPopup.Push(new YesNoPopup(SalvageWords.Salvage, text, () => Accept(player, item, afterwards), UnifiedPopup.Pop));
        }

        private static void Accept(Player player, ItemDrop.ItemData item, Action afterwards)
        {
            UnifiedPopup.Pop();
            if (Salvage(player, item))
                afterwards?.Invoke();
        }

        private static string AskText(ItemDrop.ItemData item)
        {
            StringBuilder text = new StringBuilder();
            text.Append(item.m_shared.m_name);
            if (item.m_stack > 1)
                text.Append(" x").Append(item.m_stack);
            text.Append("\n\n").Append(SalvageWords.Ask).Append('\n').Append(ReturnList(Returns(item)));
            return text.ToString();
        }
    }
}
