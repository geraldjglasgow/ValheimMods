using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// Taking and letting go of a weapon, on every peer (a dedicated server answers too), as the game's ship helm does. The
/// use key asks the weapon's owner (<see cref="WeaponKeys.Request"/>); the owner grants it when no valid operator holds
/// it and the weapon is <see cref="MannedWeapon.Free"/>, writes the operator to the ZDO and hands the ZDO over to him
/// (sent first, so his machine has it), and answers. Letting go on a machine that is not the owner tells the owner
/// (<see cref="WeaponKeys.Release"/>), which finishes a step under way and frees the weapon.
/// </summary>
internal static class WeaponHold
{
	/// <summary>The weapon's calls on its view.</summary>
	public static void Register(MannedWeapon weapon)
	{
		WeaponKeys keys = weapon.Design.Keys;
		weapon.Net.Register<long>(keys.Request, (sender, playerID) => Request(weapon, sender, playerID));
		weapon.Net.Register<long>(keys.Release, (sender, playerID) => Release(weapon, playerID));
		weapon.Net.Register<bool>(keys.Answer, (sender, granted) => Answer(weapon, granted));
	}

	/// <summary>
	/// The use key: lets go of a weapon held here, else asks for it from its stand point (from behind, when it must be),
	/// unless he sits, steers or mans something else, or the weapon says why not (<see cref="MannedWeapon.Refusal"/>).
	/// </summary>
	public static bool Interact(MannedWeapon weapon, Humanoid character, bool hold)
	{
		if (hold || !WeaponPatches.Installed || !weapon.Net.IsValid() || weapon.Frame == null || character is not Player player)
		{
			return false;
		}
		if (weapon.HeldBy(player))
		{
			player.StopDoodadControl();
			return true;
		}
		if (player.IsAttached() || player.GetDoodadController() != null || Vector3.Distance(player.transform.position, weapon.Frame.Stand.position) > weapon.Design.UseRange)
		{
			return false;
		}
		string? why = Refused(weapon, player);
		if (why != null)
		{
			player.Message(MessageHud.MessageType.Center, why);
			return false;
		}
		weapon.Net.InvokeRPC(weapon.Design.Keys.Request, player.GetPlayerID());
		return true;
	}

	/// <summary>Why the player may not ask for the weapon now: in front of one taken from behind, or the weapon's own reason; null: he may.</summary>
	private static string? Refused(MannedWeapon weapon, Player player)
	{
		if (weapon.Design.Behind is float behind && weapon.Frame!.Yaw.InverseTransformPoint(player.transform.position).z > -behind)
		{
			return weapon.Design.Words.Behind;
		}
		return weapon.Refusal(player);
	}

	/// <summary>The owner: the weapon goes to the asker when no valid operator holds it, the ZDO with it.</summary>
	private static void Request(MannedWeapon weapon, long sender, long playerID)
	{
		if (!weapon.Net.IsOwner())
		{
			return;
		}
		ZDO zdo = weapon.Net.GetZDO();
		WeaponKeys keys = weapon.Design.Keys;
		long user = zdo.GetLong(keys.User, 0L);
		if ((user != playerID && ValidUser(weapon, user)) || !weapon.Free)
		{
			weapon.Net.InvokeRPC(sender, keys.Answer, false);
			return;
		}
		zdo.Set(keys.User, playerID);
		ZDOMan.instance.ForceSendZDO(sender, zdo.m_uid);
		zdo.SetOwner(sender);
		weapon.Net.InvokeRPC(sender, keys.Answer, true);
	}

	private static void Answer(MannedWeapon weapon, bool granted)
	{
		Player? player = Player.m_localPlayer;
		if (player == null)
		{
			return;
		}
		if (!granted)
		{
			player.Message(MessageHud.MessageType.Center, weapon.Design.Words.InUse);
			return;
		}
		weapon.Take(player);
	}

	/// <summary>The owner (whoever has the ZDO now): the operator lets go.</summary>
	private static void Release(MannedWeapon weapon, long playerID)
	{
		if (weapon.Net.IsOwner() && weapon.Net.GetZDO().GetLong(weapon.Design.Keys.User, 0L) == playerID)
		{
			WeaponReload.LetGo(weapon);
		}
	}

	/// <summary>
	/// The operator's body and the weapon pass through each other while he holds it, as the game does for a chair or a
	/// ship's helm (his capsule rubbing on it slowed every side-step). Local: only his own machine moves his body.
	/// </summary>
	public static void Collide(MannedWeapon weapon, Player player, bool collide)
	{
		CapsuleCollider? own = player.GetCollider();
		if (own == null)
		{
			return;
		}
		foreach (Collider part in weapon.GetComponentsInChildren<Collider>(true))
		{
			Physics.IgnoreCollision(own, part, !collide);
		}
	}

	/// <summary>An operator who is still in the world, alive and at the weapon.</summary>
	public static bool ValidUser(MannedWeapon weapon, long user)
	{
		Player? player = user != 0L ? Player.GetPlayer(user) : null;
		return player != null && !player.IsDead() && Vector3.Distance(player.transform.position, weapon.transform.position) < weapon.Design.HolderRange;
	}
}
