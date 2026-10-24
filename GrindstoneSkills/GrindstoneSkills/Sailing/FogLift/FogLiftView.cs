using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// What a lifted fog looks like, on every client whose player is aboard a ship with its fog lifted
    /// (<see cref="FogLiftState"/>): the weather's fog is <see cref="Thinner"/> thinner and the Mistlands mist parts
    /// around the ship (<see cref="FogLiftMist"/>). The game sets the fog each frame from the weather (EnvMan.SetEnv,
    /// RenderSettings.fogDensity), so <see cref="EnvFogPass"/> calls <see cref="Apply"/> right after it and the fog is
    /// scaled from the game's own value every time, easing in and out over <see cref="EaseSeconds"/>. Players ashore and
    /// on other ships see the weather's fog; the weather itself is never changed.
    /// </summary>
    public static class FogLiftView
    {
        /// <summary>How much thinner the weather's fog is while lifted.</summary>
        public const float Thinner = 0.8f;

        /// <summary>Seconds the fog takes to thin out or close in again.</summary>
        private const float EaseSeconds = 3f;

        private static float strength;
        private static float lastTime;

        /// <summary>Right after the game set the fog for this frame.</summary>
        public static void Apply()
        {
            float dt = Mathf.Clamp(Time.time - lastTime, 0f, 0.5f);
            lastTime = Time.time;
            Ship ship = LiftedShip();
            strength = Mathf.MoveTowards(strength, ship != null ? 1f : 0f, dt / EaseSeconds);
            FogLiftMist.Follow(ship);
            if (strength > 0f)
                RenderSettings.fogDensity *= 1f - Thinner * strength;
        }

        /// <summary>The ship the local player is aboard while its fog is lifted, else null.</summary>
        private static Ship LiftedShip()
        {
            if (Player.m_localPlayer == null || !SailingSkill.Active)
                return null;
            Ship ship = Ship.GetLocalShip();
            return FogLiftState.Lifted(ship) ? ship : null;
        }
    }
}
