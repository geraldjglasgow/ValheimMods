using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The <see cref="KwForm"/> window's objects. A canvas of its own beside the game's screens (scaled by the game's own
    /// GuiScaler, sorted above the inventory and below the game's text box and pop-ups) is made the first time a form opens
    /// in a world, and again after the game's screens were rebuilt. On it each form gets a window built afresh, every part a
    /// copy of the game's own (<see cref="KwFormCopies"/>): the crafting panel's shadow and wood background, the title in
    /// the game's font with the braid line under it, a row per field (<see cref="KwFormRow"/>, a button row taking as many
    /// lines as its buttons fill), a note line under the rows when the form asks for one (<see cref="Say"/>: what the
    /// caller has to tell, such as a size it had to cut) and OK / Cancel. The window's centre sits at <see cref="Across"/> of the screen's width, so a
    /// ship and its preview in the middle stay in view; built again with more or fewer rows, its top stays put and it grows
    /// or shrinks downward (<see cref="Pin"/>). A ticker on the canvas runs the form each frame it shows
    /// (<see cref="KwFormTicker"/>).
    /// </summary>
    internal static class KwFormView
    {
        /// <summary>The height of one of the window's lines.</summary>
        public const float Spacing = 42f;

        private const int SortingOrder = 650;
        private const float Across = 0.72f;
        private const float Top = 76f;
        private const float Bottom = 76f;
        private const float NoteHeight = 44f;
        private const float ButtonWidth = 130f;
        private static readonly Color NoteColour = new Color(1f, 0.72f, 0.24f);

        private static readonly List<KwFormRow> rows = new List<KwFormRow>();
        private static GameObject? canvas;
        private static GameObject? window;
        private static KwFormRow? selected;
        private static TMP_Text? note;
        private static float? held;
        private const float Edge = 20f;

        /// <summary>Shows a new window for these fields; false while the game's screens are not there to copy from.</summary>
        public static bool Show(string title, IReadOnlyList<KwField> fields, KwFormValues values, bool noted)
        {
            InventoryGui gui = InventoryGui.instance;
            TMP_InputField? template = KwFormBoxes.Template();
            if (gui == null || gui.m_crafting == null || gui.m_craftButton == null || template == null)
                return false;
            Clear();
            GameObject on = canvas != null ? canvas : canvas = MakeCanvas(gui);
            on.SetActive(false);
            Build(gui, template, on.transform, title, fields, values, noted);
            on.SetActive(true);
            return true;
        }

        /// <summary>The pointer is over the window: its wheel goes to the window's boxes, not to the view.</summary>
        public static bool PointerOver() =>
            window != null && window.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)window.transform, ZInput.pointerPosition, null);

        public static void Hide()
        {
            Clear();
            if (canvas != null)
                canvas.SetActive(false);
        }

        /// <summary>The canvas is going with the game's screens: nothing of it is touched again.</summary>
        public static void Forget()
        {
            Release();
            window = null;
            canvas = null;
        }

        public static void FocusFirst() => rows.Find(row => row.IsNumber)?.Focus();

        /// <summary>The note line's words (empty: none).</summary>
        public static void Say(string text)
        {
            if (note != null && note.text != text)
                note.text = text;
        }

        /// <summary>The number box the player last clicked or tabbed into, where Tab goes on from when none has the keys.</summary>
        public static void Selected(KwFormRow row) => selected = row;

        /// <summary>Tab: the next number box (Shift + Tab the one before, round at the ends) takes the keys, its text selected.</summary>
        public static void Tab(int direction)
        {
            List<KwFormRow> numbers = rows.FindAll(row => row.IsNumber);
            if (numbers.Count == 0)
                return;
            int at = numbers.FindIndex(row => row.Focused);
            if (at < 0 && selected != null)
                at = numbers.IndexOf(selected);
            if (at >= 0)
                numbers[at].Commit();
            int next = at >= 0 ? (at + direction + numbers.Count) % numbers.Count : direction > 0 ? 0 : numbers.Count - 1;
            numbers[next].Focus();
        }

        // By index: a change heard by the caller may close the form, or open another, while the rows are walked.
        public static void CommitAll()
        {
            for (int i = 0; i < rows.Count; i++)
                rows[i].Commit();
        }

        public static void Tick()
        {
            for (int i = 0; i < rows.Count; i++)
                rows[i].Tick();
        }

        // The last form's rows change nothing any more; its window hides at once and is destroyed after the frame.
        private static void Clear()
        {
            Release();
            if (window != null)
            {
                window.SetActive(false);
                Object.Destroy(window);
            }
            window = null;
        }

        private static void Release()
        {
            foreach (KwFormRow row in rows)
                row.Live = false;
            rows.Clear();
            selected = null;
            note = null;
        }

        private static GameObject MakeCanvas(InventoryGui gui)
        {
            GameObject go = new GameObject("KW_Form", typeof(RectTransform));
            go.SetActive(false);
            go.layer = gui.gameObject.layer;
            go.transform.SetParent(gui.transform.parent, false);
            Canvas own = go.AddComponent<Canvas>();
            Canvas source = gui.GetComponent<Canvas>();
            own.renderMode = RenderMode.ScreenSpaceOverlay;
            own.sortingOrder = SortingOrder;
            if (source != null)
                own.additionalShaderChannels = source.additionalShaderChannels;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            CanvasScaler scale = gui.GetComponent<CanvasScaler>();
            if (scale != null)
                scaler.uiScaleMode = scale.uiScaleMode;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<GuiScaler>();
            go.AddComponent<KwFormTicker>();
            return go;
        }

        private static void Build(InventoryGui gui, TMP_InputField template, Transform on, string title, IReadOnlyList<KwField> fields, KwFormValues values, bool noted)
        {
            int lines = 0;
            foreach (KwField field in fields)
                lines += field.Lines;
            float height = Top + lines * Spacing + (noted ? NoteHeight : 0f) + Bottom;
            RectTransform frame = KwFormCopies.Rect("Window", on);
            Pin(frame, on, height);
            frame.sizeDelta = new Vector2(KwFormRow.Width, height);
            window = frame.gameObject;
            Backdrop(gui.m_crafting, frame);
            KwFormKit kit = new KwFormKit(frame, Heading(gui.m_crafting, frame, title), gui.m_craftButton, template);
            float y = Top;
            foreach (KwField field in fields)
            {
                rows.Add(KwFormRow.Make(kit, field, values, -y));
                y += field.Lines * Spacing;
            }
            note = noted ? Note(kit, y) : null;
            Footer(kit, height);
        }

        /// <summary>A new form opens: its window stands in the screen's middle again (<see cref="Pin"/>).</summary>
        public static void Fresh() => held = null;

        // The window's top: where it was while the same form is built again (a row more or less, as a sail's pattern
        // adds its own, grows or shrinks it downward and nothing above moves), else where the window stands in the
        // screen's middle; moved only as far as keeps it on the screen.
        private static void Pin(RectTransform frame, Transform on, float height)
        {
            frame.anchorMin = frame.anchorMax = new Vector2(Across, 0.5f);
            frame.pivot = new Vector2(0.5f, 1f);
            float half = on is RectTransform screen && screen.rect.height > 0f ? screen.rect.height / 2f : 540f;
            float top = Mathf.Min(Mathf.Max(held ?? height / 2f, height - half + Edge), half - Edge);
            held = top;
            frame.anchoredPosition = new Vector2(0f, top);
        }

        // The note line under the rows: small orange words, two lines at most, empty until the caller says something.
        private static TMP_Text Note(KwFormKit kit, float y)
        {
            TMP_Text text = KwFormCopies.Text(kit.Title, kit.Window, "Note", 15f, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.color = NoteColour;
            KwFormCopies.Place(text.rectTransform, KwFormRow.Margin, -y, KwFormRow.Width - 2f * KwFormRow.Margin, NoteHeight);
            return text;
        }

        /// <summary>The crafting panel's soft shadow and its wood background, stretched over the window.</summary>
        private static void Backdrop(RectTransform crafting, RectTransform frame)
        {
            Transform darken = crafting.Find("Darken");
            if (darken != null)
                KwFormCopies.Fill((RectTransform)KwFormCopies.Copy(darken, frame, "Darken").transform, -50f);
            Transform background = crafting.Find("Bkg");
            if (background != null)
                KwFormCopies.Fill((RectTransform)KwFormCopies.Copy(background, frame, "Bkg").transform, 0f);
        }

        private static TMP_Text Heading(RectTransform crafting, RectTransform frame, string title)
        {
            TMP_Text topic = crafting.Find("topic").GetComponent<TMP_Text>();
            TMP_Text text = KwFormCopies.Text(topic, frame, "Title", 26f, TextAlignmentOptions.Center);
            KwFormCopies.Place(text.rectTransform, 0f, -12f, KwFormRow.Width, 40f);
            text.text = title;
            Transform braid = crafting.Find("BraidLineHorisontalMedium");
            if (braid == null)
                return text;
            RectTransform line = (RectTransform)KwFormCopies.Copy(braid, frame, "Braid").transform;
            KwFormCopies.Place(line, (KwFormRow.Width - 320f) / 2f, -52f, 320f, 12f);
            return text;
        }

        /// <summary>OK and Cancel, side by side in the middle under the rows.</summary>
        private static void Footer(KwFormKit kit, float height)
        {
            float middle = KwFormRow.Width / 2f;
            float y = -(height - 62f);
            Button ok = KwFormCopies.Button(kit.Button, kit.Window, "OK", "OK", 17f, KwForm.Ok);
            KwFormCopies.Place((RectTransform)ok.transform, middle - ButtonWidth - 8f, y, ButtonWidth, 40f);
            Button cancel = KwFormCopies.Button(kit.Button, kit.Window, "Cancel", "Cancel", 17f, KwForm.Close);
            KwFormCopies.Place((RectTransform)cancel.transform, middle + 8f, y, ButtonWidth, 40f);
        }
    }

    /// <summary>What a row is built from: the window, its title (whose font the row's words copy), the game's button and input field.</summary>
    internal sealed class KwFormKit
    {
        public KwFormKit(RectTransform window, TMP_Text title, Button button, TMP_InputField field)
        {
            Window = window;
            Title = title;
            Button = button;
            Field = field;
        }

        public RectTransform Window { get; }
        public TMP_Text Title { get; }
        public Button Button { get; }
        public TMP_InputField Field { get; }
    }

    /// <summary>
    /// On the form's canvas: runs the form each frame the canvas shows, and when the canvas goes with the game's screens (a
    /// logout), an open form ends cancelled.
    /// </summary>
    internal sealed class KwFormTicker : MonoBehaviour
    {
        private static readonly Action Tick = KwForm.Tick;

        private void Update() => KwForm.Safe("Broadside form", Tick);

        private void OnDestroy() => KwForm.Safe("Broadside form", KwForm.Lost);
    }
}
