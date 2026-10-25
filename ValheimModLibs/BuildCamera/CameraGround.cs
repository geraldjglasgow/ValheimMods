using UnityEngine;

namespace BuildCamera;

/// <summary>
/// Keeps the camera out of the ground and the water. A move is swept with a sphere about the size of the game camera's
/// own collision probe against the <c>terrain</c> layer (the ground, and caves built into it) and <c>static_solid</c>
/// (rocks, cliffs, dungeon walls); on a hit it stops just short and slides along the surface for the rest of the move.
/// Building pieces, trees and creatures are not in the mask, so the camera passes through walls into a house. Then,
/// every frame, it is lifted above the water's surface (waves included, as the game camera is) and, where the flight
/// asks, above the terrain's surface.
/// </summary>
internal static class CameraGround
{
	private const float Radius = 0.3f, Skin = 0.05f, MinMove = 1e-4f;

	/// <summary>The game camera's own least height above water (<c>GameCamera.m_minWaterDistance</c>), and the same above ground.</summary>
	private const float Clearance = 0.3f;

	private static int mask;

	private static int Mask => mask != 0 ? mask : (mask = LayerMask.GetMask("terrain", "static_solid"));

	public static Vector3 Move(Vector3 from, Vector3 to, CameraFlight flight)
	{
		if (!flight.Collide)
		{
			return to;
		}
		Vector3 stop = Sweep(from, to, out Vector3 normal);
		if (normal == Vector3.zero)
		{
			return stop;
		}
		Vector3 slide = Vector3.ProjectOnPlane(to - stop, normal);
		return Sweep(stop, stop + slide, out _);
	}

	/// <summary>The point, lifted to the floors the flight keeps: the water's surface, and the terrain's where asked.</summary>
	public static Vector3 Hold(Vector3 point, CameraFlight flight)
	{
		float floor = float.MinValue;
		if (flight.AboveWater)
		{
			floor = Floating.GetLiquidLevel(point) + Clearance;
		}
		if (flight.AboveGround && ZoneSystem.instance != null)
		{
			floor = Mathf.Max(floor, ZoneSystem.instance.GetGroundHeight(point) + Clearance);
		}
		point.y = Mathf.Max(point.y, floor);
		return point;
	}

	/// <summary>As far toward the target as the sphere goes; the normal of what it hit, or zero when nothing.</summary>
	private static Vector3 Sweep(Vector3 from, Vector3 to, out Vector3 normal)
	{
		normal = Vector3.zero;
		Vector3 delta = to - from;
		float distance = delta.magnitude;
		if (distance < MinMove)
		{
			return from;
		}
		Vector3 direction = delta / distance;
		if (!Physics.SphereCast(from, Radius, direction, out RaycastHit hit, distance + Skin, Mask, QueryTriggerInteraction.Ignore))
		{
			return to;
		}
		normal = hit.normal;
		return from + direction * Mathf.Max(0f, hit.distance - Skin);
	}
}
