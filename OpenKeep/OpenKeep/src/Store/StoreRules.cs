using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Store
{
    /// <summary>The applied OpenKeep.Store.yml: rule lookup by container, the refuse and accept checks and the groups
    /// used for "same group" routing. Before a file applies every container has the default rule and no groups.</summary>
    public static class StoreRules
    {
        private static StoreModel current = new StoreModel();

        public static ItemGroups Groups => current.ItemGroups;

        public static void Apply(StoreModel model)
        {
            current = model ?? new StoreModel();
        }

        public static StoreRule For(Container container)
        {
            string prefab = ContainerScan.PrefabName(container);
            return current.Containers.TryGetValue(prefab, out StoreRule rule) ? rule : StoreRule.Default;
        }

        /// <summary>The container's refuse list names the item: never taken, never routed here.</summary>
        public static bool Refuses(Container container, ItemDrop.ItemData item)
        {
            StoreRule rule = For(container);
            return !rule.Refuse.IsEmpty && rule.Refuse.Matches(item);
        }

        /// <summary>The container's accept list names the item and its refuse list does not.</summary>
        public static bool Accepts(Container container, ItemDrop.ItemData item)
        {
            StoreRule rule = For(container);
            return !rule.Accept.IsEmpty && rule.Accept.Matches(item) && !Refuses(container, item);
        }

        public static bool PicksUp(Container container) => For(container).Pickup;

        /// <summary>The group names the item belongs to, in file order.</summary>
        public static List<string> GroupsOf(ItemDrop.ItemData item)
        {
            List<string> names = new List<string>();
            foreach (string group in current.ItemGroups.Names)
            {
                if (current.ItemGroups.Matches(group, item))
                    names.Add(group);
            }
            return names;
        }
    }
}
