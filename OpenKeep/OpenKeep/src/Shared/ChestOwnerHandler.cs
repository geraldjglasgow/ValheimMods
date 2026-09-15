using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Shared
{
    /// <summary>
    /// The referee of SPEC 9.2: the client that owns a chest's ZDO validates every request against its live
    /// inventory (the slot still holds what the requester saw, the amount is there, the target takes it), applies
    /// it with the game's inventory methods, saves the chest and replies. A client that no longer owns the ZDO
    /// answers "not owner". Nothing is applied when a check fails, so a denied request leaves both sides as they
    /// were.
    /// </summary>
    public static class ChestOwnerHandler
    {
        public static void Take(Container container, long sender, ZPackage pkg)
        {
            ChestRequests.Header header = ChestRequests.ReadHeader(pkg);
            Vector2i slot = pkg.ReadVector2i();
            string name = pkg.ReadString();
            int expected = pkg.ReadInt();
            int amount = pkg.ReadInt();
            Log(header, $"take {amount} of {expected} x {name} at {slot}", container);
            if (!Owns(container, sender, header))
                return;
            Inventory inventory = container.GetInventory();
            ItemDrop.ItemData item = inventory.GetItemAt(slot.x, slot.y);
            if (item == null || item.m_shared.m_name != name || amount <= 0 || item.m_stack < amount)
            {
                Deny(container, sender, header, ChestRequests.Denied);
                return;
            }
            ItemDrop.ItemData taken = ChestOps.Take(inventory, item, amount);
            ContainerScan.Save(container);
            ZPackage payload = new ZPackage();
            ItemPacket.Write(payload, taken);
            Grant(container, sender, header, payload);
        }

        public static void Put(Container container, long sender, ZPackage pkg)
        {
            ChestRequests.Header header = ChestRequests.ReadHeader(pkg);
            ItemDrop.ItemData item = ItemPacket.Read(pkg);
            bool hasSlot = pkg.ReadBool();
            Vector2i slot = pkg.ReadVector2i();
            Log(header, $"put {(item != null ? item.m_stack + " x " + item.m_shared.m_name : "an unknown item")} at {(hasSlot ? slot.ToString() : "any slot")}", container);
            if (item == null)
            {
                Deny(container, sender, header, ChestRequests.Bad);
                return;
            }
            if (!Owns(container, sender, header))
                return;
            int accepted = ChestOps.Put(container.GetInventory(), item, hasSlot ? slot : ChestOps.NoSlot);
            if (accepted <= 0)
            {
                Deny(container, sender, header, ChestRequests.ChestFull);
                return;
            }
            ContainerScan.Save(container);
            ZPackage payload = new ZPackage();
            payload.Write(accepted);
            Grant(container, sender, header, payload);
        }

        public static void Move(Container container, long sender, ZPackage pkg)
        {
            ChestRequests.Header header = ChestRequests.ReadHeader(pkg);
            Vector2i from = pkg.ReadVector2i();
            Vector2i to = pkg.ReadVector2i();
            int amount = pkg.ReadInt();
            string name = pkg.ReadString();
            Log(header, $"move {amount} x {name} from {from} to {to}", container);
            if (!Owns(container, sender, header))
                return;
            if (!ChestOps.Move(container.GetInventory(), from, to, amount, name))
            {
                Deny(container, sender, header, ChestRequests.Denied);
                return;
            }
            ContainerScan.Save(container);
            Grant(container, sender, header, null);
        }

        public static void TakeAll(Container container, long sender, ZPackage pkg)
        {
            ChestRequests.Header header = ChestRequests.ReadHeader(pkg);
            int count = pkg.ReadInt();
            Log(header, $"take all, {count} stacks", container);
            if (!Owns(container, sender, header))
                return;
            List<ItemDrop.ItemData> taken = TakeEntries(container.GetInventory(), pkg, count);
            if (taken.Count == 0)
            {
                Deny(container, sender, header, ChestRequests.Denied);
                return;
            }
            ContainerScan.Save(container);
            ZPackage payload = new ZPackage();
            payload.Write(taken.Count);
            foreach (ItemDrop.ItemData item in taken)
                ItemPacket.Write(payload, item);
            Grant(container, sender, header, payload);
        }

        /// <summary>Every entry whose slot still holds the named item gives up the requested amount, at most its stack.</summary>
        private static List<ItemDrop.ItemData> TakeEntries(Inventory inventory, ZPackage pkg, int count)
        {
            List<ItemDrop.ItemData> taken = new List<ItemDrop.ItemData>();
            for (int i = 0; i < count; i++)
            {
                Vector2i slot = pkg.ReadVector2i();
                string name = pkg.ReadString();
                int amount = pkg.ReadInt();
                ItemDrop.ItemData item = inventory.GetItemAt(slot.x, slot.y);
                if (item == null || item.m_shared.m_name != name || amount <= 0)
                    continue;
                taken.Add(ChestOps.Take(inventory, item, Mathf.Min(amount, item.m_stack)));
            }
            return taken;
        }

        public static void StackAll(Container container, long sender, ZPackage pkg)
        {
            ChestRequests.Header header = ChestRequests.ReadHeader(pkg);
            int count = pkg.ReadInt();
            Log(header, $"stack all, {count} candidate stacks", container);
            if (!Owns(container, sender, header))
                return;
            List<KeyValuePair<int, int>> accepted = StackEntries(container.GetInventory(), pkg, count);
            if (accepted.Count == 0)
            {
                Deny(container, sender, header, ChestRequests.Nothing);
                return;
            }
            ContainerScan.Save(container);
            ZPackage payload = new ZPackage();
            payload.Write(accepted.Count);
            foreach (KeyValuePair<int, int> entry in accepted)
            {
                payload.Write(entry.Key);
                payload.Write(entry.Value);
            }
            Grant(container, sender, header, payload);
        }

        /// <summary>The game's stack-all rule: a sent stack goes in when the chest already holds its kind; returns (index, units accepted).</summary>
        private static List<KeyValuePair<int, int>> StackEntries(Inventory inventory, ZPackage pkg, int count)
        {
            List<KeyValuePair<int, int>> accepted = new List<KeyValuePair<int, int>>();
            for (int i = 0; i < count; i++)
            {
                int index = pkg.ReadInt();
                ItemDrop.ItemData item = ItemPacket.Read(pkg);
                if (item == null || !inventory.ContainsItemByName(item.m_shared.m_name))
                    continue;
                int before = item.m_stack;
                int units = inventory.AddItem(item) ? before : before - item.m_stack;
                if (units > 0)
                    accepted.Add(new KeyValuePair<int, int>(index, units));
            }
            return accepted;
        }

        private static bool Owns(Container container, long sender, ChestRequests.Header header)
        {
            if (container.m_nview.IsOwner())
                return true;
            Deny(container, sender, header, ChestRequests.NotOwner);
            return false;
        }

        private static void Deny(Container container, long sender, ChestRequests.Header header, string reason)
        {
            ChestRequests.Reply(container, sender, header, false, reason, null);
        }

        private static void Grant(Container container, long sender, ChestRequests.Header header, ZPackage payload)
        {
            ChestRequests.Reply(container, sender, header, true, "", payload);
        }

        private static void Log(ChestRequests.Header header, string what, Container container)
        {
            Plugin.Log.LogDebug($"OpenKeep: request {header.Id} from {header.PlayerName}: {what} in {ContainerScan.PrefabName(container)}");
        }
    }
}
