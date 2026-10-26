using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The operator's patches, installed by hand and by name, once per merged copy (no attributes, so a mod's PatchAll never
/// picks them up); each acts only on a player whose doodad control is one of this copy's weapons. <c>Player.SetControls</c>
/// prefix: his attack buttons reach the weapon (<see cref="MannedWeapon.Buttons"/>) and nothing else that would let go of
/// a helm or swing does (jump and dodge still let go); postfix: once the game has worked out the frame's movement, his
/// feet are walked to their spot (<see cref="OperatorFeet"/>). <c>Player.SetMouseLook</c> postfix: the view held to the
/// weapon's arc (<see cref="ViewLimit"/>). <c>Character.UpdateRotation</c> (private) prefix: his facing is the weapon's
/// (<see cref="OperatorFeet.Face"/>), kept while his feet shuffle round it; every character passes through it each
/// physics step and anyone else is untouched.
/// </summary>
internal static class WeaponPatches
{
	/// <summary>The patches are in: a weapon may be manned.</summary>
	public static bool Installed { get; private set; }

	public static IReadOnlyList<string> Install(Harmony harmony)
	{
		List<string> missing = GameTargets.Missing();
		if (Installed || missing.Count > 0)
		{
			return missing;
		}
		Installed = true;
		Patch(harmony, typeof(Player), "SetControls", nameof(Buttons), nameof(Steer));
		Patch(harmony, typeof(Player), "SetMouseLook", null, nameof(Look));
		Patch(harmony, typeof(Character), "UpdateRotation", nameof(Facing), null);
		return missing;
	}

	private static void Patch(Harmony harmony, Type type, string method, string? prefix, string? postfix) =>
		harmony.Patch(AccessTools.Method(type, method),
			prefix: prefix != null ? new HarmonyMethod(typeof(WeaponPatches), prefix) : null,
			postfix: postfix != null ? new HarmonyMethod(typeof(WeaponPatches), postfix) : null);

	private static void Buttons(Player __instance, ref bool attack, ref bool attackHold, ref bool secondaryAttack,
		ref bool secondaryAttackHold, ref bool block, ref bool blockHold)
	{
		if (__instance.GetDoodadController() is not MannedWeapon weapon)
		{
			return;
		}
		(bool a, bool ah, bool s, bool sh) = (attack, attackHold, secondaryAttack, secondaryAttackHold);
		if (a || ah || s || sh)
		{
			Safe.Run(weapon.Design.TriggerContext, static (w, p, pressed) => w.Buttons(p, pressed.a, pressed.ah, pressed.s, pressed.sh), weapon, __instance, (a, ah, s, sh));
		}
		(attack, attackHold, secondaryAttack, secondaryAttackHold, block, blockHold) = (false, false, false, false, false, false);
	}

	private static void Steer(Player __instance)
	{
		if (__instance.GetDoodadController() is MannedWeapon weapon)
		{
			Safe.Run(weapon.Design.SteerContext, static (w, p) => w.Feet.Steer(w, p), weapon, __instance);
		}
	}

	private static void Look(Player __instance) => ViewLimit.Apply(__instance);

	private static bool Facing(Character __instance, float dt, ref float __result)
	{
		if (__instance is not Player player || player.GetDoodadController() is not MannedWeapon weapon)
		{
			return true;
		}
		__result = weapon.Feet.Face(weapon, player, dt);
		return false;
	}
}
