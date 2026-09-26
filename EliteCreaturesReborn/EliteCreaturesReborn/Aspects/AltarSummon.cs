using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Carries an altar's locked aspect across the one call that instantiates its boss. The altar patch sets it just
    /// before the game's delayed spawn and clears it just after; the boss wakes inside that call (Awake runs within the
    /// instantiate), and the lifecycle patch hands the aspect to the first boss that claims it, before its controller
    /// rolls. Single-threaded and scoped to one call, so no other spawn can ever pick it up.
    /// </summary>
    internal static class AltarSummon
    {
        private static Aspect? _pending;

        public static void Begin(Aspect? aspect) => _pending = aspect;

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
