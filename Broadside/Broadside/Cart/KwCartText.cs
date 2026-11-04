namespace Broadside
{
    /// <summary>The cart's words (English, as the rest of Broadside; keys shown with the game's bindings).</summary>
    public static class KwCartText
    {
        public const string Full = "The cart has no room for this crate";
        public const string TooBig = "This crate is too big for the cart";
        public const string NotReady = "The cart is not ready yet";
        public const string NoRoom = "No room beside the cart to set the crate down";
        public const string Falling = "The crate is falling";

        /// <summary>The cart's own storage refusing an item put in.</summary>
        public const string NothingIn = "The cart carries crates now: nothing more goes in here";

        /// <summary>A crate riding a cart: its hover's extra line (the game's alternate use).</summary>
        public static string UnloadLine => "\n" + KwArmWords.AltUse + " Unload beside the cart";

        /// <summary>The cart's own storage while it still holds goods: open it to take them out.</summary>
        public static string Draining(string name) =>
            $"{name}\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open\n<color=#888888>Take the goods out: the cart carries crates now, nothing more goes in</color>";

        /// <summary>An empty cart's storage hovered: the cart's own hover, and how it carries goods now.</summary>
        public static string Empty(string cart) => cart + "\n<color=#888888>Carries crates: lower them onto it with a cargo crane</color>";
    }
}
