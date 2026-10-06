using OpenKeep.Core;

namespace OpenKeep.Shared
{
    /// <summary>
    /// The synchronous path of <see cref="ChestWriter"/>: the local client owns the chest or may claim it because
    /// nobody uses it. Claim before, the game's inventory methods, save after; the same as every OpenKeep action
    /// does today. Each call holds the chest's saves (<see cref="SaveHolds"/>), so it is written once, not after every
    /// add and remove, and once for a whole quick stack when the caller holds it too. Quiet: the callers report.
    /// </summary>
    internal static class ChestSync
    {
        public static bool Take(Container container, ItemDrop.ItemData item, int amount, Player player, Vector2i slot)
        {
            using (SaveHolds.Hold(container))
                return TakeHeld(container, item, amount, player, slot);
        }

        public static bool Put(Container container, ItemDrop.ItemData item, int amount, Player player, Vector2i slot)
        {
            using (SaveHolds.Hold(container))
                return PutHeld(container, item, amount, player, slot);
        }

        public static bool Move(Container container, Vector2i from, Vector2i to, int amount)
        {
            using (SaveHolds.Hold(container))
                return MoveHeld(container, from, to, amount);
        }

        public static bool TakeAll(Container container, Player player)
        {
            using (SaveHolds.Hold(container))
                return TakeAllHeld(container, player);
        }

        public static bool StackAll(Container container, Player player)
        {
            using (SaveHolds.Hold(container))
                return StackAllHeld(container, player);
        }

        private static bool TakeHeld(Container container, ItemDrop.ItemData item, int amount, Player player, Vector2i slot)
        {
            if (!ContainerScan.Claim(container))
                return false;
            int moved = ChestOps.Transfer(container.GetInventory(), item, amount, player.GetInventory(), slot);
            if (moved > 0)
                ContainerScan.Save(container);
            return moved > 0;
        }

        private static bool PutHeld(Container container, ItemDrop.ItemData item, int amount, Player player, Vector2i slot)
        {
            if (!ContainerScan.Claim(container))
                return false;
            int moved = ChestOps.Transfer(player.GetInventory(), item, amount, container.GetInventory(), slot);
            if (moved > 0)
                ContainerScan.Save(container);
            return moved > 0;
        }

        private static bool MoveHeld(Container container, Vector2i from, Vector2i to, int amount)
        {
            if (!ContainerScan.Claim(container))
                return false;
            bool moved = ChestOps.Move(container.GetInventory(), from, to, amount, null);
            if (moved)
                ContainerScan.Save(container);
            return moved;
        }

        private static bool TakeAllHeld(Container container, Player player)
        {
            if (!ContainerScan.Claim(container))
                return false;
            player.GetInventory().MoveAll(container.GetInventory());
            ContainerScan.Save(container);
            return true;
        }

        private static bool StackAllHeld(Container container, Player player)
        {
            if (!ContainerScan.Claim(container))
                return false;
            int stacked = container.GetInventory().StackAll(player.GetInventory());
            ContainerScan.Save(container);
            Messages.Center(stacked > 0 ? "$msg_stackall " + stacked : "$msg_stackall_none");
            return stacked > 0;
        }
    }
}
