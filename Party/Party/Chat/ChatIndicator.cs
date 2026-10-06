using UnityEngine;

namespace Party.Chat
{
    /// <summary>The toggle-mode indicator, shown while chat is open.</summary>
    public static class ChatIndicator
    {
        private static readonly GUIStyle style = new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold };

        public static bool Visible() => PartyChatState.ToggleModeOn && IsChatInputOpen();

        public static void Draw()
        {
            if (!Visible())
                return;
            style.normal.textColor = ColorHelper.PartyColor();
            GUI.Label(new Rect(16, Screen.height - 90, 300, 24), "[Party Chat] everything you type goes to your party", style);
        }

        private static bool IsChatInputOpen()
        {
            global::Chat chat = global::Chat.instance;
            return chat != null && chat.m_input != null && chat.m_input.gameObject.activeSelf;
        }
    }
}
