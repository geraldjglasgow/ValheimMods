using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The operator's aim, on his own machine, each drawn frame: the yaw follows the view's own (which
/// <see cref="ViewLimit"/> holds to the weapon's arc and pace, so it stops at the arc's end and as the mouse stops), the
/// pitch lays the weapon on what the crosshair is on (measured from the plane the weapon stands on, so a base tilted on a
/// slope or a rolling deck is taken off), both within the arcs and at the weapon's rates (<see cref="WeaponArcs"/>). It
/// goes to the ZDO when it has moved a little; every other peer eases toward that.
/// </summary>
public sealed class WeaponAim
{
	private static readonly string[] Layers = { "Default", "static_solid", "Default_small", "piece", "terrain", "vehicle", "character", "character_net", "character_ghost" };
	private static readonly RaycastHit[] hits = new RaycastHit[32];
	private static int mask;

	/// <summary>The operator's aim, degrees from where the weapon was placed (yaw to the right, pitch up), ahead of what the ZDO has sent.</summary>
	public float Yaw { get; private set; }

	public float Pitch { get; private set; }

	internal void Begin(in WeaponState s) => (Yaw, Pitch) = (s.Yaw, s.Pitch);

	internal void Step(MannedWeapon weapon, Player player, float dt)
	{
		if (weapon.Frame == null || !weapon.Net.IsValid())
		{
			return;
		}
		WeaponArcs arcs = weapon.Design.Aim;
		float yaw = arcs.ClampYaw(Mathf.DeltaAngle(weapon.transform.eulerAngles.y, GameAccess.LookYaw(player).eulerAngles.y));
		Yaw = arcs.Turn(Yaw, yaw, arcs.TurnRate * dt);
		Pitch = Mathf.MoveTowards(Pitch, Elevation(weapon, arcs), arcs.TiltRate * dt);
		if (weapon.Net.IsOwner())
		{
			Write(weapon.Net.GetZDO(), weapon.Design.Keys.Yaw, Yaw, arcs.WriteStep);
			Write(weapon.Net.GetZDO(), weapon.Design.Keys.Pitch, Pitch, arcs.WriteStep);
		}
	}

	private static void Write(ZDO zdo, int key, float value, float step)
	{
		if (Mathf.Abs(zdo.GetFloat(key) - value) > step)
		{
			zdo.Set(key, value);
		}
	}

	/// <summary>
	/// The pitch that lays the weapon on what the crosshair is on, within its tilt, measured from the plane the weapon
	/// stands on (its pitch part tilts on its base, so a base tilted on a slope or a rolling deck is taken off).
	/// </summary>
	private float Elevation(MannedWeapon weapon, WeaponArcs arcs)
	{
		if (GameCamera.instance == null)
		{
			return Pitch;
		}
		Transform camera = GameCamera.instance.transform;
		Vector3 pivot = weapon.AimPivot;
		Vector3 from = camera.position + camera.forward * Mathf.Max(0f, Vector3.Dot(pivot - camera.position, camera.forward) + 1f);
		Vector3 run = weapon.transform.InverseTransformDirection(Target(weapon, arcs, from, camera.forward) - pivot);
		float pitch = Mathf.Atan2(run.y, new Vector2(run.x, run.z).magnitude) * Mathf.Rad2Deg;
		return Mathf.Clamp(pitch, arcs.PitchDown, arcs.PitchUp);
	}

	/// <summary>The first solid thing along the view from just past the weapon (not the body it stands on), or far ahead; short of the sea when asked.</summary>
	private static Vector3 Target(MannedWeapon weapon, WeaponArcs arcs, Vector3 from, Vector3 ahead)
	{
		mask = mask != 0 ? mask : LayerMask.GetMask(Layers);
		Rigidbody? carrier = weapon.Carrier;
		Vector3 target = carrier == null
			? Physics.Raycast(from, ahead, out RaycastHit hit, arcs.Reach, mask) ? hit.point : from + ahead * arcs.Reach
			: from + ahead * Past(carrier, from, ahead, arcs.Reach);
		return arcs.StopAtSea ? AtSea(from, ahead, target) : target;
	}

	/// <summary>How far the view's ray goes to the first solid thing on it, passing through the body the weapon stands on.</summary>
	private static float Past(Rigidbody carrier, Vector3 from, Vector3 ahead, float reach)
	{
		int count = Physics.RaycastNonAlloc(from, ahead, hits, reach, mask);
		float nearest = reach;
		for (int i = 0; i < count; i++)
		{
			if (hits[i].distance < nearest && hits[i].collider.attachedRigidbody != carrier)
			{
				nearest = hits[i].distance;
			}
		}
		return nearest;
	}

	/// <summary>The target, or the sea's surface along the ray when the ray goes down to it first.</summary>
	private static Vector3 AtSea(Vector3 from, Vector3 ahead, Vector3 target)
	{
		float sea = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
		if (ahead.y >= -0.01f || from.y <= sea)
		{
			return target;
		}
		float down = (sea - from.y) / ahead.y;
		return down < (target - from).magnitude ? from + ahead * down : target;
	}
}
