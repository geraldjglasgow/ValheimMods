using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The operator's view turned with his weapon, local player only, for a weapon with a stop (not all round): never so far
/// that the weapon would have to turn past its arc to lay its shot on what the crosshair is then on. The crosshair's ray
/// runs beside the weapon's line (the camera looks over the operator's right shoulder), so at the arc's end the weapon's
/// yaw and the view's differ by a degree or so; the view's ray is followed to what it is on (<see cref="WeaponSight"/>),
/// the weapon laid on it (<see cref="WeaponLay"/>) and the view turned back by what the weapon lacks, a few steps a
/// frame. So at either end the crosshair stops on what the weapon can still reach, as <see cref="ViewTilt"/> holds it
/// at the pitch arc's ends.
/// </summary>
internal static class ViewTurn
{
	private const int Tries = 3;
	private const float Near = 0.02f;

	/// <summary>The view's yaw (degrees right of where the weapon was placed), or the nearest to it whose crosshair the weapon can still turn to.</summary>
	public static float Held(MannedWeapon weapon, Player player, WeaponArcs arcs, float placed, float yaw)
	{
		if (arcs.AllRound || GameCamera.instance == null || weapon.Frame == null)
		{
			return yaw;
		}
		for (int i = 0; i < Tries; i++)
		{
			Vector3 ahead = Quaternion.Euler(arcs.ViewTiltStops ? ViewTilt.Now : GameAccess.LookPitch(player), placed + yaw, 0f) * Vector3.forward;
			Sight sight = WeaponSight.Target(weapon, arcs, ViewTilt.Camera(player, ahead), ahead);
			float need = WeaponLay.Solve(weapon, sight, weapon.Aim.Yaw, weapon.Aim.Pitch).Yaw, over = need - arcs.ClampYaw(need);
			if (Mathf.Abs(over) < Near)
			{
				return yaw;
			}
			yaw -= over;
		}
		return yaw;
	}
}
