using HarmonyLib;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Short words floating above a tree or a log for everybody near it: "Timber!", "Clean split!", a find. The machine
    /// where it happened sends a routed RPC to the players near it (<see cref="Keys.RpcWoodCallout"/>, <see cref="NearbyRpc"/>); every client with a camera
    /// within <see cref="Range"/> of the spot, and its Woodcutting "Show Callouts" on, draws it with the game's own
    /// floating damage text (<see cref="FloatingText"/>). A dedicated server draws nothing. The callout travels as a word
    /// key and its values (<see cref="LangWire"/>), so each player reads it in their own language.
    /// </summary>
    public static class WoodCallout
    {
        /// <summary>How far away a callout can be seen, in metres.</summary>
        public const float Range = FloatingText.Range;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<Vector3, string>(Keys.RpcWoodCallout, Receive);
        }

        /// <summary>Shows the word <paramref name="key"/> with its values at <paramref name="position"/> to every player near it, this one included.</summary>
        public static void Broadcast(Vector3 position, string key, params string[] values)
        {
            if (!string.IsNullOrEmpty(key))
                NearbyRpc.Send(position, FloatingText.Range, Keys.RpcWoodCallout, position, LangWire.Message(key, values));
        }

        private static void Receive(long sender, Vector3 position, string text) =>
            HookGuard.Run("wood callout", () => Show(position, LangWire.Read(text)));

        private static void Show(Vector3 position, string text)
        {
            if (WoodcuttingSettings.ShowCallouts.Value)
                FloatingText.Show(position, text);
        }
    }
}
