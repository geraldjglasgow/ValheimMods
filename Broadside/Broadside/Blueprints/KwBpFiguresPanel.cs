using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The blueprint table's figures panel, beside its window (<see cref="KwBpShelf"/>): what the ship on the dock would be
    /// as a ship from the hammer, and what the ship picked on the left would be as it would be built back onto the dock,
    /// so a design can be tuned without sailing it (<see cref="KwBpFigures"/>, <see cref="KwBpFiguresText"/>): size, top
    /// speed against a karve and in km/h, draught and waterline, freeboard, stability, weight, hull health, turning and
    /// cost. Above the picked ship's figures three boxes take how it is built back (<see cref="Shape"/>): percent longer
    /// amidships, percent wider, and a scale in percent; its figures follow them as they are typed, and a line says when a
    /// part could not be made so (<see cref="KwBpReshape"/>). The numbers go back to the ship as saved each time the window
    /// opens. Local to this player: worked out from what this game has loaded and the player's own files.
    /// </summary>
    public static class KwBpFiguresPanel
    {
        public const string ObjectName = "KW_ShipFigures";

        /// <summary>How the picked ship is to be built back onto the dock ("Build on the dock").</summary>
        public static KwBpShape Shape { get; private set; } = KwBpShape.Same;

        /// <summary>The table's window has just opened: the panel beside it, its numbers back to the ship as saved.</summary>
        public static void Opened() => KwForm.Safe("Broadside ship figures", Open);

        private static void Open()
        {
            Shape = KwBpShape.Same;
            RectTransform? window = Window();
            if (window == null)
                return;
            Transform made = window.Find(ObjectName);
            KwBpFiguresView? view = made != null ? made.GetComponent<KwBpFiguresView>() : null;
            if (view == null)
                view = Make(window);
            view?.Restart();
        }

        internal static void Set(KwBpShape shape) => Shape = shape;

        /// <summary>A number as a box shows it.</summary>
        internal static string Shown(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        // The shelf's window: its canvas under the game's GUI, by the canvas name the table gave it.
        private static RectTransform? Window()
        {
            InventoryGui gui = InventoryGui.instance;
            Transform? canvas = gui != null && gui.transform.parent != null ? gui.transform.parent.Find(KwBpShelf.Shelf.Settings.CanvasName) : null;
            return canvas != null ? canvas.Find("Window") as RectTransform : null;
        }

        private static KwBpFiguresView? Make(RectTransform window)
        {
            KwBpFiguresParts? parts = KwBpFiguresBuild.Make(window);
            if (parts == null)
                return null;
            KwBpFiguresView view = parts.Root.gameObject.AddComponent<KwBpFiguresView>();
            view.Wire(parts);
            return view;
        }
    }

    /// <summary>
    /// The building back's numbers on the table: the saved ship as the panel's boxes shape it (<see cref="KwBpFiguresPanel.Shape"/>),
    /// refused when a part would break its limits or the ship would reach past the dock (the dock counts a piece as its
    /// ship up to 6 m past either end, so the table could no longer save or clear it).
    /// </summary>
    public static class KwBpBuildBack
    {
        private const float Past = 5.5f;

        /// <summary>" (20% longer, at 120%)", or nothing when the ship is built as saved.</summary>
        public static string Words => KwBpFiguresPanel.Shape.IsSame ? "" : $" ({KwBpFiguresPanel.Shape.Label})";

        /// <summary>The ship to build, shaped; null when it cannot be, the player told why.</summary>
        public static KwBlueprint? Shaped(Player player, KwBlueprint blueprint, KwBpDockInfo dock)
        {
            KwBlueprint? shaped = Refused(blueprint, dock, out string? why);
            if (shaped == null && player != null)
                player.Message(MessageHud.MessageType.Center, $"{blueprint.Name} cannot be built so: {why}");
            return shaped;
        }

        /// <summary>The ship shaped to the panel's numbers, or null and why not.</summary>
        public static KwBlueprint? Refused(KwBlueprint blueprint, KwBpDockInfo? dock, out string? why)
        {
            KwBpShape shape = KwBpFiguresPanel.Shape;
            KwBlueprint? shaped = KwBpReshape.Apply(blueprint, shape, out why);
            if (shaped != null && dock != null && !shape.IsSame)
                why = Overhang(shaped, dock);
            return why == null ? shaped : null;
        }

        private static string? Overhang(KwBlueprint ship, KwBpDockInfo dock)
        {
            float fore = float.MinValue, aft = float.MaxValue;
            foreach (KwBpPiece piece in ship.Pieces)
                (fore, aft) = (Mathf.Max(fore, piece.Position.z), Mathf.Min(aft, piece.Position.z));
            if (fore > dock.Length + Past)
                return $"it would reach {KwBpPartText.Num(fore - dock.Length)} m past the dock's fore end: lengthen the dock, or build it shorter";
            return aft < -Past ? $"it would reach {KwBpPartText.Num(-aft)} m past the dock's aft end" : null;
        }
    }
}
