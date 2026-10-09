using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The bench window, opened with E at a Blueprint Bench: the player's own blueprints on the left, the world's shared
    /// pool on the right, each a file explorer (<see cref="LibraryPane"/>, <see cref="PoolPane"/>) drawn in the game's
    /// own panel look (<see cref="BenchUi"/>). While it is open the game treats it as one of its own windows
    /// (WindowInput: free cursor, no attacks or mouse look, Esc closes it, unless the game's name box or yes / no box is
    /// over it, whose Esc it is). F2 renames the one line picked in the side last used, Delete deletes or removes its
    /// picks. It closes when the player walks away, dies, opens the inventory or the map, the bench goes or blueprints
    /// are switched off.
    /// </summary>
    public static class BenchWindow
    {
        private const float Reach = 6f;

        private static BlueprintBench bench;
        private static BenchPaneView focused;

        public static readonly LibraryPane Library = new LibraryPane();
        public static readonly PoolPane Pool = new PoolPane();

        /// <summary>Open at a bench that still stands (a destroyed bench counts as closed).</summary>
        public static bool IsOpen => bench != null && BenchUi.Shown;

        public static void Open(BlueprintBench at)
        {
            if (!BenchUi.Ensure(Library, Pool))
                return;
            bench = at;
            focused = BenchUi.Left;
            BenchPool.Open();
            BenchUi.Show(true);
            Tick();
        }

        public static void Close()
        {
            bench = null;
            BenchDrag.End();
            BenchUi.Show(false);
        }

        /// <summary>Esc while the window is open: it closes, unless a box of the game's is over it (that Esc is the box's).</summary>
        public static void Escape()
        {
            if (!NamePrompt.Showing && !UnifiedPopup.IsVisible() && !UnifiedPopup.WasVisibleThisFrame())
                Close();
        }

        public static void Focus(BenchPaneView pane) => focused = pane;

        public static void Tick()
        {
            if (!BenchUi.Shown)
                return;
            if (!Stays())
            {
                Close();
                return;
            }
            BenchUi.Left.Tick();
            BenchUi.Right.Tick();
            BenchUi.Status.text = Status();
            Keys();
        }

        private static bool Stays()
        {
            Player player = Player.m_localPlayer;
            return bench != null && player != null && !player.IsDead() && BlueprintSettings.Enabled && !InventoryGui.IsVisible()
                && !Minimap.IsOpen() && Vector3.Distance(player.transform.position, bench.transform.position) <= Reach;
        }

        private static void Keys()
        {
            if (focused == null || NamePrompt.Showing || UnifiedPopup.IsVisible() || BenchDrag.Active)
                return;
            if (Input.GetKeyDown(BlueprintRules.RenameKey))
                focused.RenamePicked();
            else if (Input.GetKeyDown(KeyCode.Delete))
                focused.Model.Remove();
        }

        private static string Status()
        {
            if (BenchShare.Busy)
                return BlueprintWords.Format(BenchWords.Sharing, BenchShare.Left);
            if (BenchTake.Busy)
                return BlueprintWords.Format(BenchWords.Taking, BenchTake.Left);
            if (!BenchPool.Loaded)
                return Language.Localize(BenchPool.Silent ? BenchWords.NoAnswer : BenchWords.Asking);
            return Language.Localize(BenchWords.Hint);
        }
    }
}
