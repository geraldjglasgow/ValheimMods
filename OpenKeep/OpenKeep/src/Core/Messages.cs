namespace OpenKeep.Core
{
    /// <summary>
    /// Messages through the game's HUD, localized through the game's word table (the mod's $ok_ tokens are written into
    /// it by the Translations library). Safe before the HUD exists.
    /// </summary>
    public static class Messages
    {
        public static void Center(string text) => Show(MessageHud.MessageType.Center, text);

        public static void TopLeft(string text) => Show(MessageHud.MessageType.TopLeft, text);

        private static void Show(MessageHud.MessageType type, string text)
        {
            if (MessageHud.instance == null || string.IsNullOrEmpty(text))
                return;
            MessageHud.instance.ShowMessage(type, Localization.instance.Localize(text));
        }
    }
}
