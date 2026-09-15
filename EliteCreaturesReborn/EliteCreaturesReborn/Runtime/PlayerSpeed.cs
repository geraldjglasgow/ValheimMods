using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The reference run speed the movement clamp holds every creature under, so none can outrun a fleeing player. It
    /// tracks the local player's own base run speed and remembers the last value seen, so a headless server (which has
    /// no local player) still has a sane figure to clamp against once any player has been observed. Before that, and on
    /// a pure dedicated host, a documented fallback stands in - see DECISIONS.md on why it is what it is.
    /// </summary>
    internal static class PlayerSpeed
    {
        /// <summary>Valheim's player base run speed; the stand-in until a real local player is seen.</summary>
        public const float Fallback = 8f;

        private static float _reference = Fallback;

        public static float Reference()
        {
            Player local = Player.m_localPlayer;
            if (local != null && local.m_runSpeed > 0f)
            {
                _reference = local.m_runSpeed;
            }
            return _reference;
        }
    }
}
