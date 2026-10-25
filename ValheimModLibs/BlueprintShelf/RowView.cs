using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueprintShelf;

/// <summary>
/// One line of a list: a copy of the game's recipe row (its hover, highlight and click sound) with the line's icon and
/// name. A click picks (Ctrl / Shift as in a file explorer), a double click opens a folder, a right click renames, a drag
/// carries the picks (<see cref="ShelfDrag"/>), and a folder line takes drops, framed while one hovers it.
/// </summary>
internal sealed class RowView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
	IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
	private static readonly Color DropColour = new Color(0.35f, 0.75f, 0.3f, 0.85f);

	private PaneView pane = null!;
	private int index;
	private Image? icon;
	private TMP_Text? label;
	private GameObject? marker;
	private Image? markerImage;
	private Color markerColour;
	private bool picked;

	public ShelfLine? Entry { get; private set; }

	private ShelfDrag Drag => pane.Ui.Drag;

	public static RowView Make(GameObject template, RectTransform content, PaneView pane)
	{
		GameObject go = UiCopies.Copy(template.transform, content, "Row");
		foreach (string unused in new[] { "Durability", "QualityLevel" })
		{
			go.transform.Find(unused)?.gameObject.SetActive(false);
		}
		Button button = go.GetComponent<Button>();
		if (button != null)
		{
			button.onClick = new Button.ButtonClickedEvent();
		}
		RowView row = go.AddComponent<RowView>();
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
		rect.sizeDelta = new Vector2(0f, PaneView.RowHeight);
		if (icon != null)
		{
			UiCopies.Place(icon.rectTransform, 6f, -3f, PaneView.RowHeight - 6f, PaneView.RowHeight - 6f);
			icon.preserveAspect = true;
		}
		if (label != null)
		{
			LayLabel(label);
		}
	}

	private static void LayLabel(TMP_Text label)
	{
		UiCopies.Fill(label.rectTransform, 0f);
		label.rectTransform.offsetMin = new Vector2(PaneView.RowHeight + 10f, 0f);
		label.rectTransform.offsetMax = new Vector2(-8f, 0f);
		label.enableAutoSizing = false;
		label.fontSize = 18f;
		label.alignment = TextAlignmentOptions.MidlineLeft;
		label.textWrappingMode = TextWrappingModes.NoWrap;
		label.overflowMode = TextOverflowModes.Ellipsis;
		label.richText = true;
	}

	public void Show(int at, ShelfLine entry, bool isPicked)
	{
		index = at;
		Entry = entry;
		gameObject.SetActive(true);
		((RectTransform)transform).anchoredPosition = new Vector2(0f, -at * PaneView.RowHeight);
		if (icon != null)
		{
			icon.sprite = pane.Ui.Shelf.Icon(entry.Kind);
		}
		if (label != null)
		{
			label.text = entry.Kind == ShelfKind.Folder ? "<b>" + entry.Label + "</b>" : entry.Label;
		}
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
		{
			marker.SetActive(on || picked);
		}
		if (markerImage != null)
		{
			markerImage.color = on ? DropColour : markerColour;
		}
	}

	public void OnPointerClick(PointerEventData data)
	{
		if (Entry == null)
		{
			return;
		}
		if (data.button == PointerEventData.InputButton.Right)
		{
			pane.RightClick(index);
		}
		else if (data.button == PointerEventData.InputButton.Left && data.clickCount >= 2)
		{
			pane.Activate(index);
		}
		else if (data.button == PointerEventData.InputButton.Left)
		{
			pane.Click(index);
		}
	}

	public void OnBeginDrag(PointerEventData data)
	{
		if (Entry != null && data.button == PointerEventData.InputButton.Left && Entry.Kind != ShelfKind.Up)
		{
			pane.BeginDrag(index, data);
		}
	}

	public void OnDrag(PointerEventData data) => Drag.Move(data);

	public void OnEndDrag(PointerEventData data) => Drag.End();

	public void OnDrop(PointerEventData data) => Drag.DropOn(pane, Entry);

	public void OnPointerEnter(PointerEventData data) => Drag.Hover(pane, this);

	public void OnPointerExit(PointerEventData data) => Drag.Leave(this);
}

/// <summary>The list's empty space: a click there lets the picks go, a drop there lands in the folder shown.</summary>
internal sealed class ListDrop : MonoBehaviour, IPointerClickHandler, IDropHandler
{
	public PaneView? Pane;

	public void OnPointerClick(PointerEventData data)
	{
		if (data.button == PointerEventData.InputButton.Left)
		{
			Pane?.ClearPicks();
		}
	}

	public void OnDrop(PointerEventData data)
	{
		if (Pane != null)
		{
			Pane.Ui.Drag.DropOn(Pane, null);
		}
	}
}
