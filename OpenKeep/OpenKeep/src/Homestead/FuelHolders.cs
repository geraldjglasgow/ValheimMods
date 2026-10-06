using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Which loaded containers hold an item of a shared name, shared by every fire that looks for fuel: one index of the
    /// loaded containers answers all of them, instead of each fire scanning every container and reading every chest near
    /// it. A base of a hundred torches with no resin near any of them so costs a lookup per torch. When a fire asks after
    /// any inventory changed (<see cref="InventoryChanges"/>, which a container coming into the world also counts as), at
    /// most once every <see cref="RebuildSeconds"/>, the index takes in again only the containers whose contents changed
    /// (their ZDO's data revision or the revision their inventory was loaded at): the items of a quiet chest are never
    /// walked twice. A fire that misses a chest filled a moment ago finds it on its next look (<see cref="TakeRetry"/>).
    /// Only a hint: the take itself checks the chests again.
    /// </summary>
    public static class FuelHolders
    {
        private const float RebuildSeconds = 2f;

        private sealed class Indexed
        {
            public ulong Stamp;
            public readonly List<string> Names = new List<string>();
        }

        private static readonly Dictionary<string, HashSet<Container>> holders = new Dictionary<string, HashSet<Container>>();
        private static readonly Dictionary<Container, Indexed> indexed = new Dictionary<Container, Indexed>();
        private static readonly List<Container> gone = new List<Container>();
        private static readonly Container[] none = new Container[0];
        private static int builtChange = -1;
        private static float builtAt = float.MinValue;

        /// <summary>The loaded containers that held the item at the last walk, within range of a position, in no order.</summary>
        public static IReadOnlyList<Container> Near(string sharedName, Vector3 position, float range)
        {
            Refresh();
            if (sharedName == null || !holders.TryGetValue(sharedName, out HashSet<Container> all) || all.Count == 0)
                return none;
            float limit = range * range;
            List<Container> near = new List<Container>();
            foreach (Container container in all)
            {
                if (container != null && (container.transform.position - position).sqrMagnitude <= limit)
                    near.Add(container);
            }
            return near;
        }

        private static void Refresh()
        {
            float now = Time.time;
            if (InventoryChanges.Count == builtChange || now - builtAt < RebuildSeconds)
                return;
            builtChange = InventoryChanges.Count;
            builtAt = now;
            DropGone();
            foreach (Container container in ContainerScan.All())
            {
                ulong stamp = StampOf(container);
                if (!indexed.TryGetValue(container, out Indexed entry))
                    indexed[container] = entry = new Indexed { Stamp = ~stamp };
                if (entry.Stamp != stamp)
                    Reindex(container, entry, stamp);
            }
        }

        /// <summary>The ZDO's data revision and the revision the inventory was loaded at, as one number.</summary>
        private static ulong StampOf(Container container)
        {
            ZDO zdo = container.m_nview != null && container.m_nview.IsValid() ? container.m_nview.GetZDO() : null;
            return zdo == null ? 0UL : ((ulong)zdo.DataRevision << 32) | container.m_lastRevision;
        }

        /// <summary>Containers that left the world leave the index.</summary>
        private static void DropGone()
        {
            gone.Clear();
            foreach (Container container in indexed.Keys)
            {
                if (container == null)
                    gone.Add(container);
            }
            foreach (Container container in gone)
            {
                Forget(container, indexed[container]);
                indexed.Remove(container);
            }
        }

        private static void Reindex(Container container, Indexed entry, ulong stamp)
        {
            Forget(container, entry);
            entry.Stamp = stamp;
            Inventory inventory = container.GetInventory();
            if (inventory == null)
                return;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                string name = item?.m_shared?.m_name;
                if (name == null)
                    continue;
                if (!holders.TryGetValue(name, out HashSet<Container> set))
                    holders[name] = set = new HashSet<Container>();
                if (set.Add(container))
                    entry.Names.Add(name);
            }
        }

        private static void Forget(Container container, Indexed entry)
        {
            foreach (string name in entry.Names)
            {
                if (holders.TryGetValue(name, out HashSet<Container> set))
                    set.Remove(container);
            }
            entry.Names.Clear();
        }
    }
}
