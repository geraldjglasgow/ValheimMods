using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The lookout key, on the local player's client. Pressed while the game takes the player's input (no chat,
    /// console, text field, menu, inventory or map open), it checks the player may send a pulse and sends it on the
    /// ship they are aboard: the ship's own RPC to everybody who has the ship loaded (<see cref="LookoutPulse"/>). The
    /// cooldown is the sender's own. With Sailing off or the lookout turned off (level above 100) the key does nothing,
    /// since it may belong to something else then; otherwise a refusal says why.
    /// </summary>
    public static class LookoutInput
    {
        private static float readyAt;

        /// <summary>Seconds until the local player may use it again; 0 when ready.</summary>
        public static float CooldownLeft => Mathf.Max(0f, readyAt - Time.time);

        /// <summary>Every frame for the local player (<see cref="LocalPlayerTick"/>).</summary>
        public static void Tick(Player player)
        {
            if (Pressed(player))
                Guard.Run("lookout key", static p => TrySend(p), player);
        }

        public static bool Unlockable => SailingSkill.Active && LookoutSettings.Level.Value <= SailingSkill.MaxLevel;

        private static bool Pressed(Player player) => Hotkey.Pressed(LookoutSettings.Key) && player.TakeInput();

        private static void TrySend(Player player)
        {
            if (!Unlockable)
                return;
            Ship ship = Ship.GetLocalShip();
            string refusal = Refusal(ship);
            if (refusal != null)
            {
                player.Message(MessageHud.MessageType.Center, refusal);
                return;
            }
            readyAt = Time.time + Mathf.Max(0f, LookoutSettings.Cooldown.Value);
            ship.m_nview.InvokeRPC(ZNetView.Everybody, Keys.RpcLookout);
        }

        /// <summary>Why the local player cannot send a pulse now, or null when they can.</summary>
        private static string Refusal(Ship ship)
        {
            float level = LookoutSettings.Level.Value;
            if (SailingSkill.Local() < level)
                return $"The lookout needs Sailing {level:0}";
            if (ship == null || ship.m_nview == null || !ship.m_nview.IsValid())
                return "The lookout works aboard a ship";
            float wait = readyAt - Time.time;
            return wait > 0f ? $"Lookout ready in {Mathf.CeilToInt(wait)} s" : null;
        }
    }
}
