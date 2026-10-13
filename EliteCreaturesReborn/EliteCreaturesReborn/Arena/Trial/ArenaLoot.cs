using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Arena creatures drop nothing (features/arena.md section 3: "no loot, trophies or coins until rewards are
    /// designed"): a tagged creature's drop list is emptied, whoever put what in it - the game's table, ECR's loot rules,
    /// a boss's reward and trophies, Gilded's purse, another mod's extras. Called last by the drop list's patch
    /// (<see cref="Patches.LootPatch"/>), on the dying creature's owner, while its ZDO still answers; both the game's
    /// drop at death and a corpse that carries the loot build their list there. The raiders' coins are their own patch's
    /// and an arena creature carries none. What a Thieving creature stole is the players' own and still falls.
    /// </summary>
    internal static class ArenaLoot
    {
        /// <summary>Every kill's drop list passes here: a creature that is no arena creature costs a few field reads and
        /// one ZDO lookup.</summary>
        public static void Apply(CharacterDrop drop, List<KeyValuePair<GameObject, int>> result)
        {
            Character character = drop.m_character;
            ZNetView? nview = character != null ? character.m_nview : null;
            ZDO? zdo = nview != null ? nview.GetZDO() : null;
            if (result != null && ArenaTag.IsArena(zdo))
            {
                result.Clear();
            }
        }
    }
}
