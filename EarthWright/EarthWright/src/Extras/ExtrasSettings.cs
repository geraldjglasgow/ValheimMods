using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Extras
{
    /// <summary>
    /// Sections "14. Cultivator" (seed grid, cultivating anywhere, uprooting) and "15. Road Travel" (sprint bonus on
    /// roads). The seed grid is each player's own placement aid and key; everything else changes play and is synced.
    /// </summary>
    public static class ExtrasSettings
    {
        public static ConfigEntry<bool> SeedGrid { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SeedGridKey { get; private set; }
        public static ConfigEntry<float> SeedGridSpacing { get; private set; }
        public static ConfigEntry<bool> CultivateAnywhere { get; private set; }
        public static ConfigEntry<bool> UprootPicksItems { get; private set; }

        public static ConfigEntry<float> DirtRoadBonus { get; private set; }
        public static ConfigEntry<float> PavedRoadBonus { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindCultivator(synced);
            BindRoads(synced);
        }

        private static void BindCultivator(SyncedConfiguration synced)
        {
            SeedGrid = synced.Bind(Sections.Cultivator, "Seed Grid", false,
                "Seeds and saplings snap to a grid on the ground while you place them, so crops line up in rows. Switched in game with the Seed Grid Key.",
                synced: false);
            SeedGridKey = synced.Bind(Sections.Cultivator, "Seed Grid Key", new KeyboardShortcut(KeyCode.I),
                "Turns the seed grid on or off while a seed or sapling is selected in the cultivator's menu.", synced: false);
            SeedGridSpacing = synced.Bind(Sections.Cultivator, "Seed Grid Spacing", 0f,
                "Distance between grid points in metres. 0: each plant's own spacing (twice its grow radius), the closest crops can stand and still grow.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(0f, 10f));
            CultivateAnywhere = synced.Bind(Sections.Cultivator, "Cultivate Anywhere", false,
                "The cultivator's ground entries also work where the game refuses with \"needs dirt\" (bare rock, cleared or paved ground). Plants still need the right biome to grow.");
            UprootPicksItems = synced.Bind(Sections.Cultivator, "Uproot Picks Items", true,
                "Uprooting first picks what a plant carries, so its berries, mushrooms or stones drop. Off: wild plants are removed and nothing drops.");
        }

        private static void BindRoads(SyncedConfiguration synced)
        {
            DirtRoadBonus = synced.Bind(Sections.Roads, "Dirt Road Bonus", 10f,
                "Percent faster sprinting and less sprint stamina on dirt (hoe paths and levelled ground). 0: no bonus.",
                acceptableValues: new AcceptableValueRange<float>(0f, 100f));
            PavedRoadBonus = synced.Bind(Sections.Roads, "Paved Road Bonus", 20f,
                "Percent faster sprinting and less sprint stamina on paved roads. 0: no bonus.",
                acceptableValues: new AcceptableValueRange<float>(0f, 100f));
        }
    }
}
