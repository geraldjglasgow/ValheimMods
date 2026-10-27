using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Doorway and Window tools' edit window rows (<see cref="KwForm"/>), opened by a click on an opening, and the
    /// answers read back from them (<see cref="KwOpeningAsk"/>); a new opening is placed with one click and needs none
    /// (<see cref="KwOpeningHover"/>). Only rows that change what the opening looks like, for its shape (the user's words,
    /// 2026-10-25: "We should only have options for editing windows that are relevant to that specific window and that will
    /// actually make a visible change on the screen"): keep it or fill it in (filled in, nothing else); a doorway's door
    /// (none, single, double), its width only with no door (a door's is its own: 1 m single, 2 m double; an open doorway's up
    /// to the free stretch of wall it stands in), its height (never above the wall), its frame; a window's shape, then a
    /// round porthole's diameter, a square's size, a rectangle's or an arched top's width and height, its sill over the
    /// deck inside, its frame (plain timber, heavy timber, iron-banded) and only the covers its shape takes. The rows are
    /// built again when an answer asks others (<see cref="Reshaper"/>). A row's edit (Shift + click) has a Number row too.
    /// </summary>
    public static class KwOpeningForm
    {
        public const string Fill = "fill", Shape = "shape", Width = "width", Height = "height", Sill = "sill", Frame = "frame", Cover = "cover",
            Door = "door", Number = "number";

        private static readonly string[] Shapes = { KwOpening.Round, KwOpening.Square, KwOpening.Rect, KwOpening.Arch };
        private static readonly string[] ShapeNames = { "Round porthole", "Square", "Rectangle", "Arched top" };
        private static readonly string[] Frames = { KwOpening.Plain, KwOpening.Heavy, KwOpening.Iron };
        private static readonly string[] FrameNames = { "Plain timber", "Heavy timber", "Iron-banded" };
        private static readonly string[] Covers = { KwOpening.None, KwOpening.Up, KwOpening.Down, KwOpening.Shutters, KwOpening.Lid };
        private static readonly string[] CoverNames = { "None", "Lid hinged at its top (flaps up)", "Lid hinged at its bottom (flaps down)", "Side shutters", "Round lid" };
        private static readonly string[] Doors = { KwOpening.None, KwOpening.Leaf, KwOpening.DoubleDoor };
        private static readonly string[] DoorNames = { "None", "Single door (1 m)", "Double door (2 m)" };

        /// <summary>
        /// The Doorway tool's rows for a doorway clicked: fill in first; unless filled in, the door, the width only with no
        /// door (up to widest), the height (up to room high), the frame.
        /// </summary>
        public static List<KwField> Doorway(KwOpeningAsk ask, float room, float widest)
        {
            List<KwField> fields = new List<KwField> { FillRow("Doorway", ask) };
            if (ask.Filled)
                return fields;
            bool open = ask.Covered == KwOpening.None;
            float most = Mathf.Max(open ? KwOpeningTypes.OpenLeastHigh : KwOpeningShape.DoorMinHigh, room);
            fields.Add(KwField.Choice(Door, "Door", DoorNames, Index(Doors, ask.Covered)));
            if (open)
            {
                float wide = Mathf.Max(KwOpeningTypes.OpenLeastWide, widest);
                fields.Add(KwField.Number(Width, "Width", Mathf.Min(ask.Wide, wide), KwOpeningTypes.OpenLeastWide, wide, Step(Mathf.Min(ask.Wide, wide), 0.05f)));
            }
            float least = open ? KwOpeningTypes.OpenLeastHigh : KwOpeningShape.DoorMinHigh, tall = Mathf.Clamp(ask.Tall, least, most);
            fields.Add(KwField.Number(Height, "Height", tall, least, most, Step(tall, 0.05f)));
            fields.Add(KwField.Choice(Frame, "Frame", FrameNames, Index(Frames, ask.Framed)));
            return fields;
        }

        /// <summary>
        /// The Window tool's rows for a window clicked: fill in first; unless filled in, its shape, the sizes the shape has, its
        /// sill over the deck inside (over the keel line with no deck), its frame and the covers the shape takes.
        /// </summary>
        public static List<KwField> Window(KwOpeningAsk ask, bool floor)
        {
            List<KwField> fields = new List<KwField> { FillRow("Window", ask) };
            if (ask.Filled)
                return fields;
            fields.Add(KwField.Choice(Shape, "Shape", ShapeNames, Index(Shapes, ask.Kind)));
            fields.AddRange(Sizes(ask));
            fields.Add(KwField.Number(Sill, floor ? "Sill over the deck" : "Sill over the keel line", ask.Above, floor ? 0f : -2f, floor ? 6f : 12f, Step(ask.Above, 0.05f)));
            fields.Add(KwField.Choice(Frame, "Frame", FrameNames, Index(Frames, ask.Framed)));
            string[] covers = CoversOf(ask.Kind);
            fields.Add(KwField.Choice(Cover, "Cover", Array.ConvertAll(covers, c => CoverNames[Index(Covers, c)]), Index(covers, CoverFor(ask.Kind, ask.Covered))));
            return fields;
        }

        // A round port's diameter, a square's size, a rectangle's or an arched top's width and height.
        private static List<KwField> Sizes(KwOpeningAsk ask)
        {
            (float least, float most) = (KwOpeningShape.Least, KwOpeningShape.Most);
            if (ask.Kind == KwOpening.Round || ask.Kind == KwOpening.Square)
                return new List<KwField> { KwField.Number(Width, ask.Kind == KwOpening.Round ? "Diameter" : "Size", ask.Wide, least, most, Step(ask.Wide, 0.05f)) };
            return new List<KwField>
            {
                KwField.Number(Width, "Width", ask.Wide, least, most, Step(ask.Wide, 0.05f)),
                KwField.Number(Height, "Height", ask.Tall, least, most, Step(ask.Tall, 0.05f)),
            };
        }

        private static KwField FillRow(string label, KwOpeningAsk ask) =>
            KwField.Choice(Fill, label, new[] { "Keep it", "Fill in (wall again)" }, ask.Filled ? 1 : 0);

        /// <summary>A row's edit's Number row: how many the row has, one up to the most that fit.</summary>
        public static KwField NumberRow(int count, int most) => KwField.Number(Number, "Number", Mathf.Min(count, most), 1f, Mathf.Max(1, most), 1f, "");

        /// <summary>The covers a shape takes, in the window's order: a round port none or its lid; the others none, a lid up or down, or shutters.</summary>
        public static string[] CoversOf(string shape) => shape == KwOpening.Round
            ? new[] { KwOpening.None, KwOpening.Lid } : new[] { KwOpening.None, KwOpening.Up, KwOpening.Down, KwOpening.Shutters };

        /// <summary>A cover the shape takes for one asked: itself when taken; a round port's lid for any other cover; a lid hinged at its top for a round lid.</summary>
        public static string CoverFor(string shape, string cover) =>
            KwOpeningShape.Covers(shape, cover) ? cover : cover == KwOpening.None ? KwOpening.None : shape == KwOpening.Round ? KwOpening.Lid : KwOpening.Up;

        /// <summary>
        /// Which rows answers ask: filled in; a doorway with a door or open, and its frame (a thicker frame leaves less room
        /// under the wall's top, so the height row is built again with it); a round port, a square, or a shape with a height.
        /// </summary>
        public static string Rows(KwOpeningAsk ask) => ask.Filled ? "fill"
            : ask.Kind == KwOpening.Door ? (ask.Covered == KwOpening.None ? "open " : "door ") + ask.Framed
            : ask.Kind == KwOpening.Round ? "round" : ask.Kind == KwOpening.Square ? "square" : "tall";

        /// <summary>
        /// What builds a window's rows again (from the answers read and the window as it is) when an answer asks other rows:
        /// another shape, a door or none, filled in, or what more tells (a row's most for a wider type); true when it did (the
        /// window then reports its new values itself).
        /// </summary>
        public static Func<KwFormValues, bool> Reshaper(string title, KwOpeningAsk was, Func<KwOpeningAsk, KwFormValues, List<KwField>> rows, Func<KwOpeningAsk, string>? more = null)
        {
            string built = Rows(was) + more?.Invoke(was);
            return form =>
            {
                KwOpeningAsk now = Read(form, was);
                string need = Rows(now) + more?.Invoke(now);
                if (need == built)
                    return false;
                built = need;
                KwForm.Refill(title, rows(now, form));
                return true;
            };
        }

        /// <summary>The answers the window's rows hold now, from the answers it opened on (for what it has no row of).</summary>
        public static KwOpeningAsk Read(KwFormValues form, KwOpeningAsk was)
        {
            KwOpeningAsk ask = was.Copy();
            ask.Filled = form.ChoiceIndex(Fill) == 1;
            if (form.Has(Frame))
                ask.Framed = Frames[Mathf.Clamp(form.ChoiceIndex(Frame), 0, Frames.Length - 1)];
            if (form.Has(Door))
                Doorway(form, ask, was);
            else if (form.Has(Shape))
                Window(form, ask);
            return ask;
        }

        // A doorway's own rows: the door, the width with none, the height.
        private static void Doorway(KwFormValues form, KwOpeningAsk ask, KwOpeningAsk was)
        {
            ask.Covered = Doors[Mathf.Clamp(form.ChoiceIndex(Door), 0, Doors.Length - 1)];
            ask.Wide = KwOpeningTypes.DoorWidth(ask.Covered, form.Has(Width) ? form.Number(Width) : was.Wide);
            ask.Tall = form.Number(Height);
        }

        // A window's own rows: shape, sizes, sill, cover (one the shape takes). A round port or a square made a shape with a
        // height is half as high again as wide (as the window types are), so it changes from the square it was.
        private static void Window(KwFormValues form, KwOpeningAsk ask)
        {
            ask.Kind = Shapes[Mathf.Clamp(form.ChoiceIndex(Shape), 0, Shapes.Length - 1)];
            ask.Wide = form.Number(Width);
            if (form.Has(Height))
                ask.Tall = form.Number(Height);
            else if (ask.Kind == KwOpening.Rect || ask.Kind == KwOpening.Arch)
                ask.Tall = Mathf.Clamp(Mathf.Round(ask.Wide * 1.5f / 0.05f) * 0.05f, KwOpeningShape.Least, KwOpeningShape.Most);
            ask.Above = form.Number(Sill);
            ask.Covered = CoverFor(ask.Kind, Covers[Mathf.Max(0, Array.IndexOf(CoverNames, form.Choice(Cover)))]);
        }

        /// <summary>The row edit's count (none: not a row's edit).</summary>
        public static int? Count(KwFormValues form) => form.Has(Number) ? Mathf.RoundToInt(form.Number(Number)) : (int?)null;

        private static int Index(string[] names, string name) => Math.Max(0, Array.IndexOf(names, name));

        // A number's step: the window's own, or a centimetre for a value not on it (an opening made by a reshaped build).
        private static float Step(float value, float step) => Mathf.Abs(value / step - Mathf.Round(value / step)) < 0.001f ? step : 0.01f;
    }
}
