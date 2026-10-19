using HarmonyLib;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Short words floating above the water, drawn with the game's floating damage text (<see cref="FloatingText"/>), if
    /// the viewer's own Fishing "Show Callouts" is on, and announcements for the whole server.
    /// <list type="bullet">
    /// <item><see cref="ShowLocal"/>: the angler's own events ("Perfect strike!", "Spent!"), on the angler's client only.</item>
    /// <item><see cref="Broadcast"/>: events everybody near should see (a big one, a snag landed): a routed RPC to
    /// the players near it (<see cref="Keys.RpcFishCallout"/>, <see cref="NearbyRpc"/>); each client with a camera within <see cref="FloatingText.Range"/> of
    /// the spot draws it.</item>
    /// <item><see cref="Announce"/>: a line top left for every player on the server (<see cref="Keys.RpcFishAnnounce"/>),
    /// for a legendary catch. A dedicated server shows nothing.</item>
    /// </list>
    /// A broadcast or an announcement travels as a word key and its values (<see cref="LangWire"/>), so each player reads
    /// it in their own language. The routed RPCs are registered in a ZNet.Awake postfix, where the game creates a fresh
    /// ZRoutedRpc for each session.
    /// </summary>
    public static class FishCallout
    {
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                ZRoutedRpc.instance?.Register<Vector3, string>(Keys.RpcFishCallout, Receive);
                ZRoutedRpc.instance?.Register<string>(Keys.RpcFishAnnounce, ReceiveAnnouncement);
            }
        }

        /// <summary>Shows <paramref name="text"/>, in this player's language, at <paramref name="position"/> on this client only.</summary>
        public static void ShowLocal(Vector3 position, string text)
        {
            if (FishingSettings.ShowCallouts.Value)
                FloatingText.Show(position, text);
        }

        /// <summary>Shows the word <paramref name="key"/> with its values at <paramref name="position"/> to every player near it, this one included.</summary>
        public static void Broadcast(Vector3 position, string key, params string[] values)
        {
            if (!string.IsNullOrEmpty(key))
                NearbyRpc.Send(position, FloatingText.Range, Keys.RpcFishCallout, position, LangWire.Message(key, values));
        }

        /// <summary>Shows the word <paramref name="key"/> with its values top left to every player on the server, this one included.</summary>
        public static void Announce(string key, params string[] values)
        {
            if (ZRoutedRpc.instance != null && !string.IsNullOrEmpty(key))
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Keys.RpcFishAnnounce, LangWire.Message(key, values));
        }

        private static void Receive(long sender, Vector3 position, string text) =>
            HookGuard.Run("fish callout", () => ShowLocal(position, LangWire.Read(text)));

        private static void ReceiveAnnouncement(long sender, string text) =>
            HookGuard.Run("fish announcement", () => Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, LangWire.Read(text)));
    }
}
