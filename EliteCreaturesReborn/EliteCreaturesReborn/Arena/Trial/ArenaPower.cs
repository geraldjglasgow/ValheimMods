using System.Collections.Generic;
using EliteCreaturesReborn.Raids;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Where a trial begins (features/arena.md section 3, "Getting harder"): at the biome of the strongest gear among the
    /// players in the arena as Hel wakes, read with the raids' own gear check (<see cref="GearTier"/>: each player's
    /// machine publishes its tier in its player's ZDO, from the best metal ever held and the armour worn). Tier 0 (leather,
    /// bone, no metal) is the Meadows, 1 Bronze the Black Forest, 2 Iron the Swamp, 3 Silver the Mountains, 4 Black metal
    /// the Plains, 5 Mistlands gear the Mistlands, 6 Flametal the Ashlands, 7 Bloodgold the Deep North; earlier biomes
    /// are skipped. Read once, on the core's owner, and kept in the core's ZDO: players who come later change nothing.
    /// </summary>
    internal static class ArenaPower
    {
        /// <summary>The trial's first biome, 0 (Meadows) to 7 (the Deep North).</summary>
        public static int StartBiome()
        {
            int best = 0;
            List<Player> players = Player.GetAllPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                if (p != null && !p.IsDead() && ArenaHall.Contains(p.transform.position))
                {
                    best = System.Math.Max(best, GearTier.Full(p));
                }
            }
            return UnityEngine.Mathf.Clamp(best, 0, ArenaTable.Biomes - 1);
        }
    }
}
