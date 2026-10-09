using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// One side of the bench window, drawn from its model (<see cref="IBenchPane"/>): a header, the folder trail, a copy of
    /// the crafting panel's recipe list (its frame, clipping, scroll bar and wheel) holding one row per line, and a row of
    /// copies of the game's craft button. Rows are made as needed and reused; the list is filled again only when the model
    /// changed, repainted only when the picks changed.
    /// </summary>
    public sealed class BenchPaneView
    {
        public const float RowHeight = 34f;
        private const float HeaderHeight = 36f;
        private const float TrailHeight = 26f;
        private const float ButtonHeight = 44f;
        private const float BarWidth = 10f;
        private const float Gap = 6f;

        private readonly List<BenchRowView> rows = new List<BenchRowView>();
        private readonly List<(Button Button, BenchAction Action)> buttons = new List<(Button, BenchAction)>();
        private TMP_Text header;
        private TMP_Text trail;
        private RectTransform content;
        private ScrollRect scroll;
        private GameObject template;
        private int shownVersion = -1;
        private int shownPicks = -1;

        private BenchPaneView(IBenchPane model) => Model = model;

        public IBenchPane Model { get; }

        public static BenchPaneView Build(InventoryGui gui, RectTransform window, IBenchPane model, Rect area)
        {
            BenchPaneView view = new BenchPaneView(model);
            RectTransform root = BenchCopies.Rect(model is PoolPane ? "Pool" : "Library", window);
            BenchCopies.Place(root, area.x, area.y, area.width, area.height);
            view.header = BenchCopies.Text(BenchUi.Title, root, "Header", 24f, TextAlignmentOptions.Center);
            BenchCopies.Place(view.header.rectTransform, 0f, 0f, area.width, HeaderHeight);
            view.trail = BenchCopies.Text(BenchUi.Title, root, "Trail", 17f, TextAlignmentOptions.MidlineLeft);
            view.trail.color = new Color(0.86f, 0.8f, 0.68f);
            BenchCopies.Place(view.trail.rectTransform, 6f, -HeaderHeight, area.width - 12f, TrailHeight);
            float listTop = HeaderHeight + TrailHeight + Gap;
            view.MakeList(gui, root, listTop, area.width, area.height - listTop - ButtonHeight - Gap);
            view.MakeButtons(gui, root, area.width, area.height);
            return view;
        }

        private void MakeList(InventoryGui gui, RectTransform root, float top, float width, float height)
        {
            ScrollRect source = gui.m_recipeListRoot.GetComponentInParent<ScrollRect>(true);
            GameObject list = BenchCopies.Copy(source, root, "List");
            scroll = list.GetComponent<ScrollRect>();
            content = scroll.content;
            for (int i = list.transform.childCount - 1; i >= 0; i--)
            {
                if (list.transform.GetChild(i) != content)
                    Object.DestroyImmediate(list.transform.GetChild(i).gameObject);
            }
            for (int i = content.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(content.GetChild(i).gameObject);
            BenchCopies.Place((RectTransform)list.transform, 0f, -top, width - BarWidth - 4f, height);
            BenchCopies.Place(content, 0f, 0f, 0f, 0f);
            content.anchorMax = new Vector2(1f, 1f);
            scroll.verticalScrollbar = Bar(gui, root, width, top, height);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.horizontal = false;
            list.AddComponent<BenchListDrop>().Pane = this;
            template = gui.m_recipeElementPrefab;
        }

        private static Scrollbar Bar(InventoryGui gui, RectTransform root, float width, float top, float height)
        {
            if (gui.m_recipeListScroll == null)
                return null;
            Scrollbar bar = BenchCopies.Copy(gui.m_recipeListScroll, root, "Bar").GetComponent<Scrollbar>();
            BenchCopies.Place((RectTransform)bar.transform, width - BarWidth, -top, BarWidth, height);
            bar.onValueChanged = new Scrollbar.ScrollEvent();
            return bar;
        }

        private void MakeButtons(InventoryGui gui, RectTransform root, float width, float height)
        {
            int count = Model.Actions.Count;
            float each = (width - Gap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                BenchAction action = Model.Actions[i];
                Button button = BenchCopies.Button(gui.m_craftButton, root, "Button" + i, action.Word, action.Run);
                BenchCopies.Place((RectTransform)button.transform, i * (each + Gap), -(height - ButtonHeight), each, ButtonHeight);
                buttons.Add((button, action));
            }
        }

        /// <summary>Per frame while the window shows.</summary>
        public void Tick()
        {
            Model.Tick();
            if (Model.Version != shownVersion)
                Fill();
            else if (Model.Selection.Version != shownPicks)
                Paint();
            foreach ((Button button, BenchAction action) in buttons)
            {
                bool on = action.Enabled();
                if (button.interactable != on)
                    button.interactable = on;
            }
        }

        private void Fill()
        {
            List<BenchEntry> entries = Model.Entries;
            while (rows.Count < entries.Count)
                rows.Add(BenchRowView.Make(template, content, this));
            for (int i = 0; i < rows.Count; i++)
            {
                if (i < entries.Count)
                    rows[i].Show(i, entries[i], Model.Selection.Has(entries[i]));
                else
                    rows[i].Hide();
            }
            content.sizeDelta = new Vector2(0f, entries.Count * RowHeight);
            header.text = Model.Header;
            trail.text = Model.Location;
            shownVersion = Model.Version;
            shownPicks = Model.Selection.Version;
        }

        private void Paint()
        {
            foreach (BenchRowView row in rows.Where(r => r.Entry != null))
                row.Mark(Model.Selection.Has(row.Entry));
            shownPicks = Model.Selection.Version;
        }

        public void Click(int index)
        {
            BenchWindow.Focus(this);
            Model.Selection.Click(Model.Entries, index, Held(KeyCode.LeftControl, KeyCode.RightControl), Held(KeyCode.LeftShift, KeyCode.RightShift));
        }

        public void Activate(int index)
        {
            BenchWindow.Focus(this);
            Model.Open(Model.Entries[index]);
        }

        public void RightClick(int index)
        {
            BenchWindow.Focus(this);
            BenchEntry entry = Model.Entries[index];
            if (!Model.Selection.Has(entry))
                Model.Selection.Only(entry);
            Model.Rename(entry);
        }

        /// <summary>F2: renames the one picked line.</summary>
        public void RenamePicked()
        {
            List<BenchEntry> picked = Model.Selection.Of(Model.Entries);
            if (picked.Count == 1)
                Model.Rename(picked[0]);
        }

        public void ClearPicks()
        {
            BenchWindow.Focus(this);
            Model.Selection.Clear();
        }

        public void BeginDrag(int index, PointerEventData data)
        {
            BenchWindow.Focus(this);
            BenchEntry entry = Model.Entries[index];
            if (!Model.Selection.Has(entry))
                Model.Selection.Only(entry);
            BenchDrag.Begin(this, Model.Selection.Of(Model.Entries), IconOf(entry), data);
        }

        private static bool Held(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);

        public static Sprite IconOf(BenchEntry entry) => BlueprintIcons.Get(entry.Kind == BenchKind.Up ? BlueprintIcons.FolderUp
            : entry.Kind == BenchKind.Blueprint ? BlueprintIcons.Blueprint : BlueprintIcons.Folder);

        /// <summary>"Blueprints / Houses / Nordic": the top's name, then each folder made readable.</summary>
        public static string Trail(string top, string folder) => string.IsNullOrEmpty(folder) ? top
            : top + " / " + string.Join(" / ", folder.Split('/').Select(BlueprintEntries.Title));
    }
}
