using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueprintShelf;

/// <summary>
/// The window's objects, made the first time it opens in a world (again after the game's GUI was rebuilt): a canvas of its
/// own beside the game's screens (scaled by the game's own GuiScaler, sorted above the inventory and below the game's text
/// box and pop-ups) holding the window - the crafting panel's shadow and wood background, its title in the game's font
/// with the braid line under it, the two lists side by side (<see cref="PaneView"/>), the host's buttons in a row under
/// them when it has any, a status line and Close. Every part is a copy of the game's own (<see cref="UiCopies"/>), so the
/// window is the game's wood panel, or PackPanel's timber when PackPanel skins wood panels.
/// </summary>
internal sealed class ShelfUi
{
	public static readonly Color Pale = new Color(0.86f, 0.8f, 0.68f);

	private const float Width = 1240f;
	private const float BaseHeight = 720f;
	private const int SortingOrder = 650;
	private const float Side = 34f;
	private const float Top = 84f;
	private const float Bottom = 70f;
	private const float Between = 24f;
	private const float HostRow = 54f;
	private const float HostButtonWidth = 300f;

	private readonly ButtonRow hostButtons = new();
	private GameObject? canvas;

	public ShelfUi(Shelf shelf)
	{
		Shelf = shelf;
		Drag = new ShelfDrag(this);
	}

	public Shelf Shelf { get; }
	public ShelfDrag Drag { get; }
	public RectTransform Window { get; private set; } = null!;
	public TMP_Text Title { get; private set; } = null!;
	public TMP_Text Status { get; private set; } = null!;
	public PaneView? Left { get; private set; }
	public PaneView? Right { get; private set; }

	private List<ShelfButton> Buttons => Shelf.Settings.Buttons;
	private float Height => BaseHeight + (Buttons.Count > 0 ? HostRow : 0f);

	public bool Shown => canvas != null && canvas.activeSelf;

	public void Show(bool on)
	{
		if (canvas != null && canvas.activeSelf != on)
		{
			canvas.SetActive(on);
		}
	}

	public void Focus(PaneView pane) => Shelf.Window.Focus(pane);

	/// <summary>Per frame while shown: the lists, and the host's buttons' pressable state.</summary>
	public void Tick()
	{
		Left?.Tick();
		Right?.Tick();
		hostButtons.Tick();
	}

	/// <summary>Makes the window when it is missing; false while the game's inventory screen is not there to copy from.</summary>
	public bool Ensure(IShelfPane own, IShelfPane pool)
	{
		if (canvas != null)
		{
			return true;
		}
		InventoryGui gui = InventoryGui.instance;
		if (gui == null || gui.m_crafting == null || gui.m_recipeElementPrefab == null || gui.m_craftButton == null)
		{
			return false;
		}
		canvas = MakeCanvas(gui);
		Shelf.Run("window", () => Build(gui, own, pool));
		if (Left == null || Right == null)
		{
			Object.Destroy(canvas);
			canvas = null;
		}
		return canvas != null;
	}

	private GameObject MakeCanvas(InventoryGui gui)
	{
		GameObject go = new GameObject(Shelf.Settings.CanvasName, typeof(RectTransform));
		go.SetActive(false);
		go.layer = gui.gameObject.layer;
		go.transform.SetParent(gui.transform.parent, false);
		Canvas own = go.AddComponent<Canvas>();
		Canvas source = gui.GetComponent<Canvas>();
		own.renderMode = RenderMode.ScreenSpaceOverlay;
		own.sortingOrder = SortingOrder;
		if (source != null)
		{
			own.additionalShaderChannels = source.additionalShaderChannels;
		}
		CanvasScaler scaler = go.AddComponent<CanvasScaler>();
		CanvasScaler scale = gui.GetComponent<CanvasScaler>();
		if (scale != null)
		{
			scaler.uiScaleMode = scale.uiScaleMode;
		}
		go.AddComponent<GraphicRaycaster>();
		go.AddComponent<GuiScaler>();
		return go;
	}

	private void Build(InventoryGui gui, IShelfPane own, IShelfPane pool)
	{
		Left = Right = null;
		Window = UiCopies.Rect("Window", canvas!.transform);
		Window.anchorMin = Window.anchorMax = Window.pivot = new Vector2(0.5f, 0.5f);
		Window.sizeDelta = new Vector2(Width, Height);
		Backdrop(gui.m_crafting);
		Heading(gui.m_crafting);
		float paneWidth = (Width - 2f * Side - Between) / 2f;
		Rect area = new Rect(Side, -Top, paneWidth, Height - Top - Bottom - (Buttons.Count > 0 ? HostRow : 0f));
		Left = PaneView.Build(this, gui, own, area);
		area.x += paneWidth + Between;
		Right = PaneView.Build(this, gui, pool, area);
		HostButtons(gui);
		Footer(gui);
	}

	/// <summary>The crafting panel's soft shadow and its wood background, stretched over the window.</summary>
	private void Backdrop(RectTransform crafting)
	{
		Transform darken = crafting.Find("Darken");
		if (darken != null)
		{
			UiCopies.Fill((RectTransform)UiCopies.Copy(darken, Window, "Darken").transform, -50f);
		}
		Transform background = crafting.Find("Bkg");
		if (background != null)
		{
			UiCopies.Fill((RectTransform)UiCopies.Copy(background, Window, "Bkg").transform, 0f);
		}
	}

	private void Heading(RectTransform crafting)
	{
		Title = UiCopies.Text(crafting.Find("topic").GetComponent<TMP_Text>(), Window, "Title", 32f, TextAlignmentOptions.Center);
		UiCopies.Place(Title.rectTransform, 0f, -14f, Width, 46f);
		Title.text = Shelf.Words.Get(ShelfWord.Title);
		Transform braid = crafting.Find("BraidLineHorisontalMedium");
		if (braid == null)
		{
			return;
		}
		RectTransform line = (RectTransform)UiCopies.Copy(braid, Window, "Braid").transform;
		UiCopies.Place(line, (Width - 520f) / 2f, -58f, 520f, 14f);
	}

	/// <summary>The host's buttons, centred in a row between the lists and the footer.</summary>
	private void HostButtons(InventoryGui gui)
	{
		int count = Buttons.Count;
		float gap = 12f;
		float each = Mathf.Min(HostButtonWidth, (Width - 2f * Side - gap * (count - 1)) / Mathf.Max(1, count));
		float x = (Width - count * each - (count - 1) * gap) / 2f;
		for (int i = 0; i < count; i++)
		{
			ShelfButton action = Buttons[i];
			Button button = UiCopies.Button(Shelf, gui.m_craftButton, Window, "Host" + i, Shelf.Words.Text(action.Word), action.Run);
			UiCopies.Place((RectTransform)button.transform, x + i * (each + gap), -(Height - Bottom - HostRow + 8f), each, PaneView.ButtonHeight);
			hostButtons.Add(button, action);
		}
	}

	private void Footer(InventoryGui gui)
	{
		Status = UiCopies.Text(Title, Window, "Status", 16f, TextAlignmentOptions.MidlineLeft);
		Status.color = Pale;
		UiCopies.Place(Status.rectTransform, Side, -(Height - Bottom + 14f), Width - 2f * Side - 160f, 40f);
		Button close = UiCopies.Button(Shelf, gui.m_craftButton, Window, "Close", Shelf.Words.Get(ShelfWord.Close), Shelf.Close);
		UiCopies.Place((RectTransform)close.transform, Width - Side - 140f, -(Height - Bottom + 12f), 140f, 44f);
	}
}
