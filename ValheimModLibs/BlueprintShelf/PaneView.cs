using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueprintShelf;

/// <summary>
/// One side of the window, drawn from its model (<see cref="IShelfPane"/>): a header, the folder trail, a copy of the
/// crafting panel's recipe list (its frame, clipping, scroll bar and wheel) holding one row per line, and a row of copies
/// of the game's craft button. Rows are made as needed and reused; the list is filled again only when the model changed,
/// repainted only when the picks changed.
/// </summary>
internal sealed class PaneView
{
	public const float RowHeight = 34f;
	public const float ButtonHeight = 44f;
	private const float HeaderHeight = 36f;
	private const float TrailHeight = 26f;
	private const float BarWidth = 10f;
	private const float Gap = 6f;

	private readonly List<RowView> rows = new();
	private readonly ButtonRow buttons = new();
	private TMP_Text header = null!;
	private TMP_Text trail = null!;
	private RectTransform content = null!;
	private GameObject template = null!;
	private int shownVersion = -1;
	private int shownPicks = -1;

	private PaneView(ShelfUi ui, IShelfPane model)
	{
		Ui = ui;
		Model = model;
	}

	public ShelfUi Ui { get; }

	public IShelfPane Model { get; }

	private Shelf Shelf => Ui.Shelf;

	public static PaneView Build(ShelfUi ui, InventoryGui gui, IShelfPane model, Rect area)
	{
		PaneView view = new PaneView(ui, model);
		RectTransform root = UiCopies.Rect(model is PoolPane ? "Pool" : "Library", ui.Window);
		UiCopies.Place(root, area.x, area.y, area.width, area.height);
		view.header = UiCopies.Text(ui.Title, root, "Header", 24f, TextAlignmentOptions.Center);
		UiCopies.Place(view.header.rectTransform, 0f, 0f, area.width, HeaderHeight);
		view.trail = UiCopies.Text(ui.Title, root, "Trail", 17f, TextAlignmentOptions.MidlineLeft);
		view.trail.color = ShelfUi.Pale;
		UiCopies.Place(view.trail.rectTransform, 6f, -HeaderHeight, area.width - 12f, TrailHeight);
		float listTop = HeaderHeight + TrailHeight + Gap;
		view.MakeList(gui, root, listTop, area.width, area.height - listTop - ButtonHeight - Gap);
		view.MakeButtons(gui, root, area.width, area.height);
		return view;
	}

	private void MakeList(InventoryGui gui, RectTransform root, float top, float width, float height)
	{
		ScrollRect source = gui.m_recipeListRoot.GetComponentInParent<ScrollRect>(true);
		GameObject list = UiCopies.Copy(source, root, "List");
		ScrollRect scroll = list.GetComponent<ScrollRect>();
		content = scroll.content;
		Empty(list.transform, content);
		UiCopies.Place((RectTransform)list.transform, 0f, -top, width - BarWidth - 4f, height);
		UiCopies.Place(content, 0f, 0f, 0f, 0f);
		content.anchorMax = new Vector2(1f, 1f);
		scroll.verticalScrollbar = Bar(gui, root, width, top, height);
		scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
		scroll.horizontal = false;
		list.AddComponent<ListDrop>().Pane = this;
		template = gui.m_recipeElementPrefab;
	}

	/// <summary>Only the list's content stays, emptied of the game's recipe rows.</summary>
	private static void Empty(Transform list, RectTransform content)
	{
		for (int i = list.childCount - 1; i >= 0; i--)
		{
			if (list.GetChild(i) != content)
			{
				Object.DestroyImmediate(list.GetChild(i).gameObject);
			}
		}
		for (int i = content.childCount - 1; i >= 0; i--)
		{
			Object.DestroyImmediate(content.GetChild(i).gameObject);
		}
	}

