using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// A player brought quickly to a spot at a weapon, on his own machine, each physics step until he is there (taking hold
/// of a weapon, a cart's handles): about a third of a second for the usual few metres (<see cref="Duration"/>), eased in
/// and out, round the weapon's centre rather than through it (angle and distance about the centre eased together), his
/// feet ending on the floor at the spot (<see cref="OperatorReach.Floor"/>). Kept in the weapon's own space, so a spot
/// that moves with it (a weapon turning, a moving deck) is followed. His body is placed, not walked: what could stop him
/// was looked for first (<see cref="OperatorReach.Why"/>).
/// </summary>
public sealed class QuickStep
{
	private Transform? space;
	private Vector3 from, centre, lift;
	private float took, time;

	/// <summary>Whether he is still being brought there.</summary>
	public bool Moving => space != null;

	/// <summary>Seconds a step of so many metres takes: 0.3 for 3 m, between 0.15 and 0.45.</summary>
	public static float Duration(float distance) => Mathf.Clamp(0.12f + distance * 0.06f, 0.15f, 0.45f);

	/// <summary>Begins bringing the player to `spot` round `centre` (in the world), in `weapon`'s space.</summary>
	public void Begin(Player player, Vector3 spot, Vector3 centre, Transform weapon)
	{
		space = weapon;
		from = weapon.InverseTransformPoint(player.transform.position);
		this.centre = weapon.InverseTransformPoint(centre);
		lift = weapon.InverseTransformPoint(OperatorReach.Floor(spot, weapon, player)) - weapon.InverseTransformPoint(spot);
		(took, time) = (0f, Duration(Vector3.Distance(player.transform.position, spot)));
	}

	/// <summary>One physics step: the player placed further along his way to `spot` as it is now; false once he is there (or none is under way).</summary>
	public bool Step(Player player, Vector3 spot, float dt)
	{
		if (space == null)
		{
			return false;
		}
		took += dt;
		float t = Mathf.Clamp01(took / time);
		Vector3 to = space.InverseTransformPoint(spot) + lift;
		Place(player, space.TransformPoint(Along(from, to, centre, Mathf.SmoothStep(0f, 1f, t))));
		if (t >= 1f)
		{
			space = null;
		}
		return true;
	}

	/// <summary>Stops bringing him (he let go, or was let go of).</summary>
	public void Stop() => space = null;

	/// <summary>The point `t` of the way from `a` to `b` round `centre` (one space, its up the space's): angle and distance about the centre eased together, height straight.</summary>
	public static Vector3 Along(Vector3 a, Vector3 b, Vector3 centre, float t)
	{
		Vector2 p = new Vector2(a.x - centre.x, a.z - centre.z), q = new Vector2(b.x - centre.x, b.z - centre.z);
		if (p.magnitude < 0.05f || q.magnitude < 0.05f)
		{
			return Vector3.Lerp(a, b, t);
		}
		float from = Mathf.Atan2(p.x, p.y) * Mathf.Rad2Deg, to = Mathf.Atan2(q.x, q.y) * Mathf.Rad2Deg;
		float angle = (from + Mathf.DeltaAngle(from, to) * t) * Mathf.Deg2Rad, radius = Mathf.Lerp(p.magnitude, q.magnitude, t);
		return new Vector3(centre.x + Mathf.Sin(angle) * radius, Mathf.Lerp(a.y, b.y, t), centre.z + Mathf.Cos(angle) * radius);
	}

	/// <summary>His body put there, still (only his own machine moves his body).</summary>
	private static void Place(Player player, Vector3 at)
	{
		player.transform.position = at;
		Rigidbody? body = player.GetComponent<Rigidbody>();
		if (body != null)
		{
			body.position = at;
			body.linearVelocity = Vector3.zero;
		}
	}
}
