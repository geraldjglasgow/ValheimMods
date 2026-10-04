using Hotkeys;

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
            string what = recap.Dealt ? "Killed by" : "Died:";
            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                $"{what} <noparse>{recap.Killer}</noparse>. {OpenWith()}: death recap");
        }

        public static string OpenWith()
        {
            string key = KeyNames.Short(RecapSettings.Key.Value);
            return key.Length > 0 ? key : "/deaths";
        }
    }
}
