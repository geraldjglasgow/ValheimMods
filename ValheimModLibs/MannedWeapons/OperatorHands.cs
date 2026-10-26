using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The operator's upper body at a weapon, on every peer that draws, after the animator has posed him (his legs are the
/// game's own walk, <see cref="OperatorFeet"/>): the weapon says where his fists go and how far he leans
/// (<see cref="MannedWeapon.Pose"/>), and they go there by two-bone IK. All from the ZDO, so every screen shows the
/// same. The hands reach out over the last stride up to the stand point and let go as quickly.
/// </summary>
internal sealed class OperatorHands
{
	private Player? player;
	private OperatorRig? rig;
	private float weight;

	public void Pose(MannedWeapon weapon, in WeaponState s, float dt)
	{
		if (!Follow(weapon, s, dt) || rig == null || player == null)
		{
			weapon.Released(s);
			return;
		}
		Transform body = player.transform;
		OperatorPose pose = weapon.Pose(s, rig, body);
		float held = weight * pose.Hold;
		rig.Lean(pose.Lean * held, body);
		if (pose.Left is Vector3 left)
		{
			rig.Left.Reach(left, held, body);
		}
		if (pose.Right is Vector3 right)
		{
			rig.Right.Reach(right, held, body);
		}
		weapon.Gripped(s, rig, body);
	}

	/// <summary>The operator (kept while the hands let go after he did) and how fully his hands are on the weapon; false when nobody's are.</summary>
	private bool Follow(MannedWeapon weapon, in WeaponState s, float dt)
	{
		Player? holder = s.User != 0L ? Player.GetPlayer(s.User) : null;
		if (holder != null && holder != player)
		{
			(player, rig, weight) = (holder, OperatorRig.Of(holder, weapon.Design.Body.Elbows), 0f);
		}
		weight = Mathf.MoveTowards(weight, holder != null ? AtWeapon(weapon, holder, s) : 0f, weapon.Design.Body.Ease * dt);
		if (weight > 0.001f)
		{
			return true;
		}
		if (holder == null)
		{
			(player, rig) = (null, null);
		}
		return false;
	}

	/// <summary>1 once the operator's feet are at the spot the weapon asks for, 0 while still walking up to it.</summary>
	private static float AtWeapon(MannedWeapon weapon, Player holder, in WeaponState s)
	{
		OperatorBody body = weapon.Design.Body;
		Vector3 off = holder.transform.position - weapon.StandPoint(s);
		off.y = 0f;
		return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(body.Reached, body.Reaching, off.magnitude));
	}
}
