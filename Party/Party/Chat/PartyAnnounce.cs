namespace Party.Chat
{
    /// <summary>Prints one party-colored line to the chat window.</summary>
    public static class PartyAnnounce
    {
        public static void Print(string text)
        {
            if (global::Chat.instance != null)
                global::Chat.instance.AddString($"<color={PartyConfig.PartyColor.Value}>[Party] {text}</color>");
        }
    }
}
