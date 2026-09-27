using System;
using System.Collections.Generic;
using OpenKeep.Core;
using OpenKeep.Reach;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Takes a fire's fuel out of the containers near the fire (not near a player), nearest first, through Core's
    /// rules: <c>ContainerScan.Nearby</c> (usable: loaded, not open by another player, the local player's ward and
    /// privacy access, the section 0 switches, the prefab enabled), then claim, remove, save. What may be taken is
    /// Reach's: the fire's own fuel item narrowed by the fire prefab's <c>stations:</c> allow/deny
    /// (<c>StationAccepts.Fuel</c>), and per stack the container prefab's allow/deny (<c>ReachRules.RuleFor</c>).
    /// Needs a local player (the access checks are that player's), so a dedicated server takes nothing.
    /// </summary>
    public static class FuelTake
    {
        /// <summary>Removes up to <paramref name="wanted"/> units; returns how many left the containers.</summary>
        public static int Take(Fireplace fire, int wanted)
        {
            Func<ItemDrop.ItemData, bool> accepts = StationAccepts.Fuel(fire, fire.m_fuelItem);
            float range = FuelSettings.AutoFuelRange.Value;
            int taken = 0;
            foreach (Container container in ContainerScan.Nearby(fire.transform.position, range, ContainerUse.Reach))
            {
                if (taken >= wanted)
                    break;
                if (ReachCount.CountIn(container, accepts) <= 0 || !ContainerScan.Claim(container))
                    continue;
                taken += TakeFrom(container, accepts, wanted - taken);
            }
            return taken;
        }

        /// <summary>Removes from the acceptable stacks of one claimed container, then saves it through the game.</summary>
        private static int TakeFrom(Container container, Func<ItemDrop.ItemData, bool> accepts, int wanted)
        {
            ContainerRule rule = ReachRules.RuleFor(container);
            Inventory inventory = container.GetInventory();
            int taken = 0;
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (taken >= wanted)
                    break;
                if (!accepts(item) || !rule.Accepts(item))
                    continue;
                int take = Math.Min(item.m_stack, wanted - taken);
                if (inventory.RemoveItem(item, take))
                    taken += take;
            }
            if (taken > 0)
                ContainerScan.Save(container);
            return taken;
        }
    }
}
