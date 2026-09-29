using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Makes room for the info pane in the skills window, once per window: the frame grows by the pane's width and
    /// <see cref="ExtraHeight"/>, the list (the dark box holding the entries) moves left by half of the added width and
    /// grows by the added height, so it shows more skills, and the pane (<see cref="PaneBuilder"/>) fills the space on
    /// its right, as tall as the list. The frame stays centred and keeps its margins; the title and the Close button
    /// are anchored to the frame's top and bottom and follow it. Measured from the game's layout (2026-09-28): frame
    /// 458 x 657, list 410 x 526 at 48 under the frame's top, its scrollbar 1 to 11 units right of it.
    /// </summary>
    internal static class BookLayout
    {
        public const float PaneWidth = 420f;
        public const float ExtraHeight = 140f;

        /// <summary>The space between the list and the pane; the list's scrollbar sits in it.</summary>
        public const float Gap = 24f;

        public const string PaneName = "GrindstoneSkills_skillinfo";

        /// <summary>The window's info pane, made the first time (widening the window); null when the window's parts are missing.</summary>
        public static BookPane PaneOf(SkillsDialog dialog)
        {
            RectTransform list = dialog.skillListScrollRect != null ? dialog.skillListScrollRect.transform.parent as RectTransform : null;
            RectTransform frame = list != null ? list.parent as RectTransform : null;
            if (frame == null)
                return null;
            Transform existing = frame.Find(PaneName);
            if (existing != null)
                return existing.GetComponent<BookPane>();
            Widen(frame, list);
            return PaneBuilder.Build(dialog, frame, list);
        }

        private static void Widen(RectTransform frame, RectTransform list)
        {
            float added = PaneWidth + Gap;
            frame.sizeDelta += new Vector2(added, ExtraHeight);
            list.anchoredPosition -= new Vector2(added / 2f, 0f);
            list.sizeDelta += new Vector2(0f, ExtraHeight);
        }
    }
}
