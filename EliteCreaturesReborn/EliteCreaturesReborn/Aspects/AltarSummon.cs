using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Carries an altar's locked aspects (a Bountiful one's extras with it) across the one call that instantiates its
    /// boss. The altar patch sets them just before the game's delayed spawn and clears them just after; the boss wakes
    /// inside that call (Awake runs within the instantiate), and the lifecycle patch hands them to the first boss that
    /// claims them, before its controller rolls. Single-threaded and scoped to one call, so no other spawn can ever pick
    /// them up.
    /// </summary>
    internal static class AltarSummon
    {
        private static BossAspects? _pending;

        public static void Begin(BossAspects? aspects) => _pending = aspects;

        public static void End() => _pending = null;

        public static void Claim(EliteController controller)
        {
            if (_pending != null)
            {
                controller.ForceAspect(_pending.Value);
                _pending = null;
            }
        }
    }
}
