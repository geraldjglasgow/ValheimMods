using System;
using System.Collections.Generic;

namespace OpenKeep.Reach
{
    /// <summary>
    /// The reachable containers of the local player and what they hold. The container list comes from
    /// <see cref="ReachChests"/> (one list per frame, its slow rule checks every quarter second); requirement counts
    /// (<see cref="InContainers"/>) come from <see cref="StorageIndex"/>, which walks the containers once per change
    /// (the panels ask for every row every frame); per container counts and the first stack lookups read the
    /// contents live. Every count honours the prefab's allow / deny lists per stack and the game's quality and world
    /// level filters.
    /// </summary>
    public static class ReachCount
    {
        /// <summary>Every reachable container, nearest first: section 0 rules, the enabled table and the per prefab range.</summary>
        public static List<Container> Containers() => ReachChests.List();

        /// <summary>The game's requirement filter: shared name, quality (-1 for any) and the world level rule.</summary>
        public static bool Matches(ItemDrop.ItemData item, string name, int quality, bool worldLevel)
        {
            if (item == null || item.m_shared == null || item.m_shared.m_name != name)
                return false;
            if (quality >= 0 && item.m_quality != quality)
                return false;
            return !worldLevel || item.m_worldLevel >= Game.m_worldLevel;
        }

        /// <summary>Items of a shared name in every reachable container: from the storage index for the world level rule
        /// every requirement uses, else (no caller today) counted live.</summary>
        public static int InContainers(string name, int quality, bool worldLevel)
        {
            if (name != null && worldLevel)
                return StorageIndex.Count(name, quality);
            int sum = 0;
            foreach (Container container in Containers())
                sum += CountIn(container, name, quality, worldLevel);
            return sum;
        }

        public static int CountIn(Container container, string name, int quality, bool worldLevel)
        {
            return CountIn(container, item => Matches(item, name, quality, worldLevel));
        }

        /// <summary>Items in every reachable container that a station would accept (the game's world level rule applies).</summary>
        public static int CountMatching(Func<ItemDrop.ItemData, bool> accepts)
        {
            int sum = 0;
            foreach (Container container in Containers())
                sum += CountIn(container, item => item.m_worldLevel >= Game.m_worldLevel && accepts(item));
            return sum;
        }

        public static int CountIn(Container container, Func<ItemDrop.ItemData, bool> wanted)
        {
            ContainerRule rule = ReachRules.RuleFor(container);
            int sum = 0;
            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
            {
                if (wanted(item) && rule.Accepts(item))
                    sum += item.m_stack;
            }
            return sum;
        }

        /// <summary>The first stack of a shared name in the reachable containers, nearest container first.</summary>
        public static ItemDrop.ItemData FirstInContainers(string name, int quality, bool worldLevel)
        {
            foreach (Container container in Containers())
            {
                ItemDrop.ItemData found = FirstIn(container, item => Matches(item, name, quality, worldLevel));
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>The first stack the container holds that the caller wants and the prefab's lists allow.</summary>
        public static ItemDrop.ItemData FirstIn(Container container, Func<ItemDrop.ItemData, bool> wanted)
        {
            ContainerRule rule = ReachRules.RuleFor(container);
            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
            {
                if (wanted(item) && rule.Accepts(item))
                    return item;
            }
            return null;
        }
    }
}
