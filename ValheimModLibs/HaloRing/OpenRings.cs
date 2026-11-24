using System;
using HarmonyLib;
using WindowInput;

namespace HaloRing;

/// <summary>
/// The rings of this copy of the library (each mod merges its own) and the one open among them. Opening one closes
/// another of the same copy as a cancel. Across copies a ring does not open while another mod's is open: each copy
/// leaves a check of its own rings in the process-wide slot <see cref="SharedSlot"/> as it opens (a plain
/// <c>Func&lt;bool&gt;</c>, a type every copy shares), and the next opener asks it. While one is open, or a mouse
/// button held back after a ring closed is still down, this copy's WindowInput holds the game's input like a window.
/// </summary>
internal static class OpenRings
{
	private const string SharedSlot = "HaloRing.openRing";
	private static readonly Func<bool> anyOpen = () => Current != null;
	private static bool installed;
	private static int heldButton = -1;

	public static HaloRingMenu? Current { get; private set; }

	public static void Install(Harmony harmony)
	{
		if (installed)
			return;
		installed = true;
		GameWindow.Install(harmony);
		GameWindow.Add(HoldsInput, CancelCurrent);
	}

	public static void HoldUntilReleased(int mouseButton) => heldButton = mouseButton;

	/// <summary>Closes another of this copy's rings; false when another mod's ring is open (nothing changes then).</summary>
	public static bool Claim(HaloRingMenu ring)
	{
		if (Current != null && Current != ring)
			Current.Cancel();
		object? other = AppDomain.CurrentDomain.GetData(SharedSlot);
		if (other is Func<bool> check && !ReferenceEquals(check, anyOpen) && Asks(check))
			return false;
		AppDomain.CurrentDomain.SetData(SharedSlot, anyOpen);
		Current = ring;
		return true;
	}

	public static void Closed(HaloRingMenu ring)
	{
		if (Current == ring)
			Current = null;
	}

	public static void CloseOthers(HaloRingMenu ring)
	{
		if (Current != null && Current != ring)
			Current.Cancel();
	}

	private static bool Asks(Func<bool> check)
	{
		try
		{
			return check();
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static bool HoldsInput()
	{
		if (Current != null)
			return true;
		if (heldButton >= 0 && !UnityEngine.Input.GetMouseButton(heldButton))
			heldButton = -1;
		return heldButton >= 0;
	}

	private static void CancelCurrent()
	{
		heldButton = -1;
		Current?.Cancel();
	}
}
