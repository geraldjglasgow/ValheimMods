using LimbIk;
using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The operator's lean and his fists' targets for one frame (<see cref="MannedWeapon.Pose"/>): degrees leaned forward,
/// the world points his left and right fists go to (null: that hand as the game poses it), and how fully he holds on
/// (1; less to let go, as while a carriage recoils).
/// </summary>
public readonly struct OperatorPose
{
	public readonly float Lean, Hold;
	public readonly Vector3? Left, Right;

	public OperatorPose(float lean, Vector3? left, Vector3? right, float hold = 1f) => (Lean, Left, Right, Hold) = (lean, left, right, hold);
}

/// <summary>
/// One arm of the operator, posed after the animator: its fist's grip point (the game's hand attach) put on a target by
/// two-bone IK (LimbIk), the elbow toward its hint (<see cref="ElbowHint"/>), the wrist as the clip has it.
/// </summary>
public sealed class OperatorArm
{
	private readonly Transform upper, lower, hand, grip;
	private readonly float side;
	private readonly ElbowHint elbows;

	internal OperatorArm(Transform upper, Transform lower, Transform hand, Transform grip, float side, ElbowHint elbows) =>
		(this.upper, this.lower, this.hand, this.grip, this.side, this.elbows) = (upper, lower, hand, grip, side, elbows);

	/// <summary>Where the fist grips now.</summary>
	public Vector3 Grip => grip.position;

	/// <summary>The fist toward `target` by `weight` (1: on it, as far as the arm reaches).</summary>
	public void Reach(Vector3 target, float weight, Transform body)
	{
		Vector3 goal = Vector3.Lerp(grip.position, target, weight);
		Vector3 hint = elbows == ElbowHint.Square ? Square(goal, body) : upper.position + body.right * (side * 0.35f) + Vector3.down * 0.35f - body.forward * 0.15f;
		for (int pass = 0; pass < 2; pass++)
		{
			TwoBoneIk.Solve(upper, lower, hand, goal - (grip.position - hand.position), hint);
		}
	}

	/// <summary>Out to his side, back and a little down, square to the line from the shoulder to the fist.</summary>
	private Vector3 Square(Vector3 goal, Transform body)
	{
		Vector3 shoulder = upper.position, line = goal - shoulder;
		Vector3 wanted = body.right * (side * 0.7f) - body.forward * 0.5f + Vector3.down * 0.3f;
		Vector3 square = Vector3.ProjectOnPlane(wanted, line.normalized);
		if (square.sqrMagnitude < 0.01f)
		{
			square = body.right * side;
		}
		return shoulder + line * 0.5f + square.normalized * 0.3f;
	}
}

/// <summary>
/// The bones of the operator a weapon poses: the spine (leaned forward in two halves), the hips (to carry something from)
/// and both arms. A lean goes only on a pose the animator wrote this frame, never twice on the same pose (an animator
/// updating with physics skips frames): the spine is the leaned pose left from before only when it is that pose bit for
/// bit. (Compared within Quaternion.Angle's rounding, about 0.16 degrees, an animator's fresh pose that close to the last
/// leaned one was taken for it, so after a very small lean a still operator kept none.)
/// </summary>
public sealed class OperatorRig
{
	private static readonly string[] Names = { "Hips", "Spine", "Spine1", "LeftArm", "LeftForeArm", "LeftHand", "LeftHand_Attach", "RightArm", "RightForeArm", "RightHand", "RightHand_Attach" };

	public readonly OperatorArm Left, Right;
	public readonly Transform Hips;
	private readonly Transform spine, chest;
	private Quaternion leaned;

	private OperatorRig(Transform hips, Transform spine, Transform chest, OperatorArm left, OperatorArm right) =>
		(Hips, this.spine, this.chest, Left, Right) = (hips, spine, chest, left, right);

	/// <summary>A player's rig, or null when the player's model lacks one of its bones.</summary>
	public static OperatorRig? Of(Player player, ElbowHint elbows)
	{
		GameObject? visual = GameAccess.Visual(player);
		Transform root = visual != null ? visual.transform : player.transform;
		Transform[] b = new Transform[Names.Length];
		for (int i = 0; i < Names.Length; i++)
		{
			Transform? bone = Find(root, Names[i]);
			if (bone == null)
			{
				return null;
			}
			b[i] = bone;
		}
		OperatorArm left = new(b[3], b[4], b[5], b[6], -1f, elbows), right = new(b[7], b[8], b[9], b[10], 1f, elbows);
		return new OperatorRig(b[0], b[1], b[2], left, right);
	}

	/// <summary>The upper body leaned forward by `degrees` about the body's right, half at the waist, half at the chest.</summary>
	public void Lean(float degrees, Transform body)
	{
		if (Mathf.Abs(degrees) < 0.01f)
		{
			leaned = default;
			return;
		}
		if (spine.localRotation.Equals(leaned))
		{
			return;   // the animator did not pose this frame: the lean is still on
		}
		Quaternion half = Quaternion.AngleAxis(degrees * 0.5f, body.right);
		spine.rotation = half * spine.rotation;
		chest.rotation = half * chest.rotation;
		leaned = spine.localRotation;
	}

	private static Transform? Find(Transform root, string name)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
		{
			if (child.name == name)
			{
				return child;
			}
		}
		return null;
	}
}
