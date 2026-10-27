using System;
using System.Collections.Generic;
using System.IO;
using BlueprintShelf;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The blueprint table's own buttons in its window (<see cref="KwBpShelf"/>), for the local player on the dock nearest
    /// the table (<see cref="KwBpDock.Find"/>): save the ship on the dock under a name typed in the game's text box, into the
    /// folder the window's left side shows (a name already saved there is replaced; the window gets it at once); build
    /// the one ship picked on the left onto the dock when the dock is empty; clear the dock for a new ship, after the
    /// game's yes / no box; build ship: fly the build camera round the dock (<see cref="KwBpCamera"/>), or bring it back.
    /// While a ship is being built the first three wait. Each says in the middle of the screen why it did nothing.
    /// </summary>
    public static class KwBpStationActions
    {
        private const int NameLimit = 40;

        /// <summary>
        /// The row of buttons under the window's two sides, left to right. A new button goes in this list: its word, when
        /// it can be pressed (checked every frame the window is open) and what it does for the local player at the table
        /// (<see cref="Act"/>).
        /// </summary>
        public static List<ShelfButton> Buttons() => new List<ShelfButton>
        {
            new ShelfButton("Save the dock's ship", () => !KwBpJobs.Busy, () => Act(Save)),
            new ShelfButton("Build on the dock", () => !KwBpJobs.Busy && KwBpShelf.Shelf.PickedFiles.Count == 1, () => Act(Build)),
            new ShelfButton("Clear the dock", () => !KwBpJobs.Busy, () => Act(Clear)),
            new ShelfButton("Build ship", () => KwBpCamera.Out || KwBpCamera.Serves(), () => Act(BuildShip)),
        };

        // A button's action for the local player at the table the window is open at.
        private static void Act(Action<Player, Vector3> action)
        {
            Component? table = KwBpShelf.Shelf.Station;
            Player player = Player.m_localPlayer;
            if (table != null && player != null)
                action(player, table.transform.position);
        }

        private static void Save(Player player, Vector3 at)
        {
            KwBpDockInfo? dock = Dock(player, at);
            if (dock == null)
                return;
            List<string> picked = KwBpShelf.Shelf.PickedFiles;
            string initial = picked.Count == 1 ? ShelfPaths.Leaf(picked[0]) : "ship";
            if (KwBpDock.Ship(dock).Count == 0)
                Say(player, "There is no ship on the dock to save");
            else if (!ShelfPrompt.Ask("Save the ship on the dock as", initial, NameLimit, typed => Guard.Run("Broadside ship save", () => SaveAs(player, at, typed))))
                Say(player, "The game's text box is not ready");
        }

        private static void SaveAs(Player player, Vector3 at, string typed)
        {
            KwBpDockInfo? dock = Dock(player, at);
            List<Piece> ship = dock != null ? KwBpStrays.Aboard(dock) : new List<Piece>();
            if (dock == null || ship.Count == 0)
            {
                Say(player, "There is no ship on the dock to save");
                return;
            }
            string name = ShelfPaths.Join(KwBpShelf.Shelf.Folder, KwBpLibrary.SafeName(typed));
            bool replaced = File.Exists(KwBpLibrary.PathOf(name));
            string? error = KwBpLibrary.Save(KwBlueprint.Capture(name, dock.Frame, ship));
            if (error != null)
            {
                Say(player, $"{name} could not be saved: {error}");
                return;
            }
            KwBpShelf.Changed();
            Say(player, $"Saved {name}{(replaced ? " (replaced)" : "")}: {ship.Count} pieces, in the hammer to build");
        }

        private static void Build(Player player, Vector3 at)
        {
            List<string> picked = KwBpShelf.Shelf.PickedFiles;
            KwBpDockInfo? dock = Dock(player, at);
            if (dock == null || picked.Count != 1)
                return;
            if (KwBpDock.Ship(dock).Count > 0)
                Say(player, "The dock is not empty: clear it first");
            else if (KwBpJobs.Busy)
                Say(player, "Still building the last ship");
            else
                Start(player, dock, picked[0]);
        }

        private static void Start(Player player, KwBpDockInfo dock, string name)
        {
            KwBlueprint? blueprint = KwBpLibrary.Load(name);
            if (blueprint == null)
            {
                Say(player, $"{name} could not be read (the log says why)");
                return;
            }
            // longer, wider or scaled as the figures panel's boxes say (KwBpBuildBack); refused with the reason
            KwBlueprint? shaped = KwBpBuildBack.Shaped(player, blueprint, dock);
            if (shaped == null)
                return;
            Say(player, $"Building {name}{KwBpBuildBack.Words}: {shaped.Pieces.Count} pieces");
            KwBpJobs.Build(player, shaped, dock.Frame.position, dock.Frame.rotation);
        }

        private static void Clear(Player player, Vector3 at)
        {
            KwBpDockInfo? dock = Dock(player, at);
            int pieces = dock != null ? KwBpDock.Ship(dock).Count : 0;
            if (dock == null)
                return;
            if (pieces == 0)
            {
                Say(player, "The dock is already empty");
                return;
            }
            KwBpShelf.Shelf.Confirm("Clear the dock", $"Remove all {pieces} ship pieces from the dock? Nothing is refunded.", () =>
                KwBpJobs.Remove(player, KwBpDock.Ship(dock), "Dock cleared"));
        }

        // The build camera round the dock (KwBpCamera); pressed while it is out, the view comes back.
        private static void BuildShip(Player player, Vector3 at)
        {
            if (KwBpCamera.Out)
                KwBpCamera.Leave();
            else if (Dock(player, at) is KwBpDockInfo dock)
                KwBpCamera.Enter(player, dock);
        }

        private static KwBpDockInfo? Dock(Player player, Vector3 at)
        {
            KwBpDockInfo? dock = KwBpDock.Find(at);
            if (dock == null)
                Say(player, $"No dry dock (a keel block guide or a row of keel blocks) within {KwBlueprintStation.Range:0} m of the table");
            return dock;
        }

        private static void Say(Player player, string text)
        {
            if (player != null)
                player.Message(MessageHud.MessageType.Center, text);
        }
    }
}
