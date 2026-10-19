using HarmonyLib;
using PatchGuard;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Floating words above animals ("Twins!", "Strong offspring!"). The event happens on the creature's owner, which
    /// may be a dedicated server, so the owner sends a routed RPC to the players near it (<see cref="Keys.RpcHerdCallout"/>, <see cref="NearbyRpc"/>) and every
    /// client whose camera is near the spot draws it (<see cref="FloatingText"/>), if its own Husbandry "Show Callouts"
    /// is on. The words travel as a key and values (<see cref="LangWire"/>), so each player reads them in their own
    /// language. Registered in a ZNet.Awake postfix, where the game creates a fresh ZRoutedRpc for each session.
    /// </summary>
    public static class HerdCallout
    {
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<Vector3, string>(Keys.RpcHerdCallout, Receive);
        }

        /// <summary>Shows the word <paramref name="key"/>, filled with <paramref name="values"/>, to the players near the spot.</summary>
        public static void Send(Vector3 position, string key, params string[] values)
        {
            if (!string.IsNullOrEmpty(key))
                NearbyRpc.Send(position, FloatingText.Range, Keys.RpcHerdCallout, position, LangWire.Message(key, values));
        }

        private static void Receive(long sender, Vector3 position, string text) =>
            Guard.Run("herd callout", () =>
            {
                if (HusbandrySettings.ShowCallouts.Value)
                    FloatingText.Show(position, LangWire.Read(text));
            });
    }
}
