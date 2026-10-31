using System;
using System.Collections.Generic;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// Fresh death spots: the zone of a player's new tombstone, kept for KeepMinutes after the death or until the
    /// tombstone is gone, whichever comes first; at most MaxSpots at once, the newest. The server notices a tombstone
    /// when it makes the tombstone's object (TombStone.Awake, a postfix): it loads every player's area
    /// (<see cref="PlayerAreas"/>), and a tombstone appears where its player died, in their area, during the ten
    /// seconds before they respawn; the host's own tombstone is made on the host. So nothing is scanned and no hot method
    /// is patched. The time of death is the one the dying player's game wrote into the tombstone (the game's own
    /// timeOfDeath, in world time), so an old tombstone the server loads again is not taken for a new one; one without
    /// a time is left alone. The spots are checked every count (<see cref="KeptPlaces"/>): one past its time, or whose
    /// tombstone was emptied and destroyed, goes.
    /// </summary>
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Awake))]
    internal static class DeathSpots
    {
        private const int KeepMinutes = 10;
        private const int MaxSpots = 10;
        private const long KeepTicks = KeepMinutes * TimeSpan.TicksPerMinute;

        private struct Spot
        {
            public ZDOID Tomb;
            public Vector2s Zone;
            public long Died;
        }

        // Newest first.
        private static readonly List<Spot> Spots = new List<Spot>();
        private static bool _warned;

        public static int Count => Spots.Count;

        public static Vector2s ZoneAt(int index) => Spots[index].Zone;

        public static void Reset() => Spots.Clear();

        private static void Postfix(TombStone __instance)
        {
            try
            {
                Notice(__instance.m_nview);
            }
            catch (Exception e)
            {
                if (!_warned)
                {
                    _warned = true;
                    HighSeat.Log.LogWarning($"kept places: a tombstone was not noticed: {e}");
                }
            }
        }

        private static void Notice(ZNetView nview)
        {
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            if (zdo == null || !KeptPlaces.Noticing || Has(zdo.m_uid))
            {
                return;
            }
            long died = zdo.GetLong(ZDOVars.s_timeOfDeath, 0L);
            if (died == 0L || ZNet.instance.GetTime().Ticks - died > KeepTicks)
            {
                return;
            }
            Insert(new Spot { Tomb = zdo.m_uid, Zone = zdo.GetSector(), Died = died });
            KeptPlaces.Changed();
        }

        private static bool Has(ZDOID tomb)
        {
            foreach (Spot spot in Spots)
            {
                if (spot.Tomb == tomb)
                {
                    return true;
                }
            }
            return false;
        }

        // In order of death, newest first; past MaxSpots the oldest goes.
        private static void Insert(Spot spot)
        {
            int at = 0;
            while (at < Spots.Count && Spots[at].Died >= spot.Died)
            {
                at++;
            }
            Spots.Insert(at, spot);
            if (Spots.Count > MaxSpots)
            {
                Spots.RemoveAt(Spots.Count - 1);
            }
        }

        /// <summary>Drops the spots past their time and those whose tombstone is gone.</summary>
        public static void Expire()
        {
            long now = ZNet.instance.GetTime().Ticks;
            for (int i = Spots.Count - 1; i >= 0; i--)
            {
                if (now - Spots[i].Died > KeepTicks || ZDOMan.instance.GetZDO(Spots[i].Tomb) == null)
                {
                    Spots.RemoveAt(i);
                }
            }
        }
    }
}
