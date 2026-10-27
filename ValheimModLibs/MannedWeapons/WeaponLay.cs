using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// Lays a weapon so its shot lands on what the crosshair is on (<see cref="Sight"/>). The lay (yaw right of where the
/// weapon was placed, pitch up from the plane it stands on, degrees) is found from where the shot leaves
/// (<see cref="MannedWeapon.Flight"/>), worked out again for each lay tried, so a muzzle ahead of the pivots or beside
/// them is counted: the shot goes from there to the target, never parallel to the view (the game's camera looks over
/// the operator's right shoulder, its ray 0.3 m to the right of his eye, and he stands beside the weapon's line). Its way
/// is a straight line, or the low arc for the shot's speed and gravity, the carrying body's motion led off
/// (<see cref="ShotFlight.Carried"/>); past its reach, 45 degrees up (the arcs then hold the weapon at the nearest they
/// can). With nothing on the crosshair (open sky, or sea past the reach) the shot keeps the elevation the crosshair's ray
/// has from where it leaves, turned onto that ray where it comes down to the sea. Measured in the frame the yaw part
/// turns in (its parent's), so a base tilted on a slope or a rolling deck is taken off; the pitch part hangs unturned on
/// the yaw part, tilting about its own x, as the library turns it.
/// </summary>
internal static class WeaponLay
{
	/// <summary>Rounds of working out where the shot leaves for the lay found; of leading off the carrying body's motion.</summary>
	private const int Rounds = 3, Leads = 3;

	/// <summary>Closer than this (along the ground) to where the shot leaves, a target's bearing is unsteady: the view's own way is taken.</summary>
	private const float Close = 1f;

	/// <summary>The lay (not yet held to the arcs) whose shot lands on the sight, starting from the lay `yaw`, `pitch`.</summary>
	public static (float Yaw, float Pitch) Solve(MannedWeapon weapon, in Sight sight, float yaw, float pitch)
	{
		ShotFlight flight = weapon.Flight;
		for (int i = 0; i < Rounds; i++)
		{
			Vector3 from = Launch(weapon, flight, yaw, pitch);
			(yaw, pitch) = Angles(weapon, Way(flight, from, sight, Carried(weapon, flight, from)));
		}
		return (yaw, pitch);
	}

	/// <summary>The way a shot leaving `from` goes to land on the sight, the carrying body's motion `carried` led off.</summary>
	public static Vector3 Way(in ShotFlight flight, Vector3 from, in Sight sight, Vector3 carried)
	{
		Vector3 run = sight.Point - from;
		if (new Vector2(run.x, run.z).magnitude < Close)
		{
			return sight.Ahead;
		}
		return sight.Hit ? Reach(flight, run, carried) : Open(flight, from, sight);
	}

	/// <summary>Where the shot leaves with the weapon laid at `yaw`, `pitch`.</summary>
	public static Vector3 Launch(MannedWeapon weapon, in ShotFlight flight, float yaw, float pitch)
	{
		Transform turning = weapon.Frame!.Yaw, tilting = weapon.Frame.Pitch;
		Quaternion turned = Base(weapon) * Quaternion.Euler(0f, yaw, 0f);
		Vector3 trunnions = turning.position + turned * (Quaternion.Inverse(turning.rotation) * (tilting.position - turning.position));
		return trunnions + turned * Quaternion.Euler(-pitch, 0f, 0f) * flight.From;
	}

	/// <summary>The world way the shot leaves along with the weapon laid at `yaw`, `pitch`.</summary>
	public static Vector3 Direction(MannedWeapon weapon, float yaw, float pitch) => Base(weapon) * Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;

	/// <summary>The lay that points the shot along a world way.</summary>
	public static (float Yaw, float Pitch) Angles(MannedWeapon weapon, Vector3 way)
	{
		Vector3 local = Quaternion.Inverse(Base(weapon)) * way.normalized;
		return (Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg);
	}

	/// <summary>
	/// A lay held within the arcs, the closest the weapon can do: the pitch held to its stops, and then the yaw that keeps
	/// the shot's bearing at the held pitch (on a tilted base the bearing swings as the weapon tilts), held to its own.
	/// </summary>
	public static (float Yaw, float Pitch) Hold(MannedWeapon weapon, WeaponArcs arcs, float yaw, float pitch)
	{
		float held = Mathf.Clamp(pitch, arcs.PitchDown, arcs.PitchUp);
		if (held != pitch)
		{
			yaw = Upright(weapon, Direction(weapon, yaw, pitch), held, yaw);
		}
		return (arcs.ClampYaw(yaw), held);
	}

	/// <summary>The yaw that at `pitch` keeps the shot in the upright plane of the world way `way`, the one nearest `near`.</summary>
	private static float Upright(MannedWeapon weapon, Vector3 way, float pitch, float near)
	{
		Vector3 across = Quaternion.Inverse(Base(weapon)) * new Vector3(way.z, 0f, -way.x).normalized;
		float a = pitch * Mathf.Deg2Rad, sin = across.x * Mathf.Cos(a), cos = across.z * Mathf.Cos(a), size = Mathf.Sqrt(sin * sin + cos * cos);
		if (size < 1e-4f)
		{
			return near;
		}
		float turn = Mathf.Atan2(cos, sin), lift = Mathf.Asin(Mathf.Clamp(-across.y * Mathf.Sin(a) / size, -1f, 1f));
		float one = Mathf.DeltaAngle(0f, (lift - turn) * Mathf.Rad2Deg), other = Mathf.DeltaAngle(0f, (Mathf.PI - lift - turn) * Mathf.Rad2Deg);
		return Mathf.Abs(Mathf.DeltaAngle(near, one)) <= Mathf.Abs(Mathf.DeltaAngle(near, other)) ? one : other;
	}

