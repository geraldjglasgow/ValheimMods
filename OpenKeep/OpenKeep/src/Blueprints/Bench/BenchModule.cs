using HarmonyLib;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// Entry point of the Blueprint Bench (2026-10-08): a drafting desk of its own (<see cref="BenchModel"/>) where the
    /// players of a world share blueprints. Its window, in the game's own panel look, is two file explorers side by side:
    /// the player's own blueprints and the world's shared pool, by player and folder; drag across to share or take a
    /// copy, organise with folders, rename, move and delete or remove (the sharer or an admin in the pool). The window and
    /// the sharing are the BlueprintShelf library's (since 2026-10-23), set up with OpenKeep's names, files and words
    /// (<see cref="BenchWindow"/>); the pool lives on the server's disk, files travel packed in parts. No setting of its
    /// own: the bench is in the hammer while <see cref="BlueprintSettings.Enabled"/> is on.
    /// </summary>
    public static class BenchModule
    {
        public static void Initialize()
        {
            BenchWindow.Create(new Harmony(Plugin.PluginGuid + ".bench"));
            BlueprintSettings.EnabledEntry.SettingChanged += (_, _) => BenchHammer.Refresh();
        }
    }
}
