using System;
using System.Collections.Generic;
using HarmonyLib;

namespace WindowInput;

/// <summary>
/// A mod's own windows, treated like the game's own while one is open (<see cref="WindowPatches"/>): the cursor is
/// shown and free, the player does not attack, block, use hotkeys or turn the camera with the mouse, the mouse wheel
/// does not zoom, and Esc closes the window instead of opening the game menu. Walking stays possible, as with the
/// inventory. <see cref="Install"/> once per mod (the patches are installed once per merged copy of this library),
/// then <see cref="Add"/> each window with how to tell it is open and how to close it, and, for a window that lets the
/// wheel through where the pointer is (a tool's window whose wheel zooms the view off the window), when it does.
/// </summary>
public static class GameWindow
{
	private sealed class Entry
	{
		public Func<bool> IsOpen = null!;
		public Action Close = null!;
		public Func<bool>? PassesWheel;
	}

	private static readonly List<Entry> windows = new();

	public static void Install(Harmony harmony) => WindowPatches.Install(harmony);

	public static void Add(Func<bool> isOpen, Action close, Func<bool>? passesWheel = null) =>
		windows.Add(new Entry { IsOpen = isOpen, Close = close, PassesWheel = passesWheel });

	/// <summary>
	/// An open window keeps the mouse wheel from the game (no zoom): every open window but one that lets the wheel through
	/// now. A window whose check throws keeps it.
	/// </summary>
	public static bool HoldsWheel
	{
		get
		{
			foreach (Entry window in windows)
			{
				if (Open(window) && !Passes(window))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static bool Passes(Entry window)
	{
		try
		{
			return window.PassesWheel != null && window.PassesWheel();
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>One of this mod's windows is open. A window whose check throws counts as closed.</summary>
	public static bool AnyOpen
	{
		get
		{
			foreach (Entry window in windows)
			{
				if (Open(window))
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>Closes every open window of this mod (Esc). False when none was open.</summary>
	public static bool CloseOpen()
	{
		bool closed = false;
		foreach (Entry window in windows)
		{
			if (Open(window))
			{
				window.Close();
				closed = true;
			}
		}
		return closed;
	}

	private static bool Open(Entry window)
	{
		try
		{
			return window.IsOpen();
		}
		catch (Exception)
		{
			return false;
		}
	}
}
