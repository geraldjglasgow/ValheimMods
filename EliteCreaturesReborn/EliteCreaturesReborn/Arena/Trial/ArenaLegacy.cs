using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Worlds from ECR 3.25.0 and 3.26.0 keep their arena core at the old hall's origin, (0, 5000, 0) over the world's
    /// centre (features/arena.md section 4). Once a session, at the keeper's first check on the server, everything of the
    /// old arena still there is removed: the old core and any creature tagged as an arena creature, within the three by
    /// three zones round the old origin and between 4,900 and 5,100 m up (nothing on the ground is touched), with no death
    /// and so no drops. The keeper then places the new core at the arena's own origin. Nothing happens while the arena's
    /// origin is still the old one.
    /// </summary>
    internal static class ArenaLegacy
    {
        /// <summary>Where the arena stood in 3.25.0 and 3.26.0.</summary>
        public static readonly Vector3 OldOrigin = new Vector3(0f, 5000f, 0f);

        /// <summary>Metres up or down from the old origin an old arena object may be.</summary>
        private const float Band = 100f;

        private static ZDOMan? _sweptFor;

        public static void SweepOnce()
        {
            if (ReferenceEquals(_sweptFor, ZDOMan.instance))
            {
                return;
            }
            _sweptFor = ZDOMan.instance;
            if ((ArenaHall.Origin - OldOrigin).sqrMagnitude < 200f * 200f)
            {
                return; // the arena has not moved: its core is the one to keep
            }
            int gone = ArenaZdos.Remove(OldOrigin, 1, Old);
            if (gone > 0)
            {
                Log.Info($"Hel's Trial: removed {gone} object(s) of the old arena at {OldOrigin:F0} (core and creatures)");
            }
        }

        private static bool Old(ZDO zdo)
        {
            float y = zdo.GetPosition().y;
            return y > OldOrigin.y - Band && y < OldOrigin.y + Band
                && (zdo.GetPrefab() == ArenaCorePrefab.PrefabHash || ArenaTag.IsArena(zdo));
        }
    }
}
