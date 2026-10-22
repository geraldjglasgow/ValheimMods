namespace PackPanel.Core
{
    /// <summary>
    /// Messages through the game's HUD, which localizes any $token left in them. The mod's own words come in already in the
    /// player's language (<see cref="Translations.Lang"/>). Safe before the HUD exists.
    /// </summary>
    public static class Messages
    {
        public static void Center(string text) => Show(MessageHud.MessageType.Center, text);

        public static void TopLeft(string text) => Show(MessageHud.MessageType.TopLeft, text);

        private static void Show(MessageHud.MessageType type, string text)
        {
            if (MessageHud.instance == null || string.IsNullOrEmpty(text))
                return;
            MessageHud.instance.ShowMessage(type, text);
        }
    }
}
