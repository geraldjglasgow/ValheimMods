using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Loot;
using EliteCreaturesReborn.Patches;
using UnityEngine;

namespace EliteCreaturesReborn.Horde
{
    /// <summary>
    /// A horde creature's own loot, times 1 / the spawn multiplier (features/horde.md): twice the creatures at half the
    /// loot each, so the horde brings more fights, not more loot. The scaling is <see cref="OwnDrops"/>, the raiders'
    /// own: the creature's rows only, trophies when the trophy switch says so, rounded at random. Called by the drop
    /// list's patch (<see cref="LootPatch"/>) on the dying creature's owner, while its ZDO still answers; a failure is
    /// reported and swallowed, leaving the list as the loot rules made it.
    /// </summary>
    internal static class HordeLoot
    {
        /// <summary>Every kill's drop list passes here: with the horde off it costs one config read.</summary>
        public static void Apply(CharacterDrop drop, List<KeyValuePair<GameObject, int>> result)
        {
            Character character = drop.m_character;
            if (result == null || character == null || !HordeSettings.Enabled)
            {
                return;
            }
            ZNetView? nview = character.m_nview;
            float factor = HordeFactors.Drops(character, nview != null ? nview.GetZDO() : null);
            if (!Mathf.Approximately(factor, 1f))
            {
                SafeCall.Run("CharacterDrop.GenerateDropList horde loot",
                    static (d, r) => OwnDrops.Scale(d, r, 1f / HordeSettings.Spawns), drop, result);
            }
        }
    }
}
