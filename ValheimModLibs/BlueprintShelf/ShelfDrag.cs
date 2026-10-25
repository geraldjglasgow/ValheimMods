using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueprintShelf;

/// <summary>
/// Dragging lines in the window: a press that moves past the event system's drag threshold carries the picks of one list
/// (the line pressed when it was not picked), a see-through icon with their count follows the mouse, a folder line that
/// would take them is framed green, and letting go on a folder line or a list's empty space hands them to that list
/// (<see cref="IShelfPane.Drop"/>): a move within one side, a share or a copy across. Anywhere else nothing happens. Only
/// this machine; the window's lists do the rest.
/// </summary>
internal sealed class ShelfDrag
{
	private const float GhostSize = 40f;

	private readonly ShelfUi ui;
	private PaneView? from;
	private List<ShelfLine>? dragged;
	private RectTransform? ghost;
	private RowView? marked;

	public ShelfDrag(ShelfUi ui) => this.ui = ui;

	public bool Active => from != null;

	public void Begin(PaneView pane, List<ShelfLine> entries, Sprite? icon, PointerEventData data)
	{
		End();
		if (entries.Count == 0 || ui.Window == null)
		{
			return;
		}
		from = pane;
		dragged = entries;
		ghost = Ghost(icon, entries.Count);
		Move(data);
	}

	private RectTransform Ghost(Sprite? icon, int count)
	{
		RectTransform root = UiCopies.Rect(ui.Shelf.Settings.CanvasName + "_Drag", ui.Window);
		root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
		root.pivot = new Vector2(0f, 1f);
		root.sizeDelta = new Vector2(GhostSize, GhostSize);
		CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
		group.blocksRaycasts = false;
		group.alpha = 0.75f;
		Image image = root.gameObject.AddComponent<Image>();
		image.sprite = icon;
		image.preserveAspect = true;
		image.raycastTarget = false;
		TMP_Text number = UiCopies.Text(ui.Title, root, "Count", 18f, TextAlignmentOptions.BottomRight);
		UiCopies.Fill(number.rectTransform, -6f);
		number.text = count > 1 ? count.ToString() : "";
		root.SetAsLastSibling();
		return root;
	}

	public void Move(PointerEventData data)
	{
		if (ghost != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.Window, data.position, null, out Vector2 local))
		{
			ghost.anchoredPosition = local + new Vector2(14f, -14f);
		}
	}

	public void Hover(PaneView pane, RowView row)
	{
		Unmark();
		if (from == null || row.Entry == null || !pane.Model.Accepts(from.Model, row.Entry))
		{
			return;
		}
		marked = row;
		row.DropMark(true);
	}

	public void Leave(RowView row)
	{
		if (marked == row)
		{
			Unmark();
		}
	}

	private void Unmark()
	{
		if (marked != null)
		{
			marked.DropMark(false);
		}
		marked = null;
	}

	/// <summary>Letting go over a list's line (<paramref name="onto"/>) or its empty space (null).</summary>
	public void DropOn(PaneView pane, ShelfLine? onto)
	{
		if (from == null || dragged == null)
		{
			return;
		}
		IShelfPane source = from.Model;
		List<ShelfLine> entries = dragged;
		End();
		if (pane.Model.Accepts(source, onto))
		{
			ui.Shelf.Run("drop", () => pane.Model.Drop(source, entries, onto));
		}
		else if (source is OwnPane && pane.Model is PoolPane)
		{
			ui.Shelf.Say(ui.Shelf.Words.Get(ShelfWord.OnlyOwn));
		}
	}

	public void End()
	{
		Unmark();
		if (ghost != null)
		{
			Object.Destroy(ghost.gameObject);
		}
		ghost = null;
		from = null;
		dragged = null;
	}
}
