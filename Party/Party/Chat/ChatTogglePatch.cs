using HarmonyLib;

namespace Party.Chat
{
    /// <summary>While toggle mode is on, a plain typed line goes to the party instead of vanilla "say".</summary>
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
