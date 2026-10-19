using HarmonyLib;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Short words floating above a rock, drawn with the game's floating damage text (<see cref="FloatingText"/>), if the
    /// viewer's own Pickaxes "Show Callouts" is on.
    /// <list type="bullet">
    /// <item><see cref="ShowLocal"/>: the miner's own events ("Clean strike!", the Echo's distance), on the miner's client
    /// only, in that client's language.</item>
    /// <item><see cref="Broadcast"/>: events everybody near should see (finds), from any machine, the rock's owner
    /// included: a routed RPC to the players near the spot (<see cref="Keys.RpcMineCallout"/>, <see cref="NearbyRpc"/>); each client with a camera within
    /// <see cref="Range"/> of the spot draws it. A dedicated server draws nothing. The words travel as a key and values
    /// (<see cref="LangWire"/>), so each player reads them in their own language.</item>
    /// </list>
    /// The routed RPC is registered in a ZNet.Awake postfix, where the game creates a fresh ZRoutedRpc for each session.
    /// </summary>
    public static class MineCallout
    {
        /// <summary>How far away a callout can be seen, in metres.</summary>
        public const float Range = FloatingText.Range;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<Vector3, string>(Keys.RpcMineCallout, Receive);
        }

        /// <summary>Shows <paramref name="text"/> at <paramref name="position"/> on this client only.</summary>
        public static void ShowLocal(Vector3 position, string text)
        {
            if (PickaxeSettings.ShowCallouts.Value)
                FloatingText.Show(position, text);
        }

        /// <summary>
        /// Shows the word <paramref name="key"/>, filled with <paramref name="values"/>, at <paramref name="position"/> to
        /// every player near it, this one included.
        /// </summary>
        public static void Broadcast(Vector3 position, string key, params string[] values)
        {
            if (!string.IsNullOrEmpty(key))
                NearbyRpc.Send(position, Range, Keys.RpcMineCallout, position, LangWire.Message(key, values));
        }

        private static void Receive(long sender, Vector3 position, string text) =>
            HookGuard.Run("mine callout", () => ShowLocal(position, LangWire.Read(text)));
    }
}
