namespace Translations
{
    /// <summary>
    /// Text that travels to another peer in words, not in the sender's language: a server's announcement must reach each
    /// player in that player's own language. <see cref="Message"/> packs a key and its values into one string for an RPC or
    /// a ZDO; <see cref="Read"/> on the receiver turns it into that machine's language. A value may itself be a game token
    /// (<c>$enemy_troll</c>) or plain text; plain text arriving where a message was expected reads through the game's
    /// localization as before, so older senders still show.
    /// </summary>
    public static class LangWire
    {
        private const char Mark = '\u001F';

        /// <summary>A key and its values as one string for the wire.</summary>
        public static string Message(string key, params string[] values)
        {
            string text = Mark + key.TrimStart('$');
            foreach (string value in values)
            {
                text += Mark + (value ?? "");
            }
            return text;
        }

        /// <summary>A received message in this machine's language: each value localized, then filled into the key's text.</summary>
        public static string Read(string message)
        {
            if (string.IsNullOrEmpty(message) || message[0] != Mark)
            {
                return Localize(message);
            }
            string[] parts = message.Substring(1).Split(Mark);
            object?[] values = new object?[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
                values[i - 1] = Localize(parts[i]);
            }
            return Lang.Format(parts[0], values);
        }

        /// <summary>Whether a string was made by <see cref="Message"/>.</summary>
        public static bool IsMessage(string text) => !string.IsNullOrEmpty(text) && text[0] == Mark;

        private static string Localize(string text) =>
            string.IsNullOrEmpty(text) || text.IndexOf('$') < 0 ? text : global::Localization.instance.Localize(text);
    }
}
