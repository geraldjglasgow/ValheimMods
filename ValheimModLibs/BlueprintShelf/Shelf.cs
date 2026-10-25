using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BlueprintShelf;

/// <summary>
/// One mod's blueprint shelf (made 2026-10-23 from OpenKeep's Blueprint Bench window): a window in the game's own panel
/// look, opened at the host's station, with the player's own blueprints on the left (<see cref="IShelfFiles"/>, by
/// folder) and the world's shared pool on the right, kept by the server on its own disk by player and folder. Lines are
/// picked as in a file explorer, opened, renamed, moved by dragging and deleted or removed after the game's yes / no box;
/// dragging across (or Share and Take a copy) sends files into the player's part of the pool or copies them into the
/// player's files, packed and in parts. Everything of the host's comes in through <see cref="ShelfSettings"/>, its own
/// buttons included. <see cref="Create"/> once on every machine, dedicated server included (the server keeps the pool),
/// then <see cref="Open"/> at the station.
/// </summary>
public sealed class Shelf
{
	private Shelf(ShelfSettings settings)
	{
		Settings = settings;
		Rpc = new ShelfRpc(this);
		Store = new ShelfStore(this);
		Server = new ShelfServer(this);
		Pool = new ShelfPool(this);
		Share = new ShelfShare(this);
		Take = new ShelfTake(this);
		Window = new ShelfWindow(this);
	}

	public ShelfSettings Settings { get; }

	public IShelfFiles Files => Settings.Files;

	public ShelfWords Words => Settings.Words;

	internal ShelfLimits Limits => Settings.Limits;
	internal ShelfRpc Rpc { get; }
	internal ShelfStore Store { get; }
	internal ShelfServer Server { get; }
	internal ShelfPool Pool { get; }
	internal ShelfShare Share { get; }
	internal ShelfTake Take { get; }
	internal ShelfWindow Window { get; }

	/// <summary>
	/// Makes a shelf: its RPCs go on every network's table from now on (and on the current one), its window is treated as
	/// one of the game's while open, and its per-frame work runs on players' machines. The patches go in once per merged
	/// copy of the library, by hand.
	/// </summary>
	public static Shelf Create(ShelfSettings settings, Harmony harmony)
	{
		if (settings.Files == null)
		{
			throw new ArgumentException("ShelfSettings.Files is not set");
		}
		Shelf shelf = new Shelf(settings);
		ShelfHooks.Add(shelf, harmony);
		return shelf;
	}

	/// <summary>Open at a station that still stands (a destroyed station counts as closed).</summary>
	public bool IsOpen => Window.IsOpen;

	/// <summary>The station the window is open at, or null.</summary>
	public Component? Station => Window.Station;

	/// <summary>Opens the window for the local player at a station; the host checked the ward and its own rules.</summary>
	public void Open(Component station) => Run("window", () => Window.Open(station));

	public void Close() => Window.Close();

	/// <summary>The folder the player's own side shows ("" the top): where a host button saves a new blueprint.</summary>
	public string Folder => Window.Own.Folder;

	/// <summary>The blueprints picked on the player's own side, as paths.</summary>
	public List<string> PickedFiles => Window.Own.Picked(ShelfKind.File);

	/// <summary>The game's yes / no box over the window; yes runs <paramref name="yes"/>.</summary>
	public void Confirm(string header, string text, Action yes) => ShelfConfirm.Ask(this, header, text, yes);

	/// <summary>A message at the top left of the screen, through the game's localisation.</summary>
	public void Say(string text)
	{
		if (MessageHud.instance != null && !string.IsNullOrEmpty(text))
		{
			MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, Localization.instance.Localize(text));
		}
	}

	/// <summary>A folder as players read it: its path, or "the top folder".</summary>
	internal string Shown(string folder) => string.IsNullOrEmpty(folder) ? Words.Get(ShelfWord.Top) : folder;

	internal string? Clean(string? path, bool top = false) => ShelfPaths.Clean(path, Limits.NameLimit, top);

	internal Sprite? Icon(ShelfKind kind) => Settings.Icon?.Invoke(kind) ?? ShelfIcons.Get(kind);

	/// <summary>Per frame on a player's machine: the window, the pool listing, transfers.</summary>
	internal void Tick()
	{
		Window.Tick();
		Pool.Tick();
		Share.Tick();
		Take.Tick();
	}

	/// <summary>Runs a callback so a failure is logged and the game carries on.</summary>
	internal void Run(string what, Action action)
	{
		try
		{
			action();
		}
		catch (Exception e)
		{
			Error($"{Settings.Name} {what}: {e}");
		}
	}

	internal void Info(string text) => Settings.Log?.LogInfo($"{Settings.Name}: {text}");

	internal void Warn(string text)
	{
		if (Settings.Log != null)
		{
			Settings.Log.LogWarning($"{Settings.Name}: {text}");
		}
		else
		{
			Debug.LogWarning($"{Settings.Name}: {text}");
		}
	}

	private void Error(string text)
	{
		if (Settings.Log != null)
		{
			Settings.Log.LogError(text);
		}
		else
		{
			Debug.LogError(text);
		}
	}
}
