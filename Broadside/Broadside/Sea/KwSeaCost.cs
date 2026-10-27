using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a sea edit costs: the whole blueprint's cost after it less before it (<see cref="KwBpCost.Of"/>), item by item,
    /// signed (positive: the editing player pays; negative: they get it back). The whole blueprint, so a part whose cost
    /// hangs on another (a bowsprit's jibs on its foremast, an untiered railing on its planking) is right and the ship's
    /// cost always equals what its blueprint costs, which breaking it up gives back. The owner works it out and sends it with
    /// its answer; the asker pays or is paid exactly that, once (<see cref="KwSeaAsk"/>).
    /// </summary>
    public static class KwSeaCost
    {
        public static List<(string Item, int Amount)> Delta(KwBlueprint before, KwBlueprint after)
        {
            List<(string Item, int Amount)> delta = KwBpCost.Of(after);
            foreach ((string item, int amount) in KwBpCost.Of(before))
                KwBpCost.Add(delta, item, -amount);
            return delta.FindAll(c => c.Amount != 0);
        }

        /// <summary>An edit's cost on a ship: the difference, each kind's own correction (<see cref="KwSeaKind.Cost"/>), no zero lines.</summary>
        public static List<(string Item, int Amount)> Of(KwSeaShip ship, KwBlueprint before, KwBlueprint after, KwSeaChanges changes)
        {
            List<(string Item, int Amount)> delta = Delta(before, after);
            foreach (KwSeaKind kind in KwSeaKinds.All)
            {
                KwSeaChanges mine = changes.Mine(kind);
                if (!mine.Empty)
                    KwShipBody.Step("Broadside sea cost " + kind.Name, () => kind.Cost(ship, mine, delta));
            }
            return delta.FindAll(c => c.Amount != 0);
        }

        /// <summary>The items to pay (the positive amounts).</summary>
        public static List<(string Item, int Amount)> Paid(List<(string Item, int Amount)> delta) => delta.FindAll(c => c.Amount > 0);

        /// <summary>The items given back (the negative amounts, as positive).</summary>
        public static List<(string Item, int Amount)> Back(List<(string Item, int Amount)> delta) =>
            delta.FindAll(c => c.Amount < 0).ConvertAll(c => (c.Item, -c.Amount));

        public static void Write(ZPackage pkg, List<(string Item, int Amount)> delta)
        {
            pkg.Write(delta.Count);
            foreach ((string item, int amount) in delta)
            {
                pkg.Write(item);
                pkg.Write(amount);
            }
        }

        public static List<(string Item, int Amount)> Read(ZPackage pkg)
        {
            List<(string Item, int Amount)> delta = new List<(string Item, int Amount)>();
            int count = pkg.ReadInt();
            for (int i = 0; i < count && i < 128; i++)
                delta.Add((pkg.ReadString(), pkg.ReadInt()));
            return delta;
        }

        /// <summary>
        /// Items given to the player: into the inventory as far as it holds them, the rest dropped at their feet (never lost);
        /// an item this game does not know is left out.
        /// </summary>
        public static void Give(Player player, List<(string Item, int Amount)> items)
        {
            foreach ((string item, int amount) in items)
            {
                GameObject? prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(item) : null;
                ItemDrop? drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop != null)
                    Give(player, prefab!, drop, amount);
            }
        }

        // One item in stacks of its own size, each into the inventory or at the player's feet.
        private static void Give(Player player, GameObject prefab, ItemDrop drop, int amount)
        {
            int most = Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            for (int left = amount; left > 0; left -= most)
            {
                ItemDrop.ItemData stack = drop.m_itemData.Clone();
                (stack.m_dropPrefab, stack.m_stack) = (prefab, Mathf.Min(left, most));
                if (!player.GetInventory().AddItem(stack))
                    ItemDrop.DropItem(stack, 0, player.transform.position + player.transform.forward * 0.6f + Vector3.up, Quaternion.identity);
            }
        }
    }
}
