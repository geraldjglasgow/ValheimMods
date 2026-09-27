using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The auto fuel keys of section "8. Homestead": they move items out of containers, so they are synced and lockable.</summary>
    public static class FuelSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<bool> AutoFuel { get; private set; }
        public static ConfigEntry<float> AutoFuelRange { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            AutoFuel = synced.Bind(Section, "Auto Fuel", true,
                "Campfires, hearths, bonfires, braziers, torches, sconces and every other fire a player built refill themselves from containers near them with their own fuel (wood, resin, coal, guck, greydwarf eyes, whatever the fire burns). "
                + "Whenever a whole unit fits, the missing units are taken from the nearest containers within Auto Fuel Range. Fires with endless fuel, fires that cannot be refilled (the resin candle) and fires switched off are left alone. "
                + "The stations: and containers: rules of OpenKeep.Reach.yml apply, as do the switches of section 0.");
            AutoFuelRange = synced.Bind(Section, "Auto Fuel Range", 20f,
                "Metres from the fire within which Auto Fuel takes fuel from containers. 20 is the default Reach range: one wood or resin chest serves a longhouse and its yard.",
                acceptableValues: new AcceptableValueRange<float>(1f, 50f));
        }
    }
}
