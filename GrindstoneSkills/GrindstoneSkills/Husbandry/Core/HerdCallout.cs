using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Floating words above animals ("Twins!", "Strong offspring!"). The event happens on the creature's owner, which
    /// may be a dedicated server, so the owner broadcasts a routed RPC (<see cref="Keys.RpcHerdCallout"/>) and every
    /// client whose camera is near the spot draws it (<see cref="FloatingText"/>), if its own Husbandry "Show Callouts"
    /// is on. Registered in a ZNet.Awake postfix, where the game creates a fresh ZRoutedRpc for each session.
    /// </summary>
    public static class HerdCallout
    {
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<Vector3, string>(Keys.RpcHerdCallout, Receive);
        }

        public static void Send(Vector3 position, string text)
        {
            if (ZRoutedRpc.instance == null || string.IsNullOrEmpty(text))
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Keys.RpcHerdCallout, position, text);
        }

        private static void Receive(long sender, Vector3 position, string text) =>
            Guard.Run("herd callout", () =>
            {
                if (HusbandrySettings.ShowCallouts.Value)
                    FloatingText.Show(position, text);
            });
    }
}
