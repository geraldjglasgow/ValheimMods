using HarmonyLib;
using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Wind Call key, on the local player's client. Pressed while the game takes the player's input and the player
    /// steers a ship, it checks the player may call the wind and sends the call on that ship: the ship's own RPC to
    /// everybody who has the ship loaded (<see cref="WindCallReceive"/>), with the flat direction the camera looks and
    /// the caller's player ID. The cooldown is the caller's own. Away from a helm, with Sailing off or with Wind Call
    /// turned off (level above 100) the key does nothing, since it may belong to something else then; otherwise a
    /// refusal says why.
    /// </summary>
    public static class WindCallInput
    {
        private static float readyAt;

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class KeyCheck
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer && Hotkey.Pressed(WindCallSettings.Key) && __instance.TakeInput())
                    Guard.Run("wind call key", () => TrySend(__instance));
            }
        }

        public static bool Unlockable => SailingSkill.Active && WindCallSettings.Level.Value <= SailingSkill.MaxLevel;

        private static void TrySend(Player player)
        {
            Ship ship = player.GetControlledShip();
            if (!Unlockable || ship == null || ship.m_nview == null || !ship.m_nview.IsValid())
                return;
            string refusal = Refusal();
            if (refusal != null)
            {
                player.Message(MessageHud.MessageType.Center, refusal);
                return;
            }
            readyAt = Time.time + Mathf.Max(0f, WindCallSettings.Cooldown.Value);
            ship.m_nview.InvokeRPC(ZNetView.Everybody, Keys.RpcWindCall, LookDirection(player), player.GetPlayerID());
        }

        /// <summary>Why the local player cannot call the wind now, or null when they can.</summary>
        private static string Refusal()
        {
            float level = WindCallSettings.Level.Value;
            if (SailingSkill.Local() < level)
                return $"Wind Call needs Sailing {level:0}";
            float wait = readyAt - Time.time;
            return wait > 0f ? $"Wind Call ready in {Mathf.CeilToInt(wait)} s" : null;
        }

        /// <summary>The way the camera looks, flat: its yaw alone, so looking up or down still gives a direction.</summary>
        private static Vector3 LookDirection(Player player)
        {
            Camera camera = Utils.GetMainCamera();
            float yaw = camera != null ? camera.transform.eulerAngles.y : player.transform.eulerAngles.y;
            return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        }
    }
}
