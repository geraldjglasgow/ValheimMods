using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The world save key of section "8. Homestead": only the machine that saves the world (server or host) acts on it.</summary>
    public static class SaveSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<bool> QuickWorldSave { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            QuickWorldSave = synced.Bind(Section, "Quick World Save", true,
                "Shortens the moment the game stands still while the world saves: the save copies only the parts of the world that changed since the last save, "
                + "using every processor core when there is a lot to copy. What is written to disk is the same as the game's own save. "
                + "Only the server's (or the host's) setting matters; off keeps the game's own save.");
        }
    }
}
