using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Signs
{
    /// <summary>Section "7. Signs": every setting changes what exists in the shared world, so all are synced and lockable.</summary>
    public static class SignsSettings
    {
        public const string Section = "7. Signs";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> ShowCounts { get; private set; }
        public static ConfigEntry<int> MaxItems { get; private set; }
        public static ConfigEntry<int> MaxCharacters { get; private set; }
        public static ConfigEntry<float> UpdateSeconds { get; private set; }
        public static ConfigEntry<float> Height { get; private set; }
        public static ConfigEntry<float> Rotation { get; private set; }
        public static ConfigEntry<string> EmptyText { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Enabled", false,
                "A vanilla sign above every player-built container that names its contents, kept up to date by the container's owner. Off by default: the signs are real pieces that stay in the world when the mod is removed. Switching off removes every automatic sign as its container loads.");
            ShowCounts = synced.Bind(Section, "Show Counts", false,
                "'Wood 120, Stone 45' instead of 'Wood, Stone'.");
            MaxItems = synced.Bind(Section, "Max Items", 4,
                "How many item kinds a sign lists, most numerous first.", acceptableValues: new AcceptableValueRange<int>(1, 20));
            MaxCharacters = synced.Bind(Section, "Max Characters", 50,
                "The sign text never exceeds this many characters; items that do not fit are replaced by an ellipsis. The vanilla sign editor allows 50.", acceptableValues: new AcceptableValueRange<int>(10, 200));
            UpdateSeconds = synced.Bind(Section, "Update Seconds", 2f,
                "At most one rewrite per container per this many seconds; a change during the wait is applied when the wait ends.", acceptableValues: new AcceptableValueRange<float>(0.5f, 60f));
            Height = synced.Bind(Section, "Height", 0.1f,
                "Metres between the container's top and the sign's bottom.", acceptableValues: new AcceptableValueRange<float>(-2f, 5f));
            Rotation = synced.Bind(Section, "Rotation", 0f,
                "Degrees added to the container's facing for the sign.", acceptableValues: new AcceptableValueRange<float>(0f, 359f));
            EmptyText = synced.Bind(Section, "Empty Text", "",
                "The text of a sign on an empty container. Empty: a blank board.");
        }
    }
}
