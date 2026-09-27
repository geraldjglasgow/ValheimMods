using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Costs
{
    /// <summary>
    /// Section "8. Costs", the material side: the entries' own materials and how they grow with the radius, an extra
    /// item per swing, crafting stations per tool and for paving, and stone for the volume raised and the area paved.
    /// Every key changes gameplay, so every key is synced and lockable.
    /// </summary>
    public static class MaterialSettings
    {
        public static ConfigEntry<bool> ChargeMaterials { get; private set; }
        public static ConfigEntry<float> MaterialRadiusExponent { get; private set; }
        public static ConfigEntry<string> ExtraItem { get; private set; }
        public static ConfigEntry<int> ExtraItemAmount { get; private set; }
        public static ConfigEntry<bool> HoeNeedsStations { get; private set; }
        public static ConfigEntry<bool> CultivatorNeedsStations { get; private set; }
        public static ConfigEntry<bool> ShovelNeedsStations { get; private set; }
        public static ConfigEntry<bool> ModdedNeedsStations { get; private set; }
        public static ConfigEntry<bool> PavedNeedsStonecutter { get; private set; }
        public static ConfigEntry<float> StonePerCubicMetre { get; private set; }
        public static ConfigEntry<float> StonePerSquareMetre { get; private set; }
        public static ConfigEntry<string> VolumeItem { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindMaterials(synced);
            BindStations(synced);
            BindVolume(synced);
        }

        private static void BindMaterials(SyncedConfiguration synced)
        {
            ChargeMaterials = synced.Bind(Sections.Costs, "Charge Materials", true,
                "Terrain entries cost their materials per swing: the game's own (stone for Raise ground and Paved road) or the list EarthWright.Costs.yml gives an entry. Off: those materials are free; the extra item and the volume stone below still apply.");
            MaterialRadiusExponent = synced.Bind(Sections.Costs, "Material Radius Exponent", 0f,
                "How an entry's material amounts grow with the brush, as (brush radius / the entry's normal radius) to this power, rounded up and never below 1. 0: the radius does not matter; 2: grows with the area.",
                acceptableValues: new AcceptableValueRange<float>(0f, 3f));
            ExtraItem = synced.Bind(Sections.Costs, "Extra Item", "",
                "Prefab name of an item every terrain swing also costs, for example Stone, Wood or Resin. Empty: none.");
            ExtraItemAmount = synced.Bind(Sections.Costs, "Extra Item Amount", 1,
                "How many of the Extra Item one terrain swing costs.",
                acceptableValues: new AcceptableValueRange<int>(1, 100));
        }

        private static void BindStations(SyncedConfiguration synced)
        {
            HoeNeedsStations = synced.Bind(Sections.Costs, "Hoe Needs Stations", true,
                "Hoe entries need their crafting station nearby as in the game (a workbench for Raise ground, a stonecutter for Paved road). Off: the hoe works anywhere. An entry's stationRequired in EarthWright.Costs.yml overrides this.");
            CultivatorNeedsStations = synced.Bind(Sections.Costs, "Cultivator Needs Stations", true,
                "Cultivator entries need their crafting station nearby, if they have one. Off: the cultivator works anywhere.");
            ShovelNeedsStations = synced.Bind(Sections.Costs, "Shovel Needs Stations", true,
                "Shovel entries need their crafting station nearby, if they have one. Off: the shovel works anywhere.");
            ModdedNeedsStations = synced.Bind(Sections.Costs, "Modded Entries Need Stations", true,
                "Terrain entries added by other mods need their crafting station nearby, if they have one. Off: they work anywhere.");
            PavedNeedsStonecutter = synced.Bind(Sections.Costs, "Paved Road Needs Stonecutter", true,
                "Paving needs a stonecutter nearby, as the game's Paved road does; this also applies when another entry is set to paint paved ground. Off: paving works anywhere.");
        }

        private static void BindVolume(SyncedConfiguration synced)
        {
            StonePerCubicMetre = synced.Bind(Sections.Costs, "Stone Per Cubic Metre Raised", 0f,
                "Volume Cost Item charged per cubic metre of ground raised, for example 0.25. Fractions carry over to the next swing. Lowering is free. 0: off.",
                acceptableValues: new AcceptableValueRange<float>(0f, 10f));
            StonePerSquareMetre = synced.Bind(Sections.Costs, "Stone Per Square Metre Paved", 0f,
                "Volume Cost Item charged per square metre of ground newly paved. Fractions carry over to the next swing. 0: off.",
                acceptableValues: new AcceptableValueRange<float>(0f, 10f));
            VolumeItem = synced.Bind(Sections.Costs, "Volume Cost Item", "Stone",
                "Prefab name of the item the volume costs are paid in.");
        }
    }
}
