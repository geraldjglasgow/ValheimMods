using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The station feeding keys of section "8. Homestead": they move items out of containers, so they are synced and lockable.</summary>
    public static class FeedSettings
    {
        public const string Section = HomesteadModule.Section;

        public const string DefaultSkip = "FineWood, RoundLog";

        public static ConfigEntry<bool> AutoFeedStations { get; private set; }
        public static ConfigEntry<float> AutoFeedRange { get; private set; }
        public static ConfigEntry<string> AutoFeedSkip { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            AutoFeedStations = synced.Bind(Section, "Auto Feed Stations", true,
                "Smelters, blast furnaces, charcoal kilns, eitr refineries, spinning wheels, windmills, the hot tub and every other station of that kind a player built take what they work (ore, scrap, wood, flax, barley, soft tissue) and their fuel (coal, sap, wood) from containers within Auto Feed Range. "
                + "While there is room a station takes one item and one fuel a second, as if someone fed it by hand. Cooking stations, ovens and fermenters are not fed. "
                + "The stations: and containers: rules of OpenKeep.Reach.yml apply, as do the switches of section 0.");
            AutoFeedRange = synced.Bind(Section, "Auto Feed Range", 2f,
                "Metres from the station's outer edge to the middle of a container. 2 reaches the chests standing right beside a station, however big the station is.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 20f));
            AutoFeedSkip = synced.Bind(Section, "Auto Feed Skip", DefaultSkip,
                "Items Auto Feed Stations never takes, comma separated: prefab names (FineWood), name tokens ($item_finewood), prefix:, suffix: or type:. "
                + "The default keeps fine wood and core wood out of the charcoal kiln, which would burn them to coal like plain wood. You can still feed them by hand.");
        }
    }
}
