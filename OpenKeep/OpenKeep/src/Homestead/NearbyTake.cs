using System;
using System.Collections.Generic;
using OpenKeep.Core;
using OpenKeep.Reach;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Takes a fire's fuel or a station's ore and fuel out of the containers near it (not near a player), nearest
    /// first. The caller finds them with <c>ContainerScan.Nearby</c> (usable: loaded, not open by another player, the
    /// local player's ward and privacy access, the section 0 switches, the prefab enabled); here each is claimed, the
    /// units removed and the container saved. What may be taken is Reach's: the caller's predicate (the station's own
    /// items narrowed by its prefab's <c>stations:</c> allow/deny, <c>StationAccepts</c>), per stack the container
    /// prefab's allow/deny (<c>ReachRules.RuleFor</c>), and the game's world level rule, which keeps items of an
    /// easier world level out of stations as <c>Inventory.GetItem</c> and <c>HaveItem</c> do. A caller may keep some
    /// units of each item (by shared name) in every container, so a chest never runs out of what it is for. Needs a
    /// local player (the access checks are that player's), so a dedicated server takes nothing.
    /// </summary>
    public static class NearbyTake
    {
        /// <summary>
        /// Removes up to <paramref name="wanted"/> units, leaving <paramref name="keep"/> of each item in every container;
        /// returns how many left the containers. <paramref name="removed"/>, when given, hears each stack taken from with
        /// the count, so a caller can pass the item on to the game.
        /// </summary>
        public static int Take(List<Container> containers, Func<ItemDrop.ItemData, bool> accepts, int wanted, Action<ItemDrop.ItemData, int> removed = null, int keep = 0)
        {
            Func<ItemDrop.ItemData, bool> usable = item => item.m_worldLevel >= Game.m_worldLevel && accepts(item);
            int taken = 0;
            foreach (Container container in containers)
            {
                if (taken >= wanted)
                    break;
                if (Spare(container, usable, keep).Count == 0)
                    continue;
                using (SaveHolds.Hold(container))
                    taken += TakeFrom(container, usable, keep, wanted - taken, removed);
            }
            return taken;
        }

        /// <summary>
        /// Claims one container and removes from its spare units (read again after the claim loaded it), then saves it
        /// through the game, once (the caller holds its saves). A container another client owns is asked for at most once
        /// per <see cref="HandOver.BackgroundSeconds"/> and used once it is ours, never taken on this timer.
        /// </summary>
        private static int TakeFrom(Container container, Func<ItemDrop.ItemData, bool> accepts, int keep, int wanted, Action<ItemDrop.ItemData, int> removed)
        {
            if (!ContainerScan.Claim(container, HandOver.BackgroundSeconds))
                return 0;
            Dictionary<string, int> spare = Spare(container, accepts, keep);
            ContainerRule rule = ReachRules.RuleFor(container);
            Inventory inventory = container.GetInventory();
            int taken = 0;
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (taken >= wanted)
                    break;
                if (!spare.TryGetValue(item.m_shared.m_name, out int left) || !accepts(item) || !rule.Accepts(item))
                    continue;
                int take = Math.Min(Math.Min(item.m_stack, left), wanted - taken);
                if (take <= 0 || !inventory.RemoveItem(item, take))
                    continue;
                spare[item.m_shared.m_name] = left - take;
                taken += take;
                removed?.Invoke(item, take);
            }
            if (taken > 0)
                ContainerScan.Save(container);
            return taken;
        }

        /// <summary>Per shared name, the acceptable units the container holds beyond <paramref name="keep"/>; names with none spare are left out.</summary>
        private static Dictionary<string, int> Spare(Container container, Func<ItemDrop.ItemData, bool> accepts, int keep)
        {
            ContainerRule rule = ReachRules.RuleFor(container);
            Dictionary<string, int> held = new Dictionary<string, int>();
            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
            {
                if (!accepts(item) || !rule.Accepts(item))
                    continue;
                held.TryGetValue(item.m_shared.m_name, out int count);
                held[item.m_shared.m_name] = count + item.m_stack;
            }
            Dictionary<string, int> spare = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> entry in held)
            {
                if (entry.Value > keep)
                    spare[entry.Key] = entry.Value - keep;
            }
            return spare;
        }
    }
}