	/// <summary>Where a shot leaving `from` along `way` is once `flat` metres out along the ground: on its arc when it falls and `arc` is true, else on its line.</summary>
	public static Vector3 Along(in ShotFlight flight, Vector3 from, Vector3 way, float flat, bool arc)
	{
		float level = Mathf.Max(0.01f, new Vector2(way.x, way.z).magnitude);
		if (!arc || !flight.Falls)
		{
			return from + way * (flat / level);
		}
		float time = flat / (flight.Speed * level);
		return from + way * (flight.Speed * time) + Vector3.down * (0.5f * flight.Gravity * time * time + Extra(flight, time));
	}

	/// <summary>The carrying body's motion at `from`, when the shot carries it on.</summary>
	public static Vector3 Carried(MannedWeapon weapon, in ShotFlight flight, Vector3 from)
	{
		Rigidbody? body = flight.Carried && flight.Speed > 0.01f ? weapon.Carrier : null;
		return body != null ? body.GetPointVelocity(from) : Vector3.zero;
	}

	/// <summary>The rotation of the frame the yaw part turns in (its parent's).</summary>
	private static Quaternion Base(MannedWeapon weapon) => weapon.Frame!.Yaw.parent != null ? weapon.Frame.Yaw.parent.rotation : Quaternion.identity;

	/// <summary>
	/// The way that brings the shot to a point `run` away from where it leaves: the carrying body's motion led off, and
	/// aimed the little higher the game's projectile falls past the curve (<see cref="Extra"/>).
	/// </summary>
	private static Vector3 Reach(in ShotFlight flight, Vector3 run, Vector3 carried)
	{
		if (flight.Speed <= 0.01f)
		{
			return run.normalized;
		}
		float time = run.magnitude / flight.Speed;
		Vector3 way = run.normalized;
		for (int i = 0, rounds = carried.sqrMagnitude > 1e-6f || flight.Falls ? Leads : 1; i < rounds; i++)
		{
			way = Arc(flight, run - carried * time + Vector3.up * Extra(flight, time), out time);
		}
		return way;
	}

	/// <summary>How much further than the curve the game's projectile has fallen after `time`: it adds a step's gravity to its speed before each step's move, half a step's fall a second more.</summary>
	private static float Extra(in ShotFlight flight, float time) => flight.Falls ? 0.5f * flight.Gravity * time * Time.fixedDeltaTime : 0f;

	/// <summary>The low arc's way to a point `run` away at the shot's speed and gravity, and its time there; past its reach, 45 degrees up.</summary>
	private static Vector3 Arc(in ShotFlight flight, Vector3 run, out float time)
	{
		Vector3 flat = new(run.x, 0f, run.z);
		float x = flat.magnitude, v = flight.Speed, g = flight.Gravity;
		if (x < 0.01f || g <= 0f)
		{
			time = run.magnitude / v;
			return run.normalized;
		}
		float v2 = v * v, root = v2 * v2 - g * (g * x * x + 2f * run.y * v2);
		float angle = root >= 0f ? Mathf.Atan2(v2 - Mathf.Sqrt(root), g * x) : 0.25f * Mathf.PI;
		time = x / (v * Mathf.Cos(angle));
		return flat / x * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
	}

	/// <summary>Nothing on the crosshair: the elevation its ray's far end has from `from`, turned onto the ray where the shot comes down to the sea.</summary>
	private static Vector3 Open(in ShotFlight flight, Vector3 from, in Sight sight)
	{
		Vector3 far = sight.Point - from;
		float flat = new Vector2(far.x, far.z).magnitude, angle = Mathf.Atan2(far.y, flat);
		Vector3 on = OnRay(sight, from, Down(flight, from.y, angle, flat)) - from;
		Vector3 level = new Vector3(on.x, 0f, on.z).normalized;
		return level * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
	}

	/// <summary>How far out along the ground a shot leaving at `height`, `angle` up, comes down to the sea; `far` when it never does.</summary>
	private static float Down(in ShotFlight flight, float height, float angle, float far)
	{
		float above = height - (ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f), cos = Mathf.Cos(angle);
		if (!flight.Falls || above <= 0f || cos < 0.01f)
		{
			return far;
		}
		float k = flight.Gravity / (2f * flight.Speed * flight.Speed * cos * cos), tan = Mathf.Tan(angle);
		return (tan + Mathf.Sqrt(tan * tan + 4f * k * above)) / (2f * k);
	}

	/// <summary>The point on the crosshair's ray `flat` metres out along the ground from `from`; its far end when the ray never gets so far.</summary>
	private static Vector3 OnRay(in Sight sight, Vector3 from, float flat)
	{
		float qx = sight.Eye.x - from.x, qz = sight.Eye.z - from.z, bx = sight.Ahead.x, bz = sight.Ahead.z;
		float bb = bx * bx + bz * bz, qb = qx * bx + qz * bz, rest = qb * qb - bb * (qx * qx + qz * qz - flat * flat);
		if (bb < 1e-6f || rest < 0f)
		{
			return sight.Point;
		}
		return sight.Eye + sight.Ahead * ((-qb + Mathf.Sqrt(rest)) / bb);
	}
}
