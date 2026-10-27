using System;
using System.Collections.Generic;
using BlueprintShelf;
using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// The blueprint table's window: the BlueprintShelf library's (OpenKeep's Blueprint Bench window) with Broadside's own
    /// names. E at the table opens it: the player's ships on the left (<see cref="KwBpLibrary.Folder"/>, with folders), the
    /// world's shared ships on the right (the server's <c>BepInEx/config/Broadside.SharedShips/&lt;world&gt;/</c>), shared
    /// and taken by dragging across; under them the table's own buttons (<see cref="KwBpStationActions"/>): save the ship
    /// on the dock into the folder shown, build the picked ship onto the dock, clear the dock. RPCs <c>KW_Bench*</c>, the
    /// library's English with the ships' words, ship lines with the table's icon. Made on every machine when the network
    /// wakes (<see cref="KwBpShelfWake"/>); a change made in the window rescans the ships and puts the hammer's saved-ship
    /// entries in again (<see cref="KwBpHammer"/>).
    /// </summary>
    public static class KwBpShelf
    {
        private static Shelf? shelf;
        private static ShelfFolder? files;

        public static Shelf Shelf => shelf ??= Make();

        /// <summary>A ship was saved at the table: the window lists the ships again, and the hammer follows.</summary>
        public static void Changed()
        {
            files?.Changed();
            KwBpHammer.Refresh();
        }

        private static Shelf Make()
        {
            ShelfWords words = ShelfWords.English(ShipWords);
            files = new ShelfFolder(KwBpLibrary.Folder, ".json", words, path => KwBpLibrary.Load(path) != null);
            files.Edited += () => Guard.Run("Broadside ship list", Edited);
            return Shelf.Create(new ShelfSettings
            {
                Name = "Broadside ship table",
                CanvasName = "KW_ShipTable",
                RpcPrefix = "KW_Bench",
                PoolFolder = "Broadside.SharedShips",
                Extension = ".json",
                Files = files,
                Words = words,
                Log = BepInEx.Logging.Logger.CreateLogSource("Broadside ships"),
                Icon = kind => kind == ShelfKind.File ? KwBlueprintStation.Icon() : null,
                Buttons = KwBpStationActions.Buttons(),
            }, new Harmony(Broadside.PluginGuid + ".shelf"));
        }

        private static void Edited()
        {
            KwBpLibrary.Scan();
            KwBpHammer.Refresh();
        }

        // The library's English words for blueprints, said of ships.
        private static readonly Dictionary<string, string> ShipWords = new Dictionary<string, string>
        {
            [ShelfWord.Title] = KwBlueprintStation.Label,
            [ShelfWord.Yours] = "Your ships",
            [ShelfWord.Pool] = "Shared ships",
            [ShelfWord.Home] = "Ships",
            [ShelfWord.Hint] = "Drag ships and folders across to share them or take a copy, onto a folder to move them. Double click opens, right click or F2 renames, Ctrl / Shift + click picks several.",
            [ShelfWord.DeleteAsk] = "Delete $1 from your ships? Folders go with everything in them.",
            [ShelfWord.RemoveAsk] = "Remove $1 from the shared ships? Folders go with everything in them.",
            [ShelfWord.OnlyOwn] = "Ships are shared into your own part",
            [ShelfWord.Nothing] = "No ships in what you picked",
            [ShelfWord.Shared] = "Shared $1 ships",
            [ShelfWord.Took] = "Took $1 ships into $2",
            [ShelfWord.NotTaken] = "$1 ships could not be taken",
            [ShelfWord.NotReadable] = "$1 is not a readable ship",
            [ShelfWord.Full] = "$1 not shared: a player can share at most $2 ships",
            [ShelfWord.Removed] = "Removed $1 from the shared ships ($2 ships)",
            [ShelfWord.NoAnswer] = "The server did not answer (it needs Broadside)",
        };
    }

    /// <summary>
    /// ZNet.Awake postfix: the window is made on every machine (dedicated server included, which keeps the shared ships) as
    /// the first network wakes, so its RPCs are on that network's table; later networks get them from the library.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "Awake")]
    public static class KwBpShelfWake
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                _ = KwBpShelf.Shelf;
            }
            catch (Exception e)
            {
                // Never out of ZNet.Awake: a throw there stops the network from starting.
                Log.Error($"Broadside ship table could not be made: {e}");
            }
        }
    }
}
