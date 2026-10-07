using System.Collections.Generic;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// What a trophy is worth at the Rune Table (rune-table.md section 3): pure essence for the table's one pool, more the later its creature's
    /// biome (Meadows 5, Black Forest 10, Swamp and the sea 15, Mountains 20, Plains 25, Mistlands 30, Ashlands 35).
    /// Boss trophies are not listed: players need them for the Forsaken powers. A trophy the game does not have is
    /// simply never offered.
    /// </summary>
    internal static class TrophyYields
    {
        private static readonly Dictionary<string, int> Yields =
            new Dictionary<string, int>
            {
                // Meadows
                ["TrophyBoar"] = 5, ["TrophyNeck"] = 5, ["TrophyDeer"] = 5,
                // Black Forest
                ["TrophyGreydwarf"] = 10, ["TrophyBjorn"] = 10,
                ["TrophyGreydwarfBrute"] = 10, ["TrophyFrostTroll"] = 10,
                ["TrophyGreydwarfShaman"] = 10, ["TrophyGhost"] = 10,
                ["TrophySkeleton"] = 10, ["TrophySkeletonPoison"] = 10,
                // Swamp and the sea
                ["TrophySurtling"] = 15, ["TrophyBlob"] = 15, ["TrophyLeech"] = 15,
                ["TrophyAbomination"] = 15, ["TrophyWraith"] = 15,
                ["TrophyDraugr"] = 15, ["TrophyDraugrElite"] = 15, ["TrophyDraugrFem"] = 15,
                ["TrophySerpent"] = 15,
                // Mountains
                ["TrophyWolf"] = 20, ["TrophyHatchling"] = 20, ["TrophyUlv"] = 20,
                ["TrophyFenring"] = 20, ["TrophyCultist"] = 20, ["TrophySGolem"] = 20,
                // Plains
                ["TrophyLox"] = 25, ["TrophyGoblin"] = 25, ["TrophyGoblinBrute"] = 25,
                ["TrophyGoblinShaman"] = 25, ["TrophyDeathsquito"] = 25,
                ["TrophyGrowth"] = 25, ["TrophyBjornUndead"] = 25,
                // Mistlands
                ["TrophyHare"] = 30, ["TrophySeekerBrute"] = 30, ["TrophyGjall"] = 30,
                ["TrophySeeker"] = 30, ["TrophyTick"] = 30, ["TrophyDvergr"] = 30,
                // Ashlands
                ["TrophyAsksvin"] = 35, ["TrophyVolture"] = 35, ["TrophyMorgen"] = 35,
                ["TrophyCharredArcher"] = 35, ["TrophyCharredMage"] = 35,
                ["TrophyCharredMelee"] = 35, ["TrophyFallenValkyrie"] = 35,
            };

        /// <summary>The pure essence one of this trophy gives; false for anything else.</summary>
        public static bool TryGet(ItemDrop.ItemData? item, out int amount)
        {
            amount = 0;
            string? prefab = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            return prefab != null && Yields.TryGetValue(prefab, out amount);
        }
    }
}
