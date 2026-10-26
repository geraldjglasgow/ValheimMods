using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The operator's view stays within the weapon's own turn and moves at its pace (<see cref="WeaponArcs"/>): after each
/// mouse movement the view turns no further either side of where the weapon was placed than the weapon turns (no
/// looking behind a weapon that cannot turn there) and, with a view rate, no faster; it tilts within its own limits. The
/// mouse beyond that is not taken, so the view, the weapon and the operator's feet move together and stop together.
/// Local player only.
/// </summary>
internal static class ViewLimit
{
	private static MannedWeapon? held;
	private static float yaw;

	public static void Apply(Player player)
	{
		if (player.GetDoodadController() is not MannedWeapon weapon || !weapon.IsValid())
		{
			held = null;
			return;
		}
		WeaponArcs arcs = weapon.Design.Aim;
		float placed = weapon.transform.eulerAngles.y;
		if (weapon != held)
		{
			(held, yaw) = (weapon, weapon.Aim.Yaw);   // the view starts where the weapon points
		}
		float wanted = arcs.ClampYaw(Mathf.DeltaAngle(placed, GameAccess.LookYaw(player).eulerAngles.y));
		yaw = arcs.ViewRate > 0f ? arcs.Turn(yaw, wanted, arcs.ViewRate * Time.deltaTime) : wanted;
		GameAccess.LookYaw(player) = Quaternion.Euler(0f, placed + yaw, 0f);
		GameAccess.LookPitch(player) = Mathf.Clamp(GameAccess.LookPitch(player), -arcs.LookUp, arcs.LookDown);
		GameAccess.UpdateEyeRotation(player);
		GameAccess.LookDir(player) = player.m_eye.forward;
	}
}
