using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The weapon's aim. On the operator's machine, each drawn frame, the weapon is laid so its shot lands on what the
/// crosshair is on (<see cref="WeaponLay"/>: from where the shot leaves, <see cref="MannedWeapon.Flight"/>, along a
/// straight line or the shot's arc, the carrying body's motion led off; measured from the plane the weapon stands on, so
/// a base tilted on a slope or a rolling deck is taken off), held within the arcs and turned at the weapon's rates
/// (<see cref="WeaponArcs"/>). The view itself turns and tilts no further than the weapon (<see cref="ViewLimit"/>,
/// <see cref="ViewTilt"/>), so the crosshair is never on what the weapon cannot reach. It goes to the ZDO when it has
/// moved a little; every other peer eases toward that. From the trigger until the shot has left
/// (<see cref="MannedWeapon.Steady"/>) it stays laid on what the crosshair was on as the trigger was pulled
/// (<see cref="Mark"/>). Each drawn frame on every peer the yaw and pitch parts are turned to it, a shot's kick on top.
/// </summary>
public sealed class WeaponAim
{
	private float shownYaw, shownPitch;

	/// <summary>The operator's aim, degrees from where the weapon was placed (yaw to the right, pitch up), ahead of what the ZDO has sent.</summary>
	public float Yaw { get; private set; }

	public float Pitch { get; private set; }

	/// <summary>
	/// What the crosshair was on as the trigger was last pulled (taken by the library's trigger before each shot; none
	/// after taking hold): while the shot is on its way (<see cref="MannedWeapon.Steady"/>) the weapon stays laid on it and
	/// the shot leaves onto it (<see cref="WeaponShot.Way"/>), so a deck rolling or turning under it, or the operator
	/// stepping aside, does not carry the shot off.
	/// </summary>
	public Sight? Mark { get; internal set; }

	internal void Begin(in WeaponState s) => (Yaw, Pitch, Mark) = (s.Yaw, s.Pitch, null);

	/// <summary>The operator's lay toward a sight: the weapon laid so its shot lands there, held within the arcs, at its rates.</summary>
	internal void Step(MannedWeapon weapon, in Sight sight, float dt)
	{
		if (weapon.Frame == null || !weapon.Net.IsValid())
		{
			return;
		}
		WeaponArcs arcs = weapon.Design.Aim;
		(float yaw, float pitch) = WeaponLay.Solve(weapon, sight, Yaw, Pitch);
		(yaw, pitch) = WeaponLay.Hold(weapon, arcs, yaw, pitch);
		Pitch = Mathf.MoveTowards(Pitch, pitch, arcs.TiltRate * dt);
		Yaw = arcs.Turn(Yaw, yaw, arcs.TurnRate * dt);
		if (weapon.Net.IsOwner())
		{
			Write(weapon.Net.GetZDO(), weapon.Design.Keys.Yaw, Yaw, arcs.WriteStep);
			Write(weapon.Net.GetZDO(), weapon.Design.Keys.Pitch, Pitch, arcs.WriteStep);
		}
	}

	/// <summary>The operator's own aim, worked out here each frame (on the mark while <see cref="MannedWeapon.Steady"/>, held still without one); anyone else's eased toward the ZDO's; the kick on top.</summary>
	internal void Turn(MannedWeapon weapon, WeaponFrame frame, in WeaponState s, Player? operating)
	{
		if (operating != null)
		{
			Lay(weapon, operating, s);
			(shownYaw, shownPitch) = (Yaw, Pitch);
		}
		else
		{
			float ease = 1f - Mathf.Exp(-weapon.Design.Aim.Follow * Time.deltaTime);
			(shownYaw, shownPitch) = (Mathf.LerpAngle(shownYaw, s.Yaw, ease), Mathf.LerpAngle(shownPitch, s.Pitch, ease));
		}
		float kick = weapon.Kick(s);
		frame.Yaw.localRotation = Quaternion.Euler(0f, shownYaw, 0f);
		frame.Pitch.localRotation = Quaternion.Euler(-(shownPitch + kick), 0f, 0f);
	}

	// The operator's lay this frame: onto the crosshair, or while the shot is on its way onto the mark.
	private void Lay(MannedWeapon weapon, Player operating, in WeaponState s)
	{
		if (!weapon.Steady(s))
		{
			Step(weapon, WeaponSight.Crosshair(weapon, operating), Time.deltaTime);
		}
		else if (Mark is Sight mark)
		{
			Step(weapon, mark, Time.deltaTime);
		}
	}

	private static void Write(ZDO zdo, int key, float value, float step)
	{
		if (Mathf.Abs(zdo.GetFloat(key) - value) > step)
		{
			zdo.Set(key, value);
		}
	}
}
