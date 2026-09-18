namespace Party.Commands
{
    /// <summary>Prints a possibly multi-line reply into the console/chat window, or the log if neither exists yet.</summary>
    public static class PartyCommandOutput
    {
        /// <summary>Goes to the normal chat window, not the debug console.</summary>
        public static void Print(string text)
        {
            if (global::Chat.instance == null)
            {
                PartyPlugin.Log.LogInfo(text);
                return;
            }
            foreach (string line in text.Split('\n'))
                global::Chat.instance.AddString(line);
        }
    }
}
