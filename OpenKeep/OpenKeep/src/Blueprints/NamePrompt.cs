using System;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Asks the player for a name in the game's own text box (the one signs and portals use): a topic, a starting text
    /// and a length limit; the answer, trimmed, goes to the callback when the player confirms (an empty answer or Esc
    /// calls nothing). Used to name saved builds and folders and to rename them; with the build menu open the box shows
    /// over it, and the menu holds still while <see cref="Showing"/>.
    /// </summary>
    public sealed class NamePrompt : TextReceiver
    {
        private readonly string initial;
        private readonly Action<string> done;

        private NamePrompt(string initial, Action<string> done)
        {
            this.initial = initial ?? "";
            this.done = done;
        }

        /// <summary>
        /// A text box is up: shown now, or shown when this frame began (the game's own test, true for the frame its Esc or
        /// Enter closes it, so that key never reaches anything under it).
        /// </summary>
        public static bool Showing
        {
            get
            {
                TextInput box = TextInput.instance;
                return TextInput.IsVisible() || (box != null && box.m_panel != null && box.m_panel.activeSelf);
            }
        }

        /// <summary>Opens the text box; false when the game's text box is not there (no HUD yet).</summary>
        public static bool Ask(string topic, string initial, int limit, Action<string> done)
        {
            if (TextInput.instance == null)
                return false;
            TextInput.instance.RequestText(new NamePrompt(initial, done), topic, limit);
            return true;
        }

        public string GetText() => initial;

        public void SetText(string text)
        {
            string name = (text ?? "").Trim();
            if (name.Length > 0)
                BlueprintSafe.Run("OpenKeep name prompt", () => done(name));
        }
    }
}
