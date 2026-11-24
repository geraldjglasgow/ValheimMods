using HarmonyLib;
using LocalEffects;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Where a charged Greywood rock breaks (<see cref="RockBreak"/>), seen and heard by every player near: the caster's
    /// machine, which decides the break, sends one routed message (<see cref="Rpc"/>, through <see cref="NearbyCast"/>, its
    /// own and not <see cref="CasterFx"/>'s, since the burst needs the flight's direction) and each machine draws a local
    /// copy of the workshop's <see cref="Effect"/> (<c>ee_casterfx_meadows</c>), its +Z along the flight, with the game's
    /// rock crack (<see cref="WandRock.Crack"/>); the game's stone chips (<see cref="WandRock.Chips"/>) while the bundle
    /// lacks it. Nothing is networked but the message; a dedicated server draws nothing.
    /// </summary>
    public static class RockBurst
    {
        public const string Effect = "ee_fx_rock_break";
        private const string Rpc = "EliteEquipment_RockBreak";
        private const float Range = 60f;
        // LocalEffect sizes a flash from this radius; at it, an effect is drawn as authored.
        private const float AsAuthored = 4f;
        // The longest a burst copy may live, should its own timer be missing.
        private const float Life = 6f;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<Vector3, Vector3>(Rpc, Receive);
        }

        /// <summary>Shows the break at <paramref name="at"/>, flying along <paramref name="flight"/>, to every player near, this one too.</summary>
        public static void Send(Vector3 at, Vector3 flight) => NearbyCast.Send(at, Range, true, Rpc, at, flight);

        private static void Receive(long sender, Vector3 at, Vector3 flight) =>
            CasterSafe.Run("rock break effect", () => Show(at, flight));

        private static void Show(Vector3 at, Vector3 flight)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Player.m_localPlayer == null)
                return;
            LocalEffect.Sound(WandRock.Crack, at);
            GameObject burst = CasterFx.Prepared(Effect);
            if (burst == null)
            {
                LocalEffect.Flash(WandRock.Chips, at, AsAuthored);
                return;
            }
            GameObject copy = LocalEffect.Attach(burst, null, at, endless: false);
            if (copy == null)
                return;
            if (flight.sqrMagnitude > 0.0001f)
                copy.transform.rotation = Quaternion.LookRotation(flight);
            Object.Destroy(copy, Life);
        }
    }
}
