using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// Esc lets go of a weapon instead of opening the game menu, the first time (optional,
/// <see cref="MannedWeapon.InstallEscape"/>): a prefix on the private <c>Menu.Update</c>, run before any other, skips the
/// menu's frame and lets go when Esc (or the gamepad's menu button) is pressed while the local player mans one of this
/// copy's weapons and nothing else that Esc closes was open this frame or the last (the menu, the inventory, chat, the
/// console, the map, a text box, any mod's window, the build or radial menu, a popup), so Esc that closes one of those
/// closes only that.
/// </summary>
internal static class WeaponEscape
{
	private static bool installed, wasFree;

	public static IReadOnlyList<string> Install(Harmony harmony)
	{
		List<string> missing = GameTargets.Missing();
		if (installed || missing.Count > 0 || AccessTools.Method(typeof(Menu), "Update") == null)
		{
			return missing;
		}
		installed = true;
		harmony.Patch(AccessTools.Method(typeof(Menu), "Update"), prefix: new HarmonyMethod(typeof(WeaponEscape), nameof(Escape)) { priority = Priority.First });
		return missing;
	}

	private static bool Escape()
	{
		Player? player = Player.m_localPlayer;
		bool free = player != null && player.GetDoodadController() is MannedWeapon && Free(player);
		bool leave = free && wasFree && Pressed();
		wasFree = free;
		if (!leave)
		{
			return true;
		}
		player!.StopDoodadControl();
		return false;
	}

	private static bool Free(Player player) =>
		GameAccess.TakeInput(player) && !Menu.IsVisible() && !Hud.IsPieceSelectionVisible() && !Hud.InRadial() && !UnifiedPopup.IsVisible()
		&& (Chat.instance == null || !Chat.instance.m_wasFocused);

	private static bool Pressed() =>
		ZInput.GetKeyDown(KeyCode.Escape) || (ZInput.GetButtonDown("JoyMenu") && !ZInput.GetButton("JoyAltKeys"));
}
