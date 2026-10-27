using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The blueprint table's hover text: its name, the dock it serves (its pieces and the ship's, or none in reach), how
    /// many ships are saved, a ship being built, and the Use key that opens the table's window. The key's name goes through
    /// the game's localisation (<c>$KEY_Use</c>), so it follows the player's bindings.
    /// </summary>
    public static class KwBpStationText
    {
        public static string Compose(Vector3 at)
        {
            KwBpDockInfo? dock = KwBpDock.Find(at);
            int ship = dock != null ? KwBpDock.Ship(dock).Count : 0;
            int saved = KwBpLibrary.Names().Count;
            StringBuilder s = new StringBuilder(KwBlueprintStation.Label);
            s.Append('\n').Append(DockLine(dock, ship)).Append('\n').Append(saved == 1 ? "1 ship saved" : saved + " ships saved");
            if (KwBpJobs.Busy)
                s.Append("\n<color=orange>Building a ship...</color>");
            s.Append("\n[<color=yellow><b>$KEY_Use</b></color>] Open the ship plans: save, build, clear and share");
            return s.ToString();
        }

        private static string DockLine(KwBpDockInfo? dock, int ship)
        {
            if (dock == null)
                return $"<color=orange>No dry dock (a keel block guide or a row of keel blocks) within {KwBlueprintStation.Range:0} m</color>";
            return $"Dry dock: {dock.Label}, " + (ship == 0 ? "empty" : ship == 1 ? "1 ship piece" : ship + " ship pieces");
        }
    }
}
