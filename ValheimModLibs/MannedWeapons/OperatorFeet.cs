using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The operator's feet, on his own machine, each physics step after the game has worked out his controls (which the
/// doodad control leaves still): nothing holds him; the game's own movement walks him to the weapon's stand point
/// (<see cref="MannedWeapon.StandPoint"/>; on the yaw part it swings round as the weapon turns, so turning it walks him
/// round it), at a jog while walking up or keeping up with a big turn, easing in over the last stretch. He stops a few
/// centimetres from it and starts again only once it has moved on (a small turn moves the hands, not the feet), and gives
/// up on a spot he cannot reach after a moment without moving. He faces the way he walks while walking up, then the way
/// the weapon points, so a turn plays as a side step (<see cref="Face"/>, in place of the game's turning). Where he stopped
/// is kept in the weapon's own space and his speed measured against the body it stands on, so a moving deck changes
/// nothing. Every number is the design's (<see cref="OperatorBody"/>).
/// </summary>
public sealed class OperatorFeet
{
	private bool arrived, settled;
	private float stuck;
	private Vector3 restAt;

	/// <summary>Taking hold: walking up afresh.</summary>
	internal void Start() => (arrived, settled, stuck) = (false, false, 0f);

	/// <summary>Where his feet go this step, and his facing.</summary>
	internal void Steer(MannedWeapon weapon, Player player)
	{
		if (weapon.Frame == null || !weapon.Net.IsValid())
		{
			return;
		}
		OperatorBody body = weapon.Design.Body;
		Vector3 spot = weapon.StandPoint(weapon.State);
		Vector3 to = spot - player.transform.position;
		to.y = 0f;
		float distance = to.magnitude;
		GameAccess.MoveDir(player) = Stays(weapon, player, body, spot, distance) ? Vector3.zero : to / distance * Mathf.Min(1f, distance / body.Slow);
		Face(weapon, player, Time.fixedDeltaTime);
	}

	/// <summary>Whether the feet stay put this step: at rest and the spot still near where they stopped, arrived, or blocked.</summary>
	private bool Stays(MannedWeapon weapon, Player player, OperatorBody body, Vector3 spot, float distance)
	{
		if (settled && (spot - weapon.transform.TransformPoint(restAt)).magnitude < body.Restart)
		{
			return true;
		}
		stuck = Speed(weapon, player) < body.Still ? stuck + Time.fixedDeltaTime : 0f;
		if (distance < body.Arrive || stuck > body.GiveUp)
		{
			(settled, arrived, stuck) = (true, true, 0f);
			restAt = weapon.transform.InverseTransformPoint(spot);
			return true;
		}
		settled = false;
		return false;
	}

	/// <summary>His speed over what the weapon stands on.</summary>
	private static float Speed(MannedWeapon weapon, Player player)
	{
		Vector3 velocity = player.GetVelocity();
		Rigidbody? carrier = weapon.Carrier;
		return (carrier != null ? velocity - carrier.GetPointVelocity(player.transform.position) : velocity).magnitude;
	}

	/// <summary>
	/// His facing, in place of the game's: the way he goes while still walking up, then the way the weapon points, turned
	/// into fast. Returns the turn for the game's turning animation (radians a second).
	/// </summary>
	internal float Face(MannedWeapon weapon, Player player, float dt)
	{
		OperatorBody body = weapon.Design.Body;
		Vector3 travel = GameAccess.MoveDir(player), ahead = weapon.Ahead;
		Vector3 wanted = !arrived && travel.magnitude > body.Walking ? travel : ahead;
		wanted.y = 0f;
		if (wanted.sqrMagnitude < 1e-6f)
		{
			return 0f;
		}
		Quaternion facing = Quaternion.LookRotation(wanted);
		float delta = Utils.GetYawDeltaAngle(player.transform.rotation, facing);
		player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, facing, body.TurnRate * dt);
		return Mathf.Abs(delta) > 1f ? body.TurnRate * Mathf.Sign(delta) * Mathf.Deg2Rad : 0f;
	}
}
