namespace Broadside
{
    /// <summary>
    /// The game's cart carrying crates (2026-11-03, the user's words: "we also need to allow lowering a crate with the crane
    /// onto a cart, and the cart accepts the crate as cargo ... The cart wouldn't have storage anymore, just crates"): its
    /// names, ZDO keys, RPC and numbers in one place (<c>Crane/CRANE.md</c>, "Carts"). Only the game's own <see cref="Prefab"/>
    /// is changed; another mod's cart is left as it is.
    /// </summary>
    public static class KwCargoCart
    {
        /// <summary>The game's cart (its Vagon).</summary>
        public const string Prefab = "Cart";
        public static readonly int PrefabHash = Prefab.GetStableHashCode();

        /// <summary>On the cart: its own id, a random long its owner writes once (the game numbers its ZDOs afresh each time the world loads).</summary>
        public static readonly int IdHash = "kwc_cart_uid".GetStableHashCode();

        /// <summary>On a crate: the cart it rides (the cart's id; 0 when it rides none), its bed cells (bits) and its turn on the bed (degrees).</summary>
        public static readonly int CartHash = "kwc_cart".GetStableHashCode(), SlotHash = "kwc_cart_slot".GetStableHashCode(),
            YawHash = "kwc_cart_yaw".GetStableHashCode();

        /// <summary>RPC on a crate's view, to its owner: set it down beside its cart (where, its turn).</summary>
        public const string UnloadRpc = "KWC_Unload";

        /// <summary>How long a crate's owner waits for its cart to be anywhere near (with the area loaded) before it is a free crate again (s).</summary>
        public const float Patience = 30f;

        /// <summary>How far past its walls a crate goes down beside the cart (m), and how far under the bed the ground there may be.</summary>
        public const float Gap = 0.3f, MostBelow = 2f;
    }
}
