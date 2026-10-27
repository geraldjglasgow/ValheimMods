using UnityEngine;

namespace MannedWeapons;

/// <summary>Why a player cannot be brought to a spot from where he stands; <see cref="None"/>: he can.</summary>
public enum ReachBlock
{
	None,

	/// <summary>He is swimming.</summary>
	Water,

	/// <summary>The spot's floor is more than <see cref="OperatorReach.Climb"/> above his feet, or more than <see cref="OperatorReach.Drop"/> below them.</summary>
	Climb,

	/// <summary>Something solid is in the way round.</summary>
	Wall,
}

/// <summary>
/// Whether a player can be brought quickly to a spot at a weapon (its stand point, a cart's handles; <see cref="QuickStep"/>)
/// from where he stands, and the floor his feet go to there. Nothing but what truly stops him counts: swimming, a floor
/// more than <see cref="Climb"/> above his feet (or a drop of more than <see cref="Drop"/>, which the game would count as
/// a fall), or anything solid on his way round the weapon's centre at knee or chest height (a wall, a mast, a rock; the
/// weapon itself never, as his body passes through it while he holds it).
/// </summary>
public static class OperatorReach
{
	/// <summary>The highest floor above his feet he is brought up to, and the lowest below them he is brought down to, metres.</summary>
	public const float Climb = 1.2f, Drop = 3f;

	private const int Legs = 6;
	private const float Over = 1f, Under = 2.5f;
	private static readonly float[] Heights = { 0.5f, 1.3f };
	private static readonly string[] Solid = { "Default", "static_solid", "Default_small", "piece", "terrain", "vehicle" };
	private static readonly RaycastHit[] hits = new RaycastHit[16];
	private static int mask;

	/// <summary>What stops the player being brought to `spot` round `centre` (both in the world), `weapon` passing.</summary>
	public static ReachBlock Why(Player player, Vector3 spot, Vector3 centre, Transform weapon)
	{
		if (player.IsSwimming())
		{
			return ReachBlock.Water;
		}
		Vector3 feet = Floor(spot, weapon, player);
		float rise = feet.y - player.transform.position.y;
		if (rise > Climb || -rise > Drop)
		{
			return ReachBlock.Climb;
		}
		return Clear(player, feet, centre, weapon) ? ReachBlock.None : ReachBlock.Wall;
	}

	/// <summary>The floor under a spot: the first solid thing below it from a little over it that is neither the weapon nor the player; the spot itself over none.</summary>
	public static Vector3 Floor(Vector3 spot, Transform weapon, Player player)
	{
		Vector3 from = spot + Vector3.up * Over;
		float down = Nearest(from, Vector3.down, Over + Under, weapon, player);
		return down < Over + Under ? from + Vector3.down * down : spot;
	}

	/// <summary>Whether his way round, as <see cref="QuickStep"/> takes it in the weapon's space, is clear at knee and chest height.</summary>
	private static bool Clear(Player player, Vector3 feet, Vector3 centre, Transform weapon)
	{
		Vector3 a = weapon.InverseTransformPoint(player.transform.position), b = weapon.InverseTransformPoint(feet), c = weapon.InverseTransformPoint(centre);
		for (int i = 0; i < Legs; i++)
		{
			Vector3 p = weapon.TransformPoint(QuickStep.Along(a, b, c, (float)i / Legs)), q = weapon.TransformPoint(QuickStep.Along(a, b, c, (float)(i + 1) / Legs));
			foreach (float height in Heights)
			{
				Vector3 lift = Vector3.up * height, leg = q - p;
				if (leg.sqrMagnitude > 1e-6f && Nearest(p + lift, leg.normalized, leg.magnitude, weapon, player) < leg.magnitude)
				{
					return false;
				}
			}
		}
		return true;
	}

	/// <summary>How far along a ray the first solid thing is that is neither the weapon nor the player; `reach` for none.</summary>
	private static float Nearest(Vector3 from, Vector3 way, float reach, Transform weapon, Player player)
	{
		mask = mask != 0 ? mask : LayerMask.GetMask(Solid);
		int count = Physics.RaycastNonAlloc(from, way, hits, reach, mask, QueryTriggerInteraction.Ignore);
		float nearest = reach;
		for (int i = 0; i < count; i++)
		{
			Collider hit = hits[i].collider;
			if (hits[i].distance < nearest && hit != null && !hit.transform.IsChildOf(weapon) && !hit.transform.IsChildOf(player.transform))
			{
				nearest = hits[i].distance;
			}
		}
		return nearest;
	}
}
