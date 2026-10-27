using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// What a view's ray is on, for the weapon (<see cref="WeaponAim"/> and <see cref="WeaponLay"/>, which lay it so its shot
/// lands there) and for the view itself (<see cref="ViewTilt"/>, which never looks where the weapon cannot reach): the
/// first solid thing along the ray from just past the weapon's pivot, passing through the weapon itself and the body it
/// stands on, out to <see cref="WeaponArcs.Reach"/>, short of the sea with <see cref="WeaponArcs.StopAtSea"/>; with
/// nothing there, the ray's far end (<see cref="Sight.Hit"/> false: open sky, or sea past the reach).
/// </summary>
internal static class WeaponSight
{
	private static readonly string[] Layers = { "Default", "static_solid", "Default_small", "piece", "terrain", "vehicle", "character", "character_net", "character_ghost" };
	private static readonly RaycastHit[] hits = new RaycastHit[32];
	private static int mask;

	/// <summary>What the ray from `eye` along `ahead` (a unit way) is on, past the weapon.</summary>
	public static Sight Target(MannedWeapon weapon, WeaponArcs arcs, Vector3 eye, Vector3 ahead)
	{
		Vector3 pivot = weapon.AimPivot;
		Vector3 from = eye + ahead * Mathf.Max(0f, Vector3.Dot(pivot - eye, ahead) + 1f);
		float nearest = Nearest(weapon, from, ahead, arcs.Reach);
		Vector3 target = from + ahead * nearest;
		bool hit = nearest < arcs.Reach;
		if (arcs.StopAtSea && AtSea(from, ahead, ref target))
		{
			hit = true;
		}
		return new Sight(target, hit, eye, ahead);
	}

	/// <summary>The view's camera, or the operator's eye without one: where the crosshair's ray starts and its way.</summary>
	public static Sight Crosshair(MannedWeapon weapon, Player player)
	{
		Transform view = GameCamera.instance != null ? GameCamera.instance.transform : player.m_eye;
		return Target(weapon, weapon.Design.Aim, view.position, view.forward);
	}

	/// <summary>How far the ray goes to the first solid thing on it that is neither the weapon nor the body it stands on.</summary>
	private static float Nearest(MannedWeapon weapon, Vector3 from, Vector3 ahead, float reach)
	{
		mask = mask != 0 ? mask : LayerMask.GetMask(Layers);
		int count = Physics.RaycastNonAlloc(from, ahead, hits, reach, mask);
		Rigidbody? carrier = weapon.Carrier;
		float nearest = reach;
		for (int i = 0; i < count; i++)
		{
			Collider hit = hits[i].collider;
			if (hits[i].distance < nearest && hit != null && !Own(weapon, carrier, hit))
			{
				nearest = hits[i].distance;
			}
		}
		return nearest;
	}

	private static bool Own(MannedWeapon weapon, Rigidbody? carrier, Collider hit) =>
		(carrier != null && hit.attachedRigidbody == carrier) || hit.transform.IsChildOf(weapon.transform);

	/// <summary>The sea's surface along the ray, when the ray goes down to it before the target; true then.</summary>
	private static bool AtSea(Vector3 from, Vector3 ahead, ref Vector3 target)
	{
		float sea = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
		if (ahead.y >= -0.01f || from.y <= sea)
		{
			return false;
		}
		float down = (sea - from.y) / ahead.y;
		if (down >= (target - from).magnitude)
		{
			return false;
		}
		target = from + ahead * down;
		return true;
	}
}

/// <summary>
/// What the crosshair's ray is on (<see cref="Point"/>, in the world; with nothing, <see cref="Hit"/> false and the ray's
/// far end), and the ray itself (from <see cref="Eye"/> along <see cref="Ahead"/>): taken as a trigger is pulled for a
/// shot that leaves later (<see cref="WeaponAim.Mark"/>, <see cref="WeaponShot.Way"/>).
/// </summary>
public readonly struct Sight
{
	public Sight(Vector3 point, bool hit, Vector3 eye, Vector3 ahead) => (Point, Hit, Eye, Ahead) = (point, hit, eye, ahead);

	public Vector3 Point { get; }

	public bool Hit { get; }

	public Vector3 Eye { get; }

	public Vector3 Ahead { get; }
}
