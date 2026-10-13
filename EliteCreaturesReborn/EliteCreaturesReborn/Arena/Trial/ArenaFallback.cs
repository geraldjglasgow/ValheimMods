using System.Collections.Generic;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The fixed creature list of each biome of the trial, used only when the game's own spawn lists give a biome no
    /// creature (<see cref="ArenaSpawnLists"/>): a mod that empties them, or a game that renames its lists. Every name was
    /// checked in the running game (2026-10-12) as a networked creature with a monster's mind, of a faction hostile to
    /// players. A name this game no longer knows is skipped as the list is weighed.
    /// </summary>
    internal static class ArenaFallback
    {
        private static readonly string[][] Lists =
        {
            new[] { "Greyling", "Neck", "Boar" },
            new[] { "Greydwarf", "Greydwarf_Elite", "Greydwarf_Shaman", "Skeleton", "Troll" },
            new[] { "Draugr", "Draugr_Elite", "Skeleton_Swamps", "Blob", "BlobElite", "Wraith", "Abomination" },
            new[] { "Wolf", "Fenring", "Hatchling", "StoneGolem" },
            new[] { "Goblin", "GoblinBrute", "Deathsquito", "Lox", "Unbjorn" },
            new[] { "Seeker", "SeekerBrute", "Gjall", "Tick" },
            new[]
            {
                "Charred_Melee", "Charred_Archer", "Charred_Twitcher", "Asksvin", "Morgen_NonSleeping", "FallenValkyrie",
                "BlobLava", "Volture",
            },
            new[]
            {
                "Greydwarf_Frozen", "Greydwarf_Shaman_Frozen", "Skeleton_DeepNorth", "Elaking", "JotunWarrior", "Barka",
            },
        };

        /// <summary>The fixed list of the biome at a place of the trial (0 Meadows to 7 Deep North), as a new list.</summary>
        public static List<string> For(int index) =>
            new List<string>(Lists[UnityEngine.Mathf.Clamp(index, 0, Lists.Length - 1)]);
    }
}
