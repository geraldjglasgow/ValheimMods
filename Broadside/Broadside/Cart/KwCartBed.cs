using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's cart (<see cref="KwCargoCart.Prefab"/>) as it carries crates, on every peer (added as the cart wakes,
    /// <see cref="KwCartScene"/>). Its riders (<see cref="KwCartRider"/>, each a crate on its bed) are its children, so they
    /// follow it exactly however it moves (pulled on its owner's game, synced on the others); their colliders join its body,
    /// whose centre of mass is put back as it was each time one comes or goes, so a crate never makes the cart tip sooner
    /// (its turning follows the game's own, from its colliders and the mass the game sets). A rider finds its cart by the
    /// cart's own id (<see cref="KwCargoCart.IdHash"/>, a random long the cart's owner writes once into its ZDO). Before the game
    /// lets go of the cart (unloaded, removed, broken) its riders are let go first (<see cref="KwCartScene"/>), never
    /// destroyed with it. Its own storage (the game's Container on it) takes nothing in any more (<see cref="Refuses"/>, read
    /// by <see cref="KwPileFilter"/>); what it held stays until taken out (<see cref="KwCartStore"/>).
    /// </summary>
    public sealed class KwCartBed : MonoBehaviour
    {
        private const float Again = 2f;
        private static readonly List<KwCartBed> loaded = new List<KwCartBed>();
        private static readonly HashSet<long> gone = new HashSet<long>();
        private static readonly Dictionary<Inventory, KwCartBed> stores = new Dictionary<Inventory, KwCartBed>();
        private static KwCartShape? shape;
        private readonly List<KwCartRider> riders = new List<KwCartRider>();
        private ZNetView? view;
        private Rigidbody? body;
        private Vagon? cart;
        private Inventory? store;
        private float looked = -99f;

        /// <summary>How many crates ride loaded carts on this peer: 0 lets the hover and hover text patches leave at once.</summary>
        public static int Carried { get; private set; }

        /// <summary>The cart's storage inventories this peer has loaded: 0 lets the inventory patches leave at once.</summary>
        public static int Stores => stores.Count;

        /// <summary>Every cart this peer has in its scene now.</summary>
        public static IReadOnlyList<KwCartBed> Loaded => loaded;

        public IReadOnlyList<KwCartRider> Riders => riders;
        public ZDO? Zdo => view != null ? view.GetZDO() : null;

        /// <summary>The cart's own id (0 until its owner has written one).</summary>
        public long Id => Zdo?.GetLong(KwCargoCart.IdHash, 0L) ?? 0L;
        public KwCartShape Shape => shape ?? KwCartShape.Game;
        public Container? Store => cart != null ? cart.m_container : null;
        public Vagon? Cart => cart;

        private void Awake()
        {
            (view, body, cart) = (GetComponent<ZNetView>(), GetComponent<Rigidbody>(), GetComponent<Vagon>());
            if (Zdo == null || body == null || cart == null)
                enabled = false;
        }

        private void Start() => Guard.Run("Broadside cart bed", static b => b.Begin(), this);

        // The bed measured once (the first cart to wake), the storage known, the cart listed.
        private void Begin()
        {
            if (!enabled)
                return;
            shape ??= KwCartShape.Measure(transform, body!);
            store = Store != null ? Store.GetInventory() : null;
            if (store != null)
                stores[store] = this;
            loaded.Add(this);
        }

        private void OnDestroy()
        {
            loaded.Remove(this);
            if (store != null)
                stores.Remove(store);
            Carried -= riders.Count;
            riders.Clear();
        }

        // The owner gives the cart its id once, looked at every Again seconds until it has one.
        private void Update()
        {
            if (Time.time - looked < Again)
                return;
            looked = Time.time;
            ZDO? zdo = Zdo;
            if (zdo != null && zdo.IsOwner() && zdo.GetLong(KwCargoCart.IdHash, 0L) == 0L)
                zdo.Set(KwCargoCart.IdHash, NewId());
            enabled = Id == 0L;
        }

        private static long NewId()
        {
            long id = System.BitConverter.ToInt64(System.Guid.NewGuid().ToByteArray(), 0);
            return id != 0L ? id : 1L;
        }

        /// <summary>The loaded cart of this id, or null.</summary>
        public static KwCartBed? Find(long id)
        {
            if (id == 0L)
                return null;
            foreach (KwCartBed bed in loaded)
            {
                if (bed != null && bed.Zdo != null && bed.Id == id)
                    return bed;
            }
            return null;
        }

        /// <summary>Whether a cart of this id was destroyed this session (its ZDO gone, not just unloaded).</summary>
        public static bool Gone(long id) => id != 0L && gone.Contains(id);

        /// <summary>A cart of this id destroyed (here or elsewhere).</summary>
        public static void Destroyed(long id)
        {
            if (id != 0L)
                gone.Add(id);
        }

        /// <summary>Whether an inventory is a cart's storage, which takes nothing in (never while it loads from its ZDO).</summary>
        public static bool Refuses(Inventory inventory) =>
            stores.Count > 0 && stores.TryGetValue(inventory, out KwCartBed bed) && bed != null && bed.Store != null && !bed.Store.m_loading;

        /// <summary>A crate made this cart's child where it is drawn now (it eases onto its place itself), the body's centre of mass as it was.</summary>
        public void Take(KwCartRider rider)
        {
            Vector3 centre = body!.centerOfMass;
            rider.transform.SetParent(transform, true);
            body.centerOfMass = centre;
            if (riders.Contains(rider))
                return;
            riders.Add(rider);
            Carried++;
        }

        /// <summary>A crate let go where it is in the world, the body's centre of mass as it was.</summary>
        public void Drop(KwCartRider rider)
        {
            Forget(rider);
            if (rider == null || rider.transform.parent != transform)
                return;
            Vector3 centre = body!.centerOfMass;
            rider.transform.SetParent(null, true);
            body.centerOfMass = centre;
        }

        /// <summary>A crate gone (destroyed, or no longer riding), off the list.</summary>
        public void Forget(KwCartRider rider)
        {
            if (riders.Remove(rider))
                Carried--;
        }

        /// <summary>
        /// Every crate let go where it is, before the game destroys this cart's object: when its ZDO is destroyed too
        /// (<paramref name="destroyed"/>), the cart is remembered as gone, and each crate's owner makes it a free crate.
        /// </summary>
        public void LetGo(bool destroyed)
        {
            if (destroyed)
                Destroyed(Id);
            foreach (KwCartRider rider in riders.ToArray())
            {
                if (rider != null)
                    rider.Leave(destroyed);
            }
            Carried -= riders.Count;
            riders.Clear();
        }

        /// <summary>What the crates on it hold (kg), as each crate's inventory has it on this peer.</summary>
        public float Weight()
        {
            float total = 0f;
            foreach (KwCartRider rider in riders)
            {
                Container? box = rider != null ? rider.Box : null;
                if (box != null && box.GetInventory() != null)
                    total += box.GetInventory().GetTotalWeight();
            }
            return total;
        }

        /// <summary>The bed cells its crates take, but one (the crate placing itself).</summary>
        public int Used(GameObject? except)
        {
            int used = 0;
            foreach (KwCartRider rider in riders)
            {
                if (rider != null && rider.gameObject != except)
                    used |= rider.Mask;
            }
            return used;
        }
    }
}
