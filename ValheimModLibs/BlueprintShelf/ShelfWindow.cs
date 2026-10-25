using UnityEngine;

namespace BlueprintShelf;

/// <summary>
/// The window, opened at the host's station: the player's own blueprints on the left, the world's shared pool on the
/// right, each a file explorer (<see cref="OwnPane"/>, <see cref="PoolPane"/>) drawn in the game's own panel look
/// (<see cref="ShelfUi"/>). While it is open the game treats it as one of its own windows (WindowInput: free cursor, no
/// attacks or mouse look, Esc closes it, unless the game's name box or yes / no box is over it, whose Esc it is). The
/// rename key renames the one line picked in the side last used, Delete deletes or removes its picks. It closes when the
/// player walks away, dies, opens the inventory or the map, the station goes or the shelf is switched off.
/// </summary>
internal sealed class ShelfWindow
{
	private readonly Shelf shelf;
	private readonly ShelfUi ui;
	private PaneView? focused;

	public ShelfWindow(Shelf shelf)
	{
		this.shelf = shelf;
		ui = new ShelfUi(shelf);
		Own = new OwnPane(shelf);
		Pool = new PoolPane(shelf);
	}

	public OwnPane Own { get; }
	public PoolPane Pool { get; }

	public Component? Station { get; private set; }

	/// <summary>Open at a station that still stands (a destroyed station counts as closed).</summary>
	public bool IsOpen => Station != null && ui.Shown;

	public void Open(Component at)
	{
		if (!ui.Ensure(Own, Pool))
		{
			return;
		}
		Station = at;
		focused = ui.Left;
		shelf.Pool.Open();
		ui.Show(true);
		Tick();
	}

	public void Close()
	{
		Station = null;
		ui.Drag.End();
		ui.Show(false);
	}

	/// <summary>Esc while the window is open: it closes, unless a box of the game's is over it (that Esc is the box's).</summary>
	public void Escape()
	{
		if (!ShelfPrompt.Showing && !UnifiedPopup.IsVisible() && !UnifiedPopup.WasVisibleThisFrame())
		{
			Close();
		}
	}

	public void Focus(PaneView pane) => focused = pane;

	public void Tick()
	{
		if (!ui.Shown)
		{
			return;
		}
		if (!Stays())
		{
			Close();
			return;
		}
		ui.Tick();
		ui.Status.text = Status();
		Keys();
	}

	private bool Stays()
	{
		Player player = Player.m_localPlayer;
		return Station != null && player != null && !player.IsDead() && shelf.Settings.Enabled() && !InventoryGui.IsVisible()
			&& !Minimap.IsOpen() && Vector3.Distance(player.transform.position, Station.transform.position) <= shelf.Settings.Reach;
	}

	private void Keys()
	{
		if (focused == null || ShelfPrompt.Showing || UnifiedPopup.IsVisible() || ui.Drag.Active)
		{
			return;
		}
		if (Input.GetKeyDown(shelf.Settings.RenameKey))
		{
			focused.RenamePicked();
		}
		else if (Input.GetKeyDown(KeyCode.Delete))
		{
			focused.Model.Remove();
		}
	}

	private string Status()
	{
		if (shelf.Share.Busy)
		{
			return shelf.Words.Format(ShelfWord.Sharing, shelf.Share.Left);
		}
		if (shelf.Take.Busy)
		{
			return shelf.Words.Format(ShelfWord.Taking, shelf.Take.Left);
		}
		if (!shelf.Pool.Loaded)
		{
			return shelf.Words.Get(shelf.Pool.Silent ? ShelfWord.NoAnswer : ShelfWord.Asking);
		}
		return shelf.Words.Get(ShelfWord.Hint);
	}
}
