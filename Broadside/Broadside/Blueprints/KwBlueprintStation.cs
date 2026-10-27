using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The dry dock blueprint station (<c>KW_bpstation</c>): a drafting table with a longship's plan on its sloping top,
    /// the workshop's model (<see cref="KwTableModel"/>; without its bundle the generated <see cref="Table"/> in the plank
    /// wood), that stands only within <see cref="Range"/> of a dry dock (a keel block guide or a row of keel blocks,
    /// <see cref="KwBpStationRule"/>). E at it opens its window (<see cref="KwBpStation"/>, <see cref="KwBpShelf"/>): the
    /// player's saved ships in folders beside the world's shared ones, where the ship on the dock is saved as a named
    /// blueprint, a saved one is built onto the dock, the dock is cleared for a new ship, and ships are shared and taken.
    /// The table only designs: removing or breaking it removes the piece and nothing else. Every saved ship is also in the
    /// hammer as a whole ship to build on the water (<see cref="EntryPrefix"/>, <see cref="KwBpHammer"/>).
    /// A ship is every kit piece over the dock but the dock set, the tools, the station and the saved-ship entries
    /// (<see cref="IsShipPiece"/>);
    /// each piece built from a blueprint carries that build's ship id (<see cref="ShipKey"/>), so the whole ship can be
    /// removed at once (<see cref="KwBpShipRemoval"/>).
    /// </summary>
    public static class KwBlueprintStation
    {
        public const string Prefab = "KW_bpstation", EntryPrefix = "KW_bp_", ShipKey = "kw_ship", Label = "Ship blueprint table";
        public const float Range = 12f;
        public static readonly int ShipHash = ShipKey.GetStableHashCode();

        private static readonly HashSet<string> DockSet = new HashSet<string> { "KW_dockguide", "KW_keelcradle" };

        public static IEnumerable<KwDef> All()
        {
            yield return new KwDef(Prefab, Label, "station") { Model = KwTableModel.Wear, Shape = Table, Wood = true, Setup = Setup };
        }

        /// <summary>A kit piece that belongs to a ship: not the dock set, a tool, the station or a saved-ship entry.</summary>
        public static bool IsShipPiece(string name) =>
            name.StartsWith("KW_", StringComparison.Ordinal) && !DockSet.Contains(name) && name != Prefab
            && !name.StartsWith(KwToolPieces.ToolPrefix, StringComparison.Ordinal) && !name.StartsWith(EntryPrefix, StringComparison.Ordinal);

        /// <summary>The table's hammer icon, which the window's ship lines and the hammer's saved ships show too; null before the pieces are made.</summary>
        public static Sprite? Icon()
        {
            GameObject? station = KwPrefabs.All.FirstOrDefault(p => p != null && p.name == Prefab);
            return station != null ? station.GetComponent<Piece>().m_icon : null;
        }

        private static void Setup(GameObject prefab)
        {
            prefab.AddComponent<KwBpStation>();
            prefab.GetComponent<Piece>().m_description =
                $"Ship plans: save the ship on the dry dock as a blueprint, build a saved one onto the dock, clear the dock, and share plans with everyone in the world. Stands within {Range:0} m of a dry dock: a keel block guide or a row of keel blocks. Every saved ship is in the hammer too, to build on the water.";
        }

        // The top's height along x: it rises away from the one drawing at it.
        private static float TopZ(float x) => 0.95f + 0.18f * (x + 0.55f) / 1.1f;

        // The table without its bundle: four legs with stretchers, a sloping top with a plan spread on it and one rolled at
        // its head, and a shelf of rolled plans underneath.
        private static void Table(MeshBuilder b)
        {
            Solid.Sloped(b, -0.55f, 0.55f, -0.8f, 0.8f, TopZ, -0.05f, 0f);
            foreach (float x in new[] { -0.45f, 0.45f })
            {
                foreach (float y in new[] { -0.7f, 0.7f })
                    Solid.Box(b, new Vector3(x - 0.05f, y - 0.05f, 0f), new Vector3(x + 0.05f, y + 0.05f, TopZ(x) - 0.05f));
            }
            foreach (float y in new[] { -0.7f, 0.7f })
                Solid.Box(b, new Vector3(-0.45f, y - 0.03f, 0.2f), new Vector3(0.45f, y + 0.03f, 0.28f));
            Solid.Box(b, new Vector3(-0.5f, -0.75f, 0.38f), new Vector3(0.5f, 0.75f, 0.42f));
            Plans(b);
        }

        private static void Plans(MeshBuilder b)
        {
            Solid.Sloped(b, -0.42f, 0.32f, -0.5f, 0.45f, TopZ, 0f, 0.012f);
            Solid.Log(b, new Vector3(0.38f, -0.52f, TopZ(0.38f) + 0.045f), new Vector3(0.38f, 0.47f, TopZ(0.38f) + 0.045f), 0.04f);
            foreach (float x in new[] { -0.32f, -0.2f, -0.08f, 0.2f })
                Solid.Log(b, new Vector3(x, -0.62f, 0.47f), new Vector3(x, 0.6f, 0.47f), 0.05f);
            Solid.Log(b, new Vector3(-0.26f, -0.55f, 0.56f), new Vector3(-0.26f, 0.5f, 0.56f), 0.05f);
            Solid.Log(b, new Vector3(0.3f, -0.4f, 0.47f), new Vector3(0.38f, 0.5f, 0.47f), 0.045f);
        }
    }
}
