using System.Collections.Generic;
using OpenKeep.Core;
using OpenKeep.Shared;

namespace OpenKeep.Stow
{
    /// <summary>
    /// Route (modifier + click): the stack goes to the nearest nearby container that holds the exact item, then one
    /// holding an item of the same group, then one whose <c>accept</c> list matches. The container the player has
    /// open leads the candidates, so the game's own modifier click target wins a tie. Store one (its key):
    /// one item goes to the open container, else to the nearest container holding it. Both go through the writer:
    /// a shared target answers later, and its answer is the message.
    /// </summary>
    public static class Routing
    {
        public static void Route(ItemDrop.ItemData item)
        {
            if (!StowActions.Ready(out Player player) || Refused(player, item))
                return;
            string name = ItemNames.DisplayName(item);
            Container target = FindTarget(player, item);
            if (target == null)
            {
                Messages.Center(StowWords.Format(StowWords.NoRoute, name));
                return;
            }
            Send(target, item, item.m_stack, StowWords.Format(StowWords.Routed, name, StowTargets.Name(target)));
        }

        public static void StoreOne(ItemDrop.ItemData item)
        {
            if (!StowActions.Ready(out Player player) || Refused(player, item))
                return;
            string name = ItemNames.DisplayName(item);
            Container target = StowTargets.OpenTarget;
            if (target == null && StowTargets.ViewingOnly())
                return;
            target = target ?? Holder(player, item);
            if (target == null || StowRules.Refuses(target, item))
            {
                Messages.Center(StowWords.Format(StowWords.NoRoute, name));
                return;
            }
            Send(target, item, 1, StowWords.Format(StowWords.StoredOne, name));
        }

        /// <summary>One put through the writer: on success the message; a chest changed at once that took nothing
        /// says "Nothing to move", a shared chest's refusal is said by the writer.</summary>
        private static void Send(Container target, ItemDrop.ItemData item, int amount, string success)
        {
            bool shared = StowTargets.IsShared(target);
            StackMover.Reserve(item);
            ChestWriter.Put(target, item, amount, null, ok =>
            {
                StackMover.Release(item);
                if (ok)
                    Messages.Center(success);
                else if (!shared)
                    Messages.Center(StowWords.Nothing);
            });
        }

        private static bool Refused(Player player, ItemDrop.ItemData item)
        {
            string blocker = Movable.Blocker(player, player.GetInventory(), item);
            if (blocker == null)
                return false;
            if (blocker.Length > 0)
                Messages.Center(StowWords.Format(blocker, ItemNames.DisplayName(item)));
            return true;
        }

        private static Container Holder(Player player, ItemDrop.ItemData item)
        {
            foreach (Container container in StowTargets.Nearby(player))
            {
                if (container.GetInventory().ContainsItemByName(item.m_shared.m_name))
                    return container;
            }
            return null;
        }

        /// <summary>Targets within Nearby Range, the open one first, then nearest first; refusing ones dropped.</summary>
        private static Container FindTarget(Player player, ItemDrop.ItemData item)
        {
            List<Container> nearby = StowTargets.Nearby(player);
            nearby.RemoveAll(container => StowRules.Refuses(container, item));
            Container exact = nearby.Find(container => container.GetInventory().ContainsItemByName(item.m_shared.m_name));
            if (exact != null)
                return exact;
            List<string> groups = StowRules.GroupsOf(item);
            Container grouped = groups.Count > 0 ? nearby.Find(container => HoldsGroup(container, groups)) : null;
            return grouped ?? nearby.Find(container => StowRules.Accepts(container, item));
        }

        private static bool HoldsGroup(Container container, List<string> groups)
        {
            foreach (ItemDrop.ItemData held in container.GetInventory().GetAllItems())
            {
                foreach (string group in groups)
                {
                    if (StowRules.Groups.Matches(group, held))
                        return true;
                }
            }
            return false;
        }
    }
}
