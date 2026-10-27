using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// How far and how fast a weapon turns, and the operator's view with it. Yaw is degrees right of where the weapon was
/// placed, pitch degrees up. <see cref="YawLimit"/> each way (180 or more: all round, no stop), <see cref="PitchDown"/>
/// to <see cref="PitchUp"/>. The weapon follows the view's yaw at <see cref="TurnRate"/> and lays its pitch on what the
/// crosshair is on (out to <see cref="Reach"/>, short of the sea with <see cref="StopAtSea"/>) at <see cref="TiltRate"/>,
/// degrees a second. While manned the view itself turns no further than the weapon (it stops at the arc's end, as the
/// weapon does) and, with a <see cref="ViewRate"/>, no faster, so view, weapon and the operator's feet stop together; with
/// <see cref="ViewTiltStops"/> (the default) it tilts no further than the weapon can lay itself on what the crosshair is
/// on and no faster than <see cref="TiltRate"/>, so view and barrel stop together at the pitch arc's ends too; and always
/// from <see cref="LookUp"/> above level to <see cref="LookDown"/> below. The aim goes to the ZDO when it moves more than
/// <see cref="WriteStep"/>; other peers ease toward it at <see cref="Follow"/>.
/// </summary>
public sealed class WeaponArcs
{
	public float YawLimit { get; set; } = 180f;
	public float PitchDown { get; set; } = -10f;
	public float PitchUp { get; set; } = 25f;
	public float TurnRate { get; set; } = 720f;
	public float TiltRate { get; set; } = 200f;

	/// <summary>The most the view turns a second while manned; 0: as fast as the mouse.</summary>
	public float ViewRate { get; set; }

	/// <summary>While manned the view tilts only as far as the weapon can follow it, and no faster (<see cref="ViewTilt"/>); false: the view tilts freely within <see cref="LookUp"/> and <see cref="LookDown"/>.</summary>
	public bool ViewTiltStops { get; set; } = true;

	public float LookUp { get; set; } = 89f;
	public float LookDown { get; set; } = 89f;
	public float Follow { get; set; } = 12f;
	public float WriteStep { get; set; } = 0.1f;
	public float Reach { get; set; } = 150f;
	public bool StopAtSea { get; set; }

	/// <summary>The weapon turns all round, with no stop.</summary>
	public bool AllRound => YawLimit >= 180f;

	/// <summary>A yaw (-180 to 180) held within the arc.</summary>
	public float ClampYaw(float yaw) => AllRound ? yaw : Mathf.Clamp(yaw, -YawLimit, YawLimit);

	/// <summary>A yaw turned toward another by at most `most` degrees: across the back for an all-round weapon, never past a stop otherwise.</summary>
	public float Turn(float from, float to, float most) =>
		AllRound ? Mathf.DeltaAngle(0f, Mathf.MoveTowardsAngle(from, to, most)) : Mathf.MoveTowards(from, to, most);
}
