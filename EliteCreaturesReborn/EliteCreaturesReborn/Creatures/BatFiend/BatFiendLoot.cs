using System.Collections.Generic;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// What a Bat-fiend drops, the game's own items (none of the Wolf's): bat wings, as the game's bats give, and now and
    /// then a blood bag. Placeholder loot for testing; the arena's rewards are its own. An item the game lacks is left out.
    /// </summary>
    internal static class BatFiendLoot
    {
        private static readonly (string item, int min, int max, float chance)[] Drops =
        {
            ("BatWing", 2, 3, 1f),
            ("Bloodbag", 1, 2, 0.5f),
        };

        public static void Fill(CharacterDrop? drop, ZNetScene scene)
        {
            if (drop == null)
            {
                return;
            }
            var drops = new List<CharacterDrop.Drop>();
            foreach (var (item, min, max, chance) in Drops)
            {
                GameObject? prefab = scene.GetPrefab(item);
                if (prefab == null)
                {
                    Log.Warn($"Bat-fiend: the game has no {item}; it is not dropped.");
                    continue;
                }
                drops.Add(new CharacterDrop.Drop { m_prefab = prefab, m_amountMin = min, m_amountMax = max, m_chance = chance, m_levelMultiplier = true });
            }
            drop.m_drops = drops;
        }
    }
}
