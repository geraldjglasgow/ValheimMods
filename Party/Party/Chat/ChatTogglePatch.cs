using HarmonyLib;

namespace Party.Chat
{
    /// <summary>
    /// While party-chat toggle mode is on, a plain typed line (not starting with '/', so slash commands like
    /// <c>/party</c> still work normally) goes to the party instead of vanilla's "say" broadcast.
    /// </summary>
    [HarmonyPatch(typeof(global::Chat), "InputText")]
    public static class ChatTogglePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(global::Chat __instance)
        {
            if (!PartyChatState.ToggleModeOn)
                return true;
            string text = __instance.m_input.text;
            if (text.Length == 0 || text[0] == '/')
                return true;
            PartyChatState.Send(text);
            return false;
        }
    }
}
