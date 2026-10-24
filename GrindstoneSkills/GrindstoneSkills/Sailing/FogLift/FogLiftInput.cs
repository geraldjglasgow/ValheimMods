using Hotkeys;
using PatchGuard;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Fog Lift key, Sailing's level <see cref="Level"/> active, on the local player's client. Pressed while the game
    /// takes the player's input and the player is aboard a ship, it checks the player may lift the fog and sends it on that
    /// ship: the ship's own RPC to everybody who has the ship loaded (<see cref="FogLiftReceive"/>), with the caller's
    /// player ID. The cooldown is the caller's own. With Sailing off the key does nothing, since it may belong to something
    /// else then; otherwise a refusal says why. Level, time and cooldown are constants (the user's one-setting rule); only
    /// the key is a setting, each player's own.
    /// </summary>
    public static class FogLiftInput
    {
        /// <summary>Sailing level that unlocks Fog Lift.</summary>
        public const float Level = 75f;

        /// <summary>Seconds the fog stays lifted.</summary>
        public const float Duration = 120f;

        /// <summary>Seconds before the same player can lift the fog again.</summary>
        public const float Cooldown = 300f;

        private static float readyAt;

        /// <summary>Seconds until the local player may use it again; 0 when ready.</summary>
        public static float CooldownLeft => Mathf.Max(0f, readyAt - Time.time);

        public static bool Unlockable => SailingSkill.Active;

        /// <summary>Every frame for the local player (<see cref="LocalPlayerTick"/>).</summary>
        public static void Tick(Player player)
        {
            if (Hotkey.Pressed(SailingSettings.FogLiftKey) && player.TakeInput())
                Guard.Run("fog lift key", static p => TrySend(p), player);
        }

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
            readyAt = Time.time + Cooldown;
            ship.m_nview.InvokeRPC(ZNetView.Everybody, Keys.RpcFogLift, player.GetPlayerID());
        }

        /// <summary>Why the local player cannot lift the fog now, or null when they can.</summary>
        private static string Refusal(Ship ship)
        {
            if (SailingSkill.Local() < Level)
                return Lang.Format("gs_sail_foglift_needs", Level.ToString("0"));
            if (ship == null || ship.m_nview == null || !ship.m_nview.IsValid())
                return Lang.Get("gs_sail_foglift_aboard");
            float wait = readyAt - Time.time;
            return wait > 0f ? Lang.Format("gs_sail_foglift_wait", Mathf.CeilToInt(wait)) : null;
        }
    }
}
