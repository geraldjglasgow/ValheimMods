namespace DevBridge.Timing
{
    /// <summary>
    /// The game's own clock, worked out the way Game.UpdatePause writes Time.timeScale at the start of every Game.Update:
    /// 0 while the game pauses, 1 with anyone connected, otherwise the scale of its timescale console command.
    /// </summary>
    internal static class GameClock
    {
        /// <summary>The game's own pause (its menu or a cinematic), which it allows only with nobody else connected.</summary>
        internal static bool Paused => Game.instance && ZNet.instance && Game.IsPaused();

        /// <summary>The scale the game runs at when it is not paused.</summary>
        internal static float Running => ZNet.instance && ZNet.instance.GetPeerConnections() > 0 ? 1f : Game.m_timeScale;

        /// <summary>What the game writes this frame; 1 outside a world, as the main menu sets it when it loads.</summary>
        internal static float Scale => !Game.instance || !ZNet.instance ? 1f : Paused ? 0f : Running;
    }
}
