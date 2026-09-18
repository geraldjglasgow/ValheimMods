using UnityEngine;
using Party.Server;

namespace Party.UI
{
    /// <summary>The accept/decline popup for an incoming <c>Party_InvitePrompt</c>, with its own countdown.</summary>
    public static class InvitePromptUI
    {
        private static readonly int WindowId = "Party.InvitePrompt".GetStableHashCode();

        private static string inviterName;
        private static float remainingSeconds;
        private static bool active;

        public static void Show(string inviter, int timeoutSeconds)
        {
            inviterName = inviter;
            remainingSeconds = timeoutSeconds;
            active = true;
        }

        /// <summary>Advances the countdown. Called from Update, never from OnGUI.</summary>
        public static void Tick(float deltaTime)
        {
            if (!active)
                return;
            remainingSeconds -= deltaTime;
            if (remainingSeconds <= 0f)
                active = false;
        }

        public static void Draw()
        {
            if (!active)
                return;
            Rect rect = new Rect(Screen.width / 2f - 150f, Screen.height / 2f - 200f, 300f, 90f);
            GUI.Window(WindowId, rect, DrawWindow, "Party invite");
        }

        private static void DrawWindow(int id)
        {
            GUI.Label(new Rect(10, 20, 280, 24), $"{inviterName} invited you to their party. ({Mathf.CeilToInt(remainingSeconds)}s)");
            if (GUI.Button(new Rect(10, 50, 130, 28), "Accept"))
                Respond(true);
            if (GUI.Button(new Rect(150, 50, 130, 28), "Decline"))
                Respond(false);
        }

        private static void Respond(bool accept)
        {
            active = false;
            if (ZRoutedRpc.instance != null)
                ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcInviteRespond, accept);
        }
    }
}
