using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MapClicks;

/// <summary>
/// A click on a mod's own icon on the large map that leaves the game's pins working under it. <see cref="Hold"/> runs
/// the icon's action once the game's double click window has passed, and a double click there drops it, so the game's
/// own double click places a pin under the icon instead. <see cref="PinUnderPointer"/> tells a right click that one of
/// the player's placed pins is under the pointer, which the game's removal gets before the icon. Installed once per
/// merged copy, by hand, no attributes; private members are reached by name.
/// </summary>
public static class IconClick
{
	/// <summary>The game's window between the two clicks of a double click (<c>Minimap.OnMapLeftUp</c>).</summary>
	public const float DoubleClickSeconds = 0.3f;

	private static bool patched;
	private static Action? held;
	private static float due;
	private static MethodInfo? closestPin;
	private static MethodInfo? toWorld;
	private static PropertyInfo? radius;

	public static void Install(Harmony harmony)
	{
		if (patched)
		{
			return;
		}
		patched = true;
		harmony.Patch(AccessTools.Method(typeof(Minimap), "OnMapDblClick"), prefix: new HarmonyMethod(typeof(IconClick), nameof(Drop)));
		harmony.Patch(AccessTools.Method(typeof(Minimap), "Update"), postfix: new HarmonyMethod(typeof(IconClick), nameof(RunDue)));
	}

	/// <summary>Runs the action after the double click window, unless a double click comes first; a later hold replaces it.
	/// The action runs on a later frame, so it checks again that what it acts on is still there.</summary>
	public static void Hold(Action action)
	{
		held = action;
		due = Time.time + DoubleClickSeconds;
	}

	/// <summary>Forgets the held action. Also the game's double click prefix: the pin is placed, the icon's action is not.</summary>
	public static void Drop() => held = null;

	/// <summary>Whether one of the player's placed pins lies under the pointer, as the game's own click and removal find it.</summary>
	public static bool PinUnderPointer(Minimap map)
	{
		closestPin ??= AccessTools.Method(typeof(Minimap), "GetClosestPin");
		toWorld ??= AccessTools.Method(typeof(Minimap), "ScreenToWorldPoint");
		radius ??= AccessTools.Property(typeof(Minimap), "PinInteractRadius");
		if (map == null || closestPin == null || toWorld == null || radius == null)
		{
			return false;
		}
		object point = toWorld.Invoke(map, new object[] { ZInput.pointerPosition });
		return closestPin.Invoke(map, new object[] { point, radius.GetValue(map), true }) != null;
	}

	// Minimap.Update postfix: runs even when another mod's prefix replaced the game's update.
	private static void RunDue()
	{
		if (held == null || Time.time < due)
		{
			return;
		}
		Action action = held;
		held = null;
		action();
	}
}
