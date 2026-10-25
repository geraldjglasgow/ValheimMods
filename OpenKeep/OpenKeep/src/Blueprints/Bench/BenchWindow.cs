using BlueprintShelf;
using Translations;
using UnityEngine;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The bench window, opened with E at a Blueprint Bench: the BlueprintShelf library's window and sharing with OpenKeep's
    /// own names. Its RPCs are <c>OpenKeep_Bench*</c>, the player's side is the hammer's blueprint library
    /// (BepInEx/config/OpenKeep.Blueprints, through <see cref="BenchFiles"/>, so the Blueprints tab follows every change),
    /// the server's pool is BepInEx/config/OpenKeep.SharedBlueprints/&lt;world&gt;/, the words are <c>ok_bench_*</c> and the
    /// tab's (<see cref="BenchWords"/>), the icons the tab's, the canvas <c>OpenKeep_BlueprintBench</c>. It stays open while
    /// blueprints are on and the player is within 6 m; F2 renames. Made on every machine when the plugin wakes.
    /// </summary>
    public static class BenchWindow
    {
        public static Shelf Shelf { get; private set; }

        /// <summary>Open at a bench that still stands (a destroyed bench counts as closed).</summary>
        public static bool IsOpen => Shelf != null && Shelf.IsOpen;

        public static void Create(HarmonyLib.Harmony harmony) => Shelf = Shelf.Create(Settings(), harmony);

        public static void Open(BlueprintBench at) => Shelf?.Open(at);

        private static ShelfSettings Settings() => new ShelfSettings
        {
            Name = "OpenKeep blueprint bench",
            CanvasName = "OpenKeep_BlueprintBench",
            RpcPrefix = "OpenKeep_Bench",
            PoolFolder = "OpenKeep.SharedBlueprints",
            Extension = ".json",
            Files = new BenchFiles(),
            Words = new ShelfWords { Key = BenchWords.Key, Text = Lang.Get },
            Limits = new ShelfLimits { NameLimit = BlueprintFiles.NameLimit },
            Log = Plugin.Log,
            Enabled = () => BlueprintSettings.Enabled,
            Title = BlueprintEntries.Title,
            Icon = Icon,
            RenameKey = BlueprintRules.RenameKey,
        };

        private static Sprite Icon(ShelfKind kind) => BlueprintIcons.Get(kind == ShelfKind.Up ? BlueprintIcons.FolderUp
            : kind == ShelfKind.File ? BlueprintIcons.Blueprint : BlueprintIcons.Folder);
    }
}
