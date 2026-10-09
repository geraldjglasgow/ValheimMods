using HarmonyLib;
using OpenKeep.Blueprints.Sites;
using WindowInput;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// Entry point of the Blueprint Bench (2026-10-08): a drafting desk of its own (<see cref="BenchModel"/>) where the
    /// players of a world share blueprints. Its window, in the game's own panel look, is two file explorers side by side:
    /// the player's own blueprints and the world's shared pool, by player and folder; drag across to share or take a
    /// copy, organise with folders, rename, move and delete or remove (the sharer or an admin in the pool). The pool lives
    /// on the server's disk (<see cref="BenchStore"/>), files travel packed in parts (<see cref="BenchRpc"/>). No setting of
    /// its own: the bench is in the hammer while <see cref="BlueprintSettings.Enabled"/> is on.
    /// </summary>
    public static class BenchModule
    {
        public static void Initialize()
        {
            BenchWords.Register();
            SiteHooks.OnUpdate("OpenKeep blueprint bench", BenchSession.Tick);
            // Installed once per merged copy whoever asks first (the Site planner does too).
            GameWindow.Install(new Harmony(Plugin.PluginGuid + ".bench"));
            GameWindow.Add(() => BenchWindow.IsOpen, BenchWindow.Escape);
            BlueprintSettings.EnabledEntry.SettingChanged += (_, _) => BenchHammer.Refresh();
        }
    }

    /// <summary>Per frame on a player's machine while the blueprint feature is busy: the pool listing, transfers, the window.</summary>
    public static class BenchSession
    {
        public static void Tick()
        {
            BenchWindow.Tick();
            BenchPool.Tick();
            BenchShare.Tick();
            BenchTake.Tick();
        }
    }
}
