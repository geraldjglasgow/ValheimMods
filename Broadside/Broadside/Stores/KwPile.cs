using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An ammunition pile in the world, on every peer that has its object (a dedicated server too, where it has one): kept in
    /// a list of the loaded piles (<see cref="OnShip"/>: the piles riding a ship, for the captain's broadside), its
    /// inventory known to the item filter (<see cref="KwPileFilter"/>), and the two calls by which the captain's game takes a
    /// load's items from a pile another peer owns (<see cref="KwPileAsk"/>): <see cref="TakeCall"/> to the pile's owner, who
    /// takes what it holds of what was asked (the Container saves it to the ZDO, so every peer sees it gone) and always
    /// answers what it took with <see cref="TookCall"/> (nothing when it no longer owns the pile). A placement ghost (no
    /// ZDO) does none of this.
    /// </summary>
    public sealed class KwPile : MonoBehaviour
    {
        public const string TakeCall = "BS_Pile_Take", TookCall = "BS_Pile_Took";

        /// <summary>The most one ask takes (a load's powder or ball is one to three).</summary>
        public const int MostAsked = 10;

        private static readonly HashSet<KwPile> loaded = new HashSet<KwPile>();

        /// <summary>Which pile it is: its index in <see cref="KwPiles.Defs"/>, set on the prefab and copied with it.</summary>
        public int Kind;

        private Inventory? inventory;

        public ZNetView Net { get; private set; } = null!;
        public Container Box { get; private set; } = null!;
        public KwPileDef Def => KwPiles.Of(Kind);

        /// <summary>Until when this pile is left out of the captain's asks: its owner never answered (a peer without its object).</summary>
        public float QuietUntil { get; set; }

        /// <summary>Whether the item may go in: the one sort the pile holds.</summary>
        public bool Accepts(ItemDrop.ItemData item) => Def.Accepts(item);

        /// <summary>How many of the item (by the name inventories count it by) it holds, as this peer has its inventory.</summary>
        public int Count(string name) => inventory != null ? inventory.CountItems(name) : 0;

        /// <summary>On the pile's owner only: up to `amount` of the item taken out (the Container saves the change to the ZDO); how many were.</summary>
        public int Take(string name, int amount)
        {
            if (inventory == null || amount <= 0 || !Net.IsValid() || !Net.IsOwner())
                return 0;
            int took = Mathf.Min(amount, inventory.CountItems(name));
            if (took > 0)
                inventory.RemoveItem(name, took);
            return took;
        }

        /// <summary>
        /// The piles riding the ship on this peer added to the list: those this game owns, or those another peer owns (but
        /// one whose owner has gone quiet).
        /// </summary>
        public static void OnShip(KwShipRiders ship, List<KwPile> into, bool owned)
        {
            foreach (KwPile pile in loaded)
            {
                if (pile.Net.IsValid() && pile.Net.IsOwner() == owned && (owned || Time.time >= pile.QuietUntil) && pile.Aboard(ship))
                    into.Add(pile);
            }
        }

        private bool Aboard(KwShipRiders ship)
        {
            KwShipRider? rider = GetComponent<KwShipRider>();
            return rider != null && rider.On == ship;
        }

        // In Start, after the ZNetView and the Container have woken (the Container makes its inventory only with a ZDO).
        private void Start()
        {
            ZNetView? view = GetComponent<ZNetView>();
            Container? box = GetComponent<Container>();
            inventory = view != null && view.GetZDO() != null && box != null ? box.GetInventory() : null;
            if (inventory == null)
                return;
            (Net, Box) = (view!, box!);
            Net.Register<int, string, int>(TakeCall, (sender, id, item, amount) => Guard.Run("Broadside pile take", () => Asked(sender, id, item, amount)));
            Net.Register<int, string, int>(TookCall, (sender, id, item, amount) => Guard.Run("Broadside pile took", () => KwPileAsk.Answered(this, id, item, amount)));
            loaded.Add(this);
            KwPileFilter.Track(inventory, this);
        }

        private void OnDestroy()
        {
            loaded.Remove(this);
            if (inventory != null)
                KwPileFilter.Untrack(inventory);
        }

        // On the owner: the captain's game asks for a load's items; what was taken answered (none when this game no longer owns it).
        private void Asked(long sender, int id, string item, int amount)
        {
            int took = Take(item, Mathf.Min(amount, MostAsked));
            Net.InvokeRPC(sender, TookCall, id, item, took);
        }
    }
}
