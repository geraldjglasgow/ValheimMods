using System;

namespace GrindstoneSkills
{
    /// <summary>
    /// A ship's lifted fog, kept in the ship's ZDO so every client aboard reads the same one, a player who boards while it
    /// lasts included, and it outlives a change of owner or a relog: the world time it ends
    /// (<see cref="Keys.FogLiftUntil"/>). Only the ship's owner writes it. Read every frame while the local player is
    /// aboard, so the key is hashed once.
    /// </summary>
    public static class FogLiftState
    {
        private static readonly int UntilHash = Keys.FogLiftUntil.GetStableHashCode();

        /// <summary>On the ship's owner: the fog stays lifted for the next <paramref name="seconds"/>.</summary>
        public static void Write(ZDO zdo, float seconds)
        {
            DateTime until = ZNet.instance.GetTime().AddSeconds(seconds);
            zdo.Set(UntilHash, until.Ticks);
        }

        /// <summary>Whether the ship's fog is lifted now.</summary>
        public static bool Lifted(Ship ship)
        {
            ZNetView nview = ship != null ? ship.m_nview : null;
            ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            return zdo != null && ZNet.instance != null && zdo.GetLong(UntilHash) > ZNet.instance.GetTime().Ticks;
        }
    }
}
