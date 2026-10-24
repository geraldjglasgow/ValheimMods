using EliteCreaturesReborn.Arena.Trial;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// The trial a Helgrind is set to (features/arena.md section 2, "Choosing the trial"; the user, 2026-10-23): E on the
    /// gate steps to the next trial the world has open - Meadows, Black Forest ... up to the latest boss's biome, then
    /// Infinite, then the Meadows again - and Shift + E back (<see cref="ArenaModes"/>). Kept in the gate's own ZDO, so
    /// everyone sees the same choice; the pressing player's machine takes the gate's ownership to write it, as the game's
    /// signs do, past the ward's access check. A player walking through carries the gate's trial into the hall in their
    /// own custom data (<see cref="ArenaKeys.Mode"/>), and waking Hel asks for it.
    /// </summary>
    internal static class GateChoice
    {
        /// <summary>The gate's trial as set; <see cref="ArenaModes.Unset"/> when nobody chose (or no ZDO).</summary>
        public static int Read(ZNetView? view) => view != null && view.IsValid()
            ? view.GetZDO().GetInt(ArenaKeys.GateModeHash, ArenaModes.Unset) : ArenaModes.Unset;

        /// <summary>The gate's trial as the world has it open now.</summary>
        public static int Shown(ZNetView? view) => ArenaModes.Resolve(Read(view), ArenaModes.Highest());

        /// <summary>E (or Shift + E, <paramref name="back"/>) on the gate: the next (previous) open trial. False when the
        /// ward refuses this player.</summary>
        public static bool Step(ZNetView? view, bool back)
        {
            if (view == null || !view.IsValid() || !PrivateArea.CheckAccess(view.transform.position))
            {
                return false;
            }
            int highest = ArenaModes.Highest();
            int now = ArenaModes.Resolve(Read(view), highest);
            int next = back ? Previous(now, highest) : ArenaModes.Next(now, highest);
            view.ClaimOwnership();
            view.GetZDO().Set(ArenaKeys.GateModeHash, next);
            return true;
        }

        /// <summary>The local player walks through: the gate's trial goes with them.</summary>
        public static void Carry(Player player, ZNetView? view) =>
            player.m_customData[ArenaKeys.Mode] = Read(view).ToString(System.Globalization.CultureInfo.InvariantCulture);

        // One step back: Infinite to the highest biome, the Meadows to Infinite.
        private static int Previous(int now, int highest) =>
            now == ArenaModes.Infinite ? highest : now <= 0 ? ArenaModes.Infinite : now - 1;
    }
}
