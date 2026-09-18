using UnityEngine;

namespace Party.Chat
{
    /// <summary>The "everything you type goes to the party" indicator, shown while toggle mode is on and the chat box is open.</summary>
    public static class ChatIndicator
    {
        private static readonly GUIStyle style = new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold };

        public static void Draw()
        {
            if (!PartyChatState.ToggleModeOn || !IsChatInputOpen())
                return;
            style.normal.textColor = ColorHelper.Parse(PartyConfig.PartyColor.Value);
            GUI.Label(new Rect(16, Screen.height - 90, 300, 24), "[Party Chat] everything you type goes to your party", style);
        }

        private static bool IsChatInputOpen()
        {
            global::Chat chat = global::Chat.instance;
            return chat != null && chat.m_input != null && chat.m_input.gameObject.activeSelf;
        }
    }
}