	private static Scrollbar? Bar(InventoryGui gui, RectTransform root, float width, float top, float height)
	{
		if (gui.m_recipeListScroll == null)
		{
			return null;
		}
		Scrollbar bar = UiCopies.Copy(gui.m_recipeListScroll, root, "Bar").GetComponent<Scrollbar>();
		UiCopies.Place((RectTransform)bar.transform, width - BarWidth, -top, BarWidth, height);
		bar.onValueChanged = new Scrollbar.ScrollEvent();
		return bar;
	}

	private void MakeButtons(InventoryGui gui, RectTransform root, float width, float height)
	{
		int count = Model.Actions.Count;
		float each = (width - Gap * (count - 1)) / count;
		for (int i = 0; i < count; i++)
		{
			ShelfButton action = Model.Actions[i];
			Button button = UiCopies.Button(Shelf, gui.m_craftButton, root, "Button" + i, Shelf.Words.Get(action.Word), action.Run);
			UiCopies.Place((RectTransform)button.transform, i * (each + Gap), -(height - ButtonHeight), each, ButtonHeight);
			buttons.Add(button, action);
		}
	}

	/// <summary>Per frame while the window shows.</summary>
	public void Tick()
	{
		Model.Tick();
		if (Model.Version != shownVersion)
		{
			Fill();
		}
		else if (Model.Selection.Version != shownPicks)
		{
			Paint();
		}
		buttons.Tick();
	}

	private void Fill()
	{
		List<ShelfLine> entries = Model.Entries;
		while (rows.Count < entries.Count)
		{
			rows.Add(RowView.Make(template, content, this));
		}
		for (int i = 0; i < rows.Count; i++)
		{
			if (i < entries.Count)
			{
				rows[i].Show(i, entries[i], Model.Selection.Has(entries[i]));
			}
			else
			{
				rows[i].Hide();
			}
		}
		content.sizeDelta = new Vector2(0f, entries.Count * RowHeight);
		header.text = Model.Header;
		trail.text = Model.Location;
		shownVersion = Model.Version;
		shownPicks = Model.Selection.Version;
	}

	private void Paint()
	{
		foreach (RowView row in rows.Where(r => r.Entry != null))
		{
			row.Mark(Model.Selection.Has(row.Entry!));
		}
		shownPicks = Model.Selection.Version;
	}

	public void Click(int index)
	{
		Ui.Focus(this);
		Model.Selection.Click(Model.Entries, index, Held(KeyCode.LeftControl, KeyCode.RightControl), Held(KeyCode.LeftShift, KeyCode.RightShift));
	}

	public void Activate(int index)
	{
		Ui.Focus(this);
		Model.Open(Model.Entries[index]);
	}

	public void RightClick(int index)
	{
		Ui.Focus(this);
		ShelfLine entry = Model.Entries[index];
		if (!Model.Selection.Has(entry))
		{
			Model.Selection.Only(entry);
		}
		Model.Rename(entry);
	}

	/// <summary>The rename key: renames the one picked line.</summary>
	public void RenamePicked()
	{
		List<ShelfLine> picked = Model.Selection.Of(Model.Entries);
		if (picked.Count == 1)
		{
			Model.Rename(picked[0]);
		}
	}

	public void ClearPicks()
	{
		Ui.Focus(this);
		Model.Selection.Clear();
	}

	public void BeginDrag(int index, PointerEventData data)
	{
		Ui.Focus(this);
		ShelfLine entry = Model.Entries[index];
		if (!Model.Selection.Has(entry))
		{
			Model.Selection.Only(entry);
		}
		Ui.Drag.Begin(this, Model.Selection.Of(Model.Entries), Shelf.Icon(entry.Kind), data);
	}

	private static bool Held(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);

	/// <summary>"Blueprints / Houses / Nordic": the top's name, then each folder made readable.</summary>
	public static string Trail(Shelf shelf, string top, string folder) => string.IsNullOrEmpty(folder) ? top
		: top + " / " + string.Join(" / ", folder.Split('/').Select(shelf.Settings.Title));
}
