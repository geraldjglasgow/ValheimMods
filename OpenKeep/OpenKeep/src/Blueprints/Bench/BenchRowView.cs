using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// One line of a bench list: a copy of the game's recipe row (its hover, highlight and click sound) with the line's
    /// icon and name. A click picks (Ctrl / Shift as in a file explorer), a double click opens a folder, a right click
    /// renames, a drag carries the picks (<see cref="BenchDrag"/>), and a folder line takes drops, framed while one hovers it.
    /// </summary>
    public sealed class BenchRowView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color DropColour = new Color(0.35f, 0.75f, 0.3f, 0.85f);

        private BenchPaneView pane;
        private int index;
        private Image icon;
        private TMP_Text label;
        private GameObject marker;
        private Image markerImage;
        private Color markerColour;
        private bool picked;

        public BenchEntry Entry { get; private set; }

        public static BenchRowView Make(GameObject template, RectTransform content, BenchPaneView pane)
        {
            GameObject go = BenchCopies.Copy(template.transform, content, "Row");
            foreach (string unused in new[] { "Durability", "QualityLevel" })
                go.transform.Find(unused)?.gameObject.SetActive(false);
            Button button = go.GetComponent<Button>();
            if (button != null)
                button.onClick = new Button.ButtonClickedEvent();
            BenchRowView row = go.AddComponent<BenchRowView>();
            row.pane = pane;
            row.icon = go.transform.Find("icon")?.GetComponent<Image>();
            row.label = go.transform.Find("name")?.GetComponent<TMP_Text>();
            row.marker = go.transform.Find("selected")?.gameObject;
            row.markerImage = row.marker != null ? row.marker.GetComponent<Image>() : null;
            row.markerColour = row.markerImage != null ? row.markerImage.color : Color.white;
            row.Lay((RectTransform)go.transform);
            return row;
        }

        private void Lay(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, BenchPaneView.RowHeight);
            if (icon != null)
            {
                BenchCopies.Place(icon.rectTransform, 6f, -3f, BenchPaneView.RowHeight - 6f, BenchPaneView.RowHeight - 6f);
                icon.preserveAspect = true;
            }
            if (label == null)
                return;
            BenchCopies.Fill(label.rectTransform, 0f);
            label.rectTransform.offsetMin = new Vector2(BenchPaneView.RowHeight + 10f, 0f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);
            label.enableAutoSizing = false;
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.richText = true;
        }

        public void Show(int at, BenchEntry entry, bool isPicked)
        {
            index = at;
            Entry = entry;
            gameObject.SetActive(true);
            ((RectTransform)transform).anchoredPosition = new Vector2(0f, -at * BenchPaneView.RowHeight);
            if (icon != null)
                icon.sprite = BenchPaneView.IconOf(entry);
            if (label != null)
                label.text = entry.Kind == BenchKind.Folder ? "<b>" + entry.Label + "</b>" : entry.Label;
            Mark(isPicked);
        }

        public void Hide()
        {
            Entry = null;
            gameObject.SetActive(false);
        }

        public void Mark(bool isPicked)
        {
            picked = isPicked;
            DropMark(false);
        }

        /// <summary>The row's highlight: green while a drag could drop here, the game's own while picked.</summary>
        public void DropMark(bool on)
        {
            if (marker != null)
                marker.SetActive(on || picked);
            if (markerImage != null)
                markerImage.color = on ? DropColour : markerColour;
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (Entry == null)
                return;
            if (data.button == PointerEventData.InputButton.Right)
                pane.RightClick(index);
            else if (data.button == PointerEventData.InputButton.Left && data.clickCount >= 2)
                pane.Activate(index);
            else if (data.button == PointerEventData.InputButton.Left)
                pane.Click(index);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (Entry != null && data.button == PointerEventData.InputButton.Left && Entry.Kind != BenchKind.Up)
                pane.BeginDrag(index, data);
        }

        public void OnDrag(PointerEventData data) => BenchDrag.Move(data);

        public void OnEndDrag(PointerEventData data) => BenchDrag.End();

        public void OnDrop(PointerEventData data) => BenchDrag.DropOn(pane, Entry);

        public void OnPointerEnter(PointerEventData data) => BenchDrag.Hover(pane, this);

        public void OnPointerExit(PointerEventData data) => BenchDrag.Leave(this);
    }

    /// <summary>The list's empty space: a click there lets the picks go, a drop there lands in the folder shown.</summary>
    public sealed class BenchListDrop : MonoBehaviour, IPointerClickHandler, IDropHandler
    {
        public BenchPaneView Pane;

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
                Pane?.ClearPicks();
        }

        public void OnDrop(PointerEventData data) => BenchDrag.DropOn(Pane, null);
    }
}
