using System;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The little spell at a caster's wand tip, as shared state: the caster's machine writes when the charge began and when
    /// it was let go into the caster's own ZDO, on the world clock every machine shares (<c>ZNet.GetTime</c>), so every
    /// machine, one that comes near later too, works out the same charge (<see cref="ChargeOrb"/> draws it). Two writes a
    /// cast and one to clear it.
    /// </summary>
    public static class ChargeState
    {
        private static readonly int StartKey = "EliteEquipment.charge".GetStableHashCode();
        private static readonly int EndKey = "EliteEquipment.chargeEnd".GetStableHashCode();

        private static long writtenEnd;

        /// <summary>The little spell shows, charging from <paramref name="pressedAt"/> (game time), let go at
        /// <paramref name="releasedAt"/> or still held (below 0).</summary>
        public static void Show(Player player, float pressedAt, float releasedAt)
        {
            ZDO zdo = Zdo(player);
            if (zdo == null)
                return;
            writtenEnd = releasedAt < 0f ? 0L : Ticks(releasedAt);
            zdo.Set(StartKey, Ticks(pressedAt));
            zdo.Set(EndKey, writtenEnd);
        }

        /// <summary>Let go: the little spell stops growing (written once).</summary>
        public static void Release(Player player, float releasedAt)
        {
            ZDO zdo = Zdo(player);
            if (zdo == null || writtenEnd != 0L)
                return;
            writtenEnd = Ticks(releasedAt);
            zdo.Set(EndKey, writtenEnd);
        }

        public static void Hide(Player player)
        {
            ZDO zdo = Zdo(player);
            if (zdo == null)
                return;
            zdo.Set(StartKey, 0L);
            zdo.Set(EndKey, 0L);
        }

        /// <summary>The charge a player's little spell shows, 0 to 1 (full at the controls' full charge); false when none shows.</summary>
        public static bool Read(Player player, out float charge)
        {
            charge = 0f;
            ZDO zdo = Zdo(player);
            long start = zdo != null ? zdo.GetLong(StartKey, 0L) : 0L;
            if (start == 0L || ZNet.instance == null)
                return false;
            long end = zdo.GetLong(EndKey, 0L);
            long until = end != 0L ? end : ZNet.instance.GetTime().Ticks;
            charge = Mathf.Clamp01((float)((until - start) / (double)TimeSpan.TicksPerSecond) / CasterControls.FullCharge);
            return true;
        }

        /// <summary>A player still charging (the little spell showing, not yet let go): how charged, 0 to 1.</summary>
        public static bool Holding(Player player, out float charge)
        {
            charge = 0f;
            ZDO zdo = Zdo(player);
            return zdo != null && zdo.GetLong(EndKey, 0L) == 0L && Read(player, out charge);
        }

        private static ZDO Zdo(Player player) =>
            player != null && player.m_nview != null && player.m_nview.IsValid() ? player.m_nview.GetZDO() : null;

        /// <summary>A moment in this machine's game time, on the shared world clock.</summary>
        private static long Ticks(float at) =>
            ZNet.instance.GetTime().Ticks - (long)((Time.time - at) * TimeSpan.TicksPerSecond);
    }
}
