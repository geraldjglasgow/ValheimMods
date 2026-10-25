using HarmonyLib;
using UnityEngine;

namespace BuildCamera;

/// <summary>
/// Esc brings the camera back instead of opening the game menu, the first time: a prefix on the private
/// <c>Menu.Update</c>, run before any other (<see cref="Priority.First"/>), skips the menu's frame and ends the trip
/// when Esc (or the gamepad's menu button) is pressed while the flight lets it and nothing else that Esc closes was open
/// this frame or the last: the game menu, the inventory, chat, console, map, a text box, a window of any mod (those take
/// the player's input away, as WindowInput's do), the build or radial menu or a popup. So Esc that closes one of those
/// closes only that, and WindowInput's own Esc prefix, which runs after this one, still closes a mod's window first.
/// </summary>
internal static class EscapePatch
{
	private static bool wasFree;

	public static void Install(Harmony harmony) =>
		harmony.Patch(AccessTools.Method(typeof(Menu), "Update"), prefix: new HarmonyMethod(typeof(EscapePatch), nameof(Escape)) { priority = Priority.First });

	private static bool Escape()
	{
		CameraFlight? flight = CameraTrip.Current;
		bool free = flight != null && flight.EscapeLeaves && Free(Player.m_localPlayer);
		bool leave = free && wasFree && Pressed();
		wasFree = free;
		if (!leave)
		{
			return true;
		}
		CameraTrip.End(CameraEnd.Escape);
		return false;
	}

	private static bool Free(Player player) =>
		GameAccess.TakeInput(player) && !Menu.IsVisible() && !Hud.IsPieceSelectionVisible() && !Hud.InRadial() && !UnifiedPopup.IsVisible()
		&& (Chat.instance == null || !Chat.instance.m_wasFocused);

	private static bool Pressed() =>
		ZInput.GetKeyDown(KeyCode.Escape) || (ZInput.GetButtonDown("JoyMenu") && !ZInput.GetButton("JoyAltKeys"));
}
