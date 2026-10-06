using System.Collections.Generic;
using OpenKeep.Shared;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The materials a player hands to a construction site, held out of the inventory until the site's owner has put them
    /// in its store: the amounts per item prefab (what is sent) and the stacks taken, as copies with their own quality,
    /// world level and data (what comes back). <see cref="Refund"/> returns every stack to the local player, what does
    /// not fit dropped at the player's feet; it runs once.
    /// </summary>
    internal sealed class SiteParcel
    {
        private readonly List<ItemDrop.ItemData> held = new List<ItemDrop.ItemData>();
        private bool settled;

        /// <summary>Units per item prefab name.</summary>
        public Dictionary<string, int> Given { get; } = new Dictionary<string, int>();

        /// <summary>Takes up to each missing amount out of the inventory, stack by stack (the game's world level rule for
        /// paying); the parcel is empty when the player carries none of it.</summary>
        public static SiteParcel Take(Inventory inventory, Dictionary<string, int> missing)
        {
            SiteParcel parcel = new SiteParcel();
            foreach (KeyValuePair<string, int> need in missing)
            {
                string name = SiteCosts.SharedName(need.Key);
                int taken = name == null ? 0 : parcel.TakeStacks(inventory, name, need.Value);
                if (taken > 0)
                    parcel.Given[need.Key] = taken;
            }
            return parcel;
        }

        private int TakeStacks(Inventory inventory, string name, int wanted)
        {
            int taken = 0;
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (taken >= wanted)
                    break;
                if (item.m_shared.m_name != name || item.m_worldLevel < Game.m_worldLevel || item.m_equipped)
                    continue;
                int n = System.Math.Min(item.m_stack, wanted - taken);
                ItemDrop.ItemData copy = ItemPacket.Copy(item, n);
                if (!inventory.RemoveItem(item, n))
                    continue;
                held.Add(copy);
                taken += n;
            }
            return taken;
        }

        /// <summary>The site did not take the materials: every stack goes back to the local player.</summary>
        public void Refund()
        {
            if (settled)
                return;
            settled = true;
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                Plugin.Log.LogWarning($"OpenKeep: materials held for a construction site could not be returned: no player ({SiteCosts.Describe(Given, 8)})");
                return;
            }
            foreach (ItemDrop.ItemData item in held)
                ChestOps.GiveToPlayer(player, item, ChestOps.NoSlot);
        }

        /// <summary>The site took the materials: nothing comes back.</summary>
        public void Keep() => settled = true;
    }
}
