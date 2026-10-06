using System;
using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Shared
{
    /// <summary>
    /// The request path of <see cref="ChestWriter"/>: builds each request of SPEC 9.2, checks what the requester
    /// must check first (a take fits the inventory, a stack-all has candidates), and applies the owner's answer
    /// to the requester's own inventory. What a put or a stack-all sends is held out of the inventory until the
    /// answer (<see cref="Escrow"/>): the chest's take stays with the chest, the rest comes back, all of it on a "no"
    /// or when no answer ever comes. The chest itself is never touched here; its copy on this client refreshes from
    /// the ZDO once the owner saved.
    /// </summary>
    internal static class ChestAsk
    {
        public static void Take(Container container, ItemDrop.ItemData item, int amount, Player player, Vector2i slot, Action<bool> done)
        {
            Vector2i from = item.m_gridPos;
            if (ChestRequester.IsPending(container, from))
            {
                done(false);
                return;
            }
            int fit = new InventoryFit(player.GetInventory()).Reserve(item, amount);
            if (fit <= 0)
            {
                Messages.Center(SharedWords.NoFit);
                done(false);
                return;
            }
            string name = item.m_shared.m_name;
            int expected = item.m_stack;
            ChestRequester.Send(container, ChestRequests.TakeRpc, from,
                pkg => { pkg.Write(from); pkg.Write(name); pkg.Write(expected); pkg.Write(fit); },
                payload => ReceiveItem(player, payload, slot), null, done);
        }

        private static bool ReceiveItem(Player player, ZPackage payload, Vector2i slot)
        {
            ItemDrop.ItemData taken = ItemPacket.Read(payload);
            if (taken == null)
                return false;
            ChestOps.GiveToPlayer(player, taken, slot);
            return true;
        }

        public static void Put(Container container, ItemDrop.ItemData item, int amount, Player player, Vector2i slot, Action<bool> done)
        {
            ItemDrop.ItemData copy = ItemPacket.Copy(item, amount);
            if (copy.m_dropPrefab == null)
            {
                Messages.Center(SharedWords.Unavailable);
                done(false);
                return;
            }
            Escrow held = Escrow.Hold(player, item, copy.m_stack);
            if (held == null)
            {
                done(false);
                return;
            }
            bool hasSlot = slot.x >= 0;
            ChestRequester.Send(container, ChestRequests.PutRpc, slot,
                pkg => { ItemPacket.Write(pkg, copy); pkg.Write(hasSlot); pkg.Write(slot); },
                payload => { held.Return(payload.ReadInt()); return true; },
                () => held.Return(0), done);
        }

        public static void Move(Container container, Vector2i from, Vector2i to, int amount, Action<bool> done)
        {
            ItemDrop.ItemData item = container.GetInventory().GetItemAt(from.x, from.y);
            if (item == null || ChestRequester.IsPending(container, from))
            {
                done(false);
                return;
            }
            string name = item.m_shared.m_name;
            ChestRequester.Send(container, ChestRequests.MoveRpc, from,
                pkg => { pkg.Write(from); pkg.Write(to); pkg.Write(amount); pkg.Write(name); },
                payload => true, null, done);
        }

        public static void TakeAll(Container container, Player player, Action<bool> done)
        {
            List<KeyValuePair<ItemDrop.ItemData, int>> entries = FitEntries(container.GetInventory(), player.GetInventory());
            if (entries.Count == 0)
            {
                Messages.Center(SharedWords.NoFit);
                done(false);
                return;
            }
            ChestRequester.Send(container, ChestRequests.TakeAllRpc, ChestOps.NoSlot,
                pkg => WriteEntries(pkg, entries),
                payload => ReceiveItems(player, payload), null, done);
        }

        /// <summary>The chest stacks, in the game's order, with the units of each that the inventory can take.</summary>
        private static List<KeyValuePair<ItemDrop.ItemData, int>> FitEntries(Inventory chest, Inventory inventory)
        {
            InventoryFit fit = new InventoryFit(inventory);
            List<KeyValuePair<ItemDrop.ItemData, int>> entries = new List<KeyValuePair<ItemDrop.ItemData, int>>();
            foreach (ItemDrop.ItemData item in chest.GetAllItems())
            {
                int units = fit.Reserve(item, item.m_stack);
                if (units > 0)
                    entries.Add(new KeyValuePair<ItemDrop.ItemData, int>(item, units));
            }
            return entries;
        }

        private static void WriteEntries(ZPackage pkg, List<KeyValuePair<ItemDrop.ItemData, int>> entries)
        {
            pkg.Write(entries.Count);
            foreach (KeyValuePair<ItemDrop.ItemData, int> entry in entries)
            {
                pkg.Write(entry.Key.m_gridPos);
                pkg.Write(entry.Key.m_shared.m_name);
                pkg.Write(entry.Value);
            }
        }

        private static bool ReceiveItems(Player player, ZPackage payload)
        {
            int count = payload.ReadInt();
            int received = 0;
            for (int i = 0; i < count; i++)
            {
                ItemDrop.ItemData item = ItemPacket.Read(payload);
                if (item == null)
                    continue;
                ChestOps.GiveToPlayer(player, item, ChestOps.NoSlot);
                received++;
            }
            return received > 0;
        }

        public static void StackAll(Container container, Player player, Action<bool> done)
        {
            List<ItemDrop.ItemData> candidates = StackCandidates(container.GetInventory(), player);
            if (candidates.Count == 0)
            {
                Messages.Center("$msg_stackall_none");
                done(false);
                return;
            }
            List<ItemDrop.ItemData> copies = new List<ItemDrop.ItemData>();
            List<Escrow> held = HoldAll(player, candidates, copies);
            ChestRequester.Send(container, ChestRequests.StackAllRpc, ChestOps.NoSlot,
                pkg => WriteCandidates(pkg, copies),
                payload => ReceiveAccepted(held, payload),
                () => held.ForEach(escrow => escrow.Return(0)), done);
        }

        /// <summary>
        /// The game's stack-all candidates: unequipped inventory stacks whose kind the chest already holds, from the grid
        /// only (PackPanel's slots keep theirs, as with the game's own Place stacks: <see cref="PackPanelGrid"/>).
        /// </summary>
        private static List<ItemDrop.ItemData> StackCandidates(Inventory chest, Player player)
        {
            List<ItemDrop.ItemData> candidates = new List<ItemDrop.ItemData>();
            Inventory own = player.GetInventory();
            foreach (ItemDrop.ItemData item in own.GetAllItems())
            {
                if (item.m_equipped || player.IsItemEquiped(item) || item.m_dropPrefab == null || PackPanelGrid.InSlot(player, own, item))
                    continue;
                if (chest.ContainsItemByName(item.m_shared.m_name))
                    candidates.Add(item);
            }
            return candidates;
        }

        /// <summary>Holds every candidate stack back and lists the copy of each one held; a stack that left meanwhile is not sent.</summary>
        private static List<Escrow> HoldAll(Player player, List<ItemDrop.ItemData> candidates, List<ItemDrop.ItemData> copies)
        {
            List<Escrow> held = new List<Escrow>();
            foreach (ItemDrop.ItemData item in candidates)
            {
                ItemDrop.ItemData copy = ItemPacket.Copy(item, item.m_stack);
                Escrow escrow = Escrow.Hold(player, item, item.m_stack);
                if (escrow == null)
                    continue;
                copies.Add(copy);
                held.Add(escrow);
            }
            return held;
        }

        private static void WriteCandidates(ZPackage pkg, List<ItemDrop.ItemData> copies)
        {
            pkg.Write(copies.Count);
            for (int i = 0; i < copies.Count; i++)
            {
                pkg.Write(i);
                ItemPacket.Write(pkg, copies[i]);
            }
        }

        /// <summary>The chest kept what it accepted of each held stack; the rest of every stack comes back.</summary>
        private static bool ReceiveAccepted(List<Escrow> held, ZPackage payload)
        {
            int[] accepted = new int[held.Count];
            int count = payload.ReadInt();
            for (int i = 0; i < count; i++)
            {
                int index = payload.ReadInt();
                int units = payload.ReadInt();
                if (index >= 0 && index < held.Count && units > 0)
                    accepted[index] += units;
            }
            int total = 0;
            for (int i = 0; i < held.Count; i++)
            {
                held[i].Return(accepted[i]);
                total += accepted[i];
            }
            Messages.Center(total > 0 ? "$msg_stackall " + total : "$msg_stackall_none");
            return total > 0;
        }
    }
}
