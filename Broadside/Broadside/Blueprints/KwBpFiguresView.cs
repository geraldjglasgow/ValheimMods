using System.Collections.Generic;
using System.Globalization;
using PatchGuard;
using TMPro;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The figures panel at work (<see cref="KwBpFiguresPanel"/>), on its own object, so it runs only while the table's
    /// window shows. Cheaply: twice a second it finds the dock and its ship pieces and works the dock's figures out again
    /// only when those pieces changed (which pieces they are and each one's data revision, in any order), never while a
    /// ship is being built onto it; the picked ship is read from its file when the pick or the files change; its figures
    /// as built back follow the boxes, a few times a second at most while they are typed in. A number box takes the keys
    /// as the game's own text box does while it is being typed in (<see cref="KwBpFiguresTyping"/>).
    /// </summary>
    internal sealed class KwBpFiguresView : MonoBehaviour
    {
        private const float DockEvery = 0.5f, PickEvery = 0.2f, ShapeEvery = 0.15f;
        private static readonly float[] Defaults = { 0f, 0f, 100f };
        private static int typedFrame = -10;

        private readonly float[] values = (float[])Defaults.Clone();
        private KwBpFiguresParts parts = null!;
        private KwBlueprint? picked;
        private KwBpDockInfo? dock;
        private string pickKey = "", pickName = "";
        private long dockSign = long.MinValue;
        private float dockAt, pickAt, shapeAt;
        private bool shapeDirty = true, broken;

        /// <summary>A number box has the keys (or had them this frame or the last).</summary>
        public static bool Typing => Time.frameCount - typedFrame <= 1;

        public void Wire(KwBpFiguresParts made)
        {
            parts = made;
            for (int i = 0; i < parts.Boxes.Length; i++)
            {
                int which = i;
                parts.Boxes[i].onValueChanged.AddListener(text => Run(() => Typed(which, text)));
                parts.Boxes[i].onEndEdit.AddListener(_ => Run(() => Shown(which)));
            }
            Restart();
        }

        /// <summary>The window opened again: the boxes back to the ship as saved, everything worked out afresh.</summary>
        public void Restart()
        {
            Defaults.CopyTo(values, 0);
            for (int i = 0; i < values.Length; i++)
                Shown(i);
            KwBpFiguresPanel.Set(KwBpShape.Same);
            (dockSign, pickKey, dockAt, pickAt, shapeAt, shapeDirty) = (long.MinValue, "", 0f, 0f, 0f, true);
        }

        // A throw is reported once and stops the panel, rather than once a frame.
        private void Update()
        {
            if (broken || !KwBpShelf.Shelf.IsOpen)
                return;
            broken = true;
            Guard.Run("Broadside ship figures", Tick);
            broken = false;
        }

        private void Tick()
        {
            Boxes();
            float now = Time.time;
            if (now >= dockAt)
            {
                dockAt = now + DockEvery;
                Dock();
            }
            if (now >= pickAt)
            {
                pickAt = now + PickEvery;
                Pick();
            }
            if (shapeDirty && now >= shapeAt)
            {
                (shapeAt, shapeDirty) = (now + ShapeEvery, false);
                ShowPicked();
            }
        }

        // Each box keeps its character rule (the game's input field adds its own when it starts) and says when it has the keys.
        private void Boxes()
        {
            foreach (TMP_InputField box in parts.Boxes)
            {
                if (box.onValidateInput != KwBpFiguresTyping.Rule)
                    box.onValidateInput = KwBpFiguresTyping.Rule;
                if (box.isFocused)
                    typedFrame = Time.frameCount;
            }
        }

        private void Dock()
        {
            Component? table = KwBpShelf.Shelf.Station;
            dock = table != null ? KwBpDock.Find(table.transform.position) : null;
            if (dock == null || KwBpJobs.Busy)
            {
                parts.DockHead.text = KwBpFiguresText.Header(dock == null ? "No dry dock" : "On the dock: building...");
                if (dock == null)
                    parts.DockBody.text = KwBpFiguresText.Quiet($"No keel block guide or row of keel blocks within {KwBlueprintStation.Range:0} m.");
                dockSign = dock == null ? long.MinValue : dockSign;
                return;
            }
            List<Piece> pieces = KwBpDock.Ship(dock);
            long sign = Sign(pieces);
            if (sign == dockSign)
                return;
            dockSign = sign;
            parts.DockHead.text = KwBpFiguresText.Header(pieces.Count == 0 ? "The dock is empty" : "The ship on the dock");
            parts.DockBody.text = pieces.Count == 0 ? KwBpFiguresText.Quiet("Build a ship on it, or a saved one onto it, to see its figures.")
                : Figures(KwBlueprint.Capture("dock", dock.Frame, KwBpStrays.Aboard(dock)));
            shapeDirty = true;
        }

        // Which pieces are on the dock and each one's data revision, in any order.
        private static long Sign(List<Piece> pieces)
        {
            long sign = pieces.Count;
            foreach (Piece piece in pieces)
            {
                ZDO? zdo = piece != null && piece.m_nview != null ? piece.m_nview.GetZDO() : null;
                if (zdo != null)
                    sign += unchecked((long)zdo.m_uid.GetHashCode() * 2654435761L + zdo.DataRevision * 40503L);
            }
            return sign;
        }

        private void Pick()
        {
            List<string> files = KwBpShelf.Shelf.PickedFiles;
            string key = files.Count == 1 ? files[0] + "#" + KwBpLibrary.Version : "#" + files.Count;
            if (key == pickKey)
                return;
            pickKey = key;
            pickName = files.Count == 1 ? files[0] : "";
            picked = files.Count == 1 ? KwBpLibrary.Load(files[0]) : null;
            shapeDirty = true;
        }

        private void ShowPicked()
        {
            parts.Note.text = "";
            if (picked == null)
            {
                parts.PickHead.text = KwBpFiguresText.Header(pickName.Length == 0 ? "A saved ship" : KwBpFiguresText.Plain(pickName));
                parts.PickBody.text = KwBpFiguresText.Quiet(pickName.Length == 0
                    ? "Pick one of your ships on the left to see its figures as it would be built back onto the dock."
                    : "This ship's file could not be read (the log says why).");
                return;
            }
            KwBlueprint? shaped = KwBpBuildBack.Refused(picked, dock, out string? why);
            KwBpShape shape = KwBpFiguresPanel.Shape;
            string how = shape.IsSame ? "" : shaped == null ? ", as saved" : ", " + shape.Label;
            parts.PickHead.text = KwBpFiguresText.Header(KwBpFiguresText.Plain(pickName) + how);
            parts.PickBody.text = Figures(shaped ?? picked);
            parts.Note.color = KwBpFiguresText.Note;
            parts.Note.text = why != null ? "Cannot be built so: " + why : "";
        }

        private static string Figures(KwBlueprint ship) => KwBpFiguresText.Lines(KwBpFigures.Of(ship, KwBpShipRef.Game()));

        // A typed number that reads becomes the box's value at once; one that does not (empty, a lone "-") waits.
        private void Typed(int which, string text)
        {
            string plain = (text ?? "").Trim().Replace(',', '.');
            if (!float.TryParse(plain, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value))
                return;
            values[which] = value;
            KwBpFiguresPanel.Set(new KwBpShape(values[0], values[1], values[2]));
            shapeDirty = true;
        }

        // The typing ended: the box shows the value it keeps.
        private void Shown(int which)
        {
            string shown = KwBpFiguresPanel.Shown(values[which]);
            if (parts.Boxes[which].text != shown)
                parts.Boxes[which].SetTextWithoutNotify(shown);
        }

        private static void Run(System.Action action) => KwForm.Safe("Broadside ship figures box", action);
    }
}
