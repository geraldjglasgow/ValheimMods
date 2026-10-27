using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// An unmanned weapon going back to its rest lay (<see cref="WeaponDesign.Rest"/>), on its owner, each physics step: when
/// no valid operator holds it (he let go, walked off, died or left the game, the owner then being whoever the game gave
/// the ZDO to, a dedicated server too) and its aim is off the rest lay, the aim is turned toward it at the weapon's own
/// turn and tilt rates and written to the ZDO, every peer easing after it as after an operator's aim. At rest nothing is
/// written, and it is looked at again only every <see cref="Idle"/> seconds. Nothing else writes the aim: a called shot
/// fires where the weapon points, and a reload leaves it alone.
/// </summary>
internal static class RestLay
{
	/// <summary>Seconds between looks at a weapon at rest, or manned.</summary>
	private const float Idle = 0.5f;

	/// <summary>On the owner of a weapon nobody holds on this machine: one step toward the rest lay, when due.</summary>
	public static void Tick(MannedWeapon weapon)
	{
		if (Time.time < weapon.restAt || weapon.Frame == null)
		{
			return;
		}
		WeaponState s = weapon.State;
		if (WeaponHold.ValidUser(weapon, s.User) || !Toward(weapon, s, Time.fixedDeltaTime))
		{
			weapon.restAt = Time.time + Idle;
		}
	}

	/// <summary>The rest lay held within the arcs; (0, 0) for a weapon without one.</summary>
	public static (float Yaw, float Pitch) Lay(WeaponDesign design)
	{
		WeaponRest? rest = design.Rest;
		WeaponArcs arcs = design.Aim;
		return rest == null ? (0f, 0f) : (arcs.ClampYaw(rest.Yaw), Mathf.Clamp(rest.Pitch, arcs.PitchDown, arcs.PitchUp));
	}

	// One step toward the rest lay, written; false when it is there (nothing written).
	private static bool Toward(MannedWeapon weapon, in WeaponState s, float dt)
	{
		WeaponArcs arcs = weapon.Design.Aim;
		(float yaw, float pitch) = Lay(weapon.Design);
		if (Mathf.Abs(Mathf.DeltaAngle(s.Yaw, yaw)) < 0.01f && Mathf.Abs(s.Pitch - pitch) < 0.01f)
		{
			return false;
		}
		ZDO zdo = weapon.Net.GetZDO();
		zdo.Set(weapon.Design.Keys.Yaw, arcs.Turn(s.Yaw, yaw, arcs.TurnRate * dt));
		zdo.Set(weapon.Design.Keys.Pitch, Mathf.MoveTowards(s.Pitch, pitch, arcs.TiltRate * dt));
		return true;
	}
}
