using Hotkeys;
using Translations;

namespace EliteCreaturesReborn.Recap
{
    /// <summary>
    /// One line at the top left once a recap is made: who killed the player and how to watch it. The game's own top-left
    /// message, so it looks like the game's and fades like it; nothing waits for it.
    /// </summary>
    internal static class RecapNotice
    {
        public static void Show(DeathRecap recap)
        {
            if (!RecapSettings.Notice.Value || MessageHud.instance == null)
            {
                return;
            }
            string key = recap.Dealt ? "ecr_recap_notice_killed" : "ecr_recap_notice_died";
            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                Lang.Format(key, "<noparse>" + recap.Killer + "</noparse>", OpenWith()));
        }

        /// <summary>The recap key's short name, or the chat command where no key is set; never translated.</summary>
        public static string OpenWith()
        {
            string key = KeyNames.Short(RecapSettings.Key.Value);
            return key.Length > 0 ? key : "/deaths";
        }
    }
}
