using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Carries an altar's locked draw - its stars and aspects, a Bountiful one's extras with them - across the one call
    /// that instantiates its boss. The altar patch sets it just before the game's delayed spawn and clears it just after;
    /// the boss wakes inside that call (Awake runs within the instantiate), and the lifecycle patch hands it to the first
    /// boss that claims it, before its controller rolls. Single-threaded and scoped to one call, so no other spawn can
    /// ever pick it up.
    /// </summary>
    internal static class AltarSummon
    {
        private static BossDraw? _pending;

        public static void Begin(BossDraw? draw) => _pending = draw;

        public static void End() => _pending = null;

        public static void Claim(EliteController controller)
        {
            if (_pending != null)
            {
                controller.ForceDraw(_pending.Value);
                _pending = null;
            }
        }
    }
}
