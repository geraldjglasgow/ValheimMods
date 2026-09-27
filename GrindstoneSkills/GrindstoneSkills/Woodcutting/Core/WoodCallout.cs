using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Short words floating above a tree or a log for everybody near it: "Timber!", "Clean split!", a find. The machine
    /// where it happened sends a routed RPC to everybody (<see cref="Keys.RpcWoodCallout"/>); every client with a camera
    /// within <see cref="Range"/> of the spot, and "Show Callouts" on, draws it with the game's own floating damage text
    /// (DamageText.AddInworldText, local only). A dedicated server draws nothing. Send the English text; it is shown as
    /// given (GrindstoneSkills has no localization keys yet).
    /// </summary>
    public static class WoodCallout
    {
        /// <summary>How far away a callout can be seen, in metres.</summary>
        public const float Range = 40f;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<Vector3, string>(Keys.RpcWoodCallout, Receive);
        }

        /// <summary>Shows <paramref name="text"/> at <paramref name="position"/> to every player near it, this one included.</summary>
        public static void Broadcast(Vector3 position, string text)
        {
            if (ZRoutedRpc.instance != null && !string.IsNullOrEmpty(text))
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Keys.RpcWoodCallout, position, text);
        }

        private static void Receive(long sender, Vector3 position, string text) =>
            WoodGuard.Run("wood callout", () => Show(position, text));

        private static void Show(Vector3 position, string text)
        {
            Camera camera = Utils.GetMainCamera();
            if (camera == null || DamageText.instance == null || Hud.IsUserHidden() || !WoodcuttingSettings.ShowCallouts.Value)
                return;
            float distance = Vector3.Distance(camera.transform.position, position);
            if (distance <= Range)
                DamageText.instance.AddInworldText(DamageText.TextType.Bonus, position, distance, text, false);
        }
    }
}
