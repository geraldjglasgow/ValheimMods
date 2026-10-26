using HarmonyLib;
using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// A weapon's shot, on the operator's machine, the way the game's ballista launches its bolts: a networked copy of the
/// projectile prefab, flown by this peer and seen by every other through its ZDO, with the operator as the attacker
/// (his hit, his skill rises, his friends spared). Optionally it passes through what it was fired from
/// (<see cref="InstallPassThrough"/>): the game's projectile tests its first step from half a step behind where it
/// starts, which from a weapon on a ship or a carriage runs back through the weapon and its body.
/// </summary>
public static class WeaponShot
{
	private static bool installed;

	/// <summary>
	/// Launches `prefab` from `at`, turned `rotation`, at `velocity` (a weapon on a moving body adds the body's own), with
	/// `hit` and the ammunition item it carries; it passes through `body` and everything under `weapon` once
	/// <see cref="InstallPassThrough"/> is in (null: nothing). Returns the projectile.
	/// </summary>
	public static GameObject Launch(GameObject prefab, Vector3 at, Quaternion rotation, Vector3 velocity, Character attacker, HitData hit,
		ItemDrop.ItemData? ammo, float noise, Rigidbody? body = null, Transform? weapon = null)
	{
		GameObject shot = Object.Instantiate(prefab, at, rotation);
		if (body != null || weapon != null)
		{
			FiredFrom from = shot.AddComponent<FiredFrom>();
			(from.Body, from.Weapon) = (body, weapon);
		}
		shot.GetComponent<IProjectile>()?.Setup(attacker, velocity, noise, hit, null, ammo);
		return shot;
	}

	/// <summary>A hit from an ammunition item's numbers, as the game's ballista makes one: its damage, push, stagger, tool tier and skill.</summary>
	public static HitData Hit(ItemDrop.ItemData item)
	{
		ItemDrop.ItemData.SharedData shared = item.m_shared;
		HitData hit = new()
		{
			m_toolTier = (short)shared.m_toolTier,
			m_pushForce = shared.m_attackForce,
			m_backstabBonus = shared.m_backstabBonus,
			m_staggerMultiplier = shared.m_attack.m_staggerMultiplier,
			m_blockable = shared.m_blockable,
			m_dodgeable = shared.m_dodgeable,
			m_skill = shared.m_skillType,
			m_hitType = HitData.HitType.PlayerHit,
			m_itemWorldLevel = (byte)Game.m_worldLevel,
		};
		hit.m_damage.Add(item.GetDamage());
		return hit;
	}

	/// <summary>A prefix on <c>Projectile.OnHit</c> (only the flying peer runs it) letting a marked shot through what it was fired from; once per merged copy.</summary>
	public static void InstallPassThrough(Harmony harmony)
	{
		if (installed || AccessTools.Method(typeof(Projectile), "OnHit") == null)
		{
			return;
		}
		installed = true;
		harmony.Patch(AccessTools.Method(typeof(Projectile), "OnHit"), prefix: new HarmonyMethod(typeof(WeaponShot), nameof(Through)));
	}

	private static bool Through(Projectile __instance, Collider collider)
	{
		FiredFrom? from = collider != null ? __instance.GetComponent<FiredFrom>() : null;
		return from == null || !from.Passes(collider!);
	}
}

/// <summary>What a shot was fired from (a body, a weapon), on its projectile, so it passes through them; never sent.</summary>
public sealed class FiredFrom : MonoBehaviour
{
	public Rigidbody? Body;
	public Transform? Weapon;

	public bool Passes(Collider collider) =>
		(Body != null && collider.attachedRigidbody == Body) || (Weapon != null && collider.transform.IsChildOf(Weapon));
}
