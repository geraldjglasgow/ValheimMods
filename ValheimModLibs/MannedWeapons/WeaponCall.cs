namespace MannedWeapons;

/// <summary>
/// A shot without an operator, asked for by a player who does not man the weapon (a ship's captain firing his cannons
/// from the helm; <see cref="MannedWeapon.CallShot"/>), on every peer. The caller's machine asks the weapon's owner
/// (<see cref="WeaponKeys.Call"/>); the owner, when nobody mans it, it is free (<see cref="MannedWeapon.Free"/>) and it
/// is loaded and not mid-step, hands its ZDO to the caller (sent first), and answers (<see cref="WeaponKeys.Called"/>);
/// the caller's machine then fires it where it points once the ZDO is its own, as its operator's trigger would
/// (<see cref="MannedWeapon.ShootCalled"/>, written empty and counted: every peer's cue for its look and sound). Nothing
/// reloads it: that is still an operator's work.
/// <para>
/// A shot ordered whoever mans the weapon (<see cref="MannedWeapon.OrderShot"/>, <see cref="WeaponKeys.Order"/>) goes to
/// the owner too, which decides: a weapon its operator holds there (his machine owns it while he does) is fired by his
/// own trigger, where he has it laid (<see cref="WeaponReload.Ordered"/>), and reloads as his always does; any other is
/// a called shot as above. One machine decides and the shot writes the weapon empty at once, so an order, his own pull
/// and his letting go never fire it twice.
/// </para>
/// </summary>
internal static class WeaponCall
{
	/// <summary>The weapon's calls on its view.</summary>
	public static void Register(MannedWeapon weapon)
	{
		WeaponKeys keys = weapon.Design.Keys;
		weapon.Net.Register<long>(keys.Call, (sender, caller) => Asked(weapon, sender));
		weapon.Net.Register<bool>(keys.Called, (sender, granted) => Answer(weapon, granted));
		weapon.Net.Register<long>(keys.Order, (sender, caller) => Ordered(weapon, sender));
	}

	/// <summary>On the ordering machine: orders the shot, manned or not; false (nothing asked) when the weapon already looks empty or mid-step here.</summary>
	public static bool Order(MannedWeapon weapon, Player caller)
	{
		if (weapon.Frame == null || !weapon.Net.IsValid() || !Ready(weapon))
		{
			return false;
		}
		weapon.Net.InvokeRPC(weapon.Design.Keys.Order, caller.GetPlayerID());
		return true;
	}

	/// <summary>Loaded and not mid-step, whoever mans it.</summary>
	public static bool Ready(MannedWeapon weapon)
	{
		WeaponState s = weapon.State;
		return s.Loaded && !s.Busy;
	}

	/// <summary>The owner: fired here by the trigger of the operator who holds it; else a called shot.</summary>
	private static void Ordered(MannedWeapon weapon, long sender)
	{
		if (!weapon.Net.IsOwner())
		{
			return;
		}
		if (Player.m_localPlayer is Player holder && weapon.HeldBy(holder))
		{
			WeaponReload.Ordered(weapon, holder);
		}
		else
		{
			Asked(weapon, sender);
		}
	}

	/// <summary>On the caller's machine: asks for the shot; false (nothing asked) when the weapon already looks unfit here.</summary>
	public static bool Ask(MannedWeapon weapon, Player caller)
	{
		if (weapon.Frame == null || !weapon.Net.IsValid() || !Fit(weapon))
		{
			return false;
		}
		weapon.Net.InvokeRPC(weapon.Design.Keys.Call, caller.GetPlayerID());
		return true;
	}

	/// <summary>Loaded, not mid-step, and nobody valid mans it.</summary>
	public static bool Fit(MannedWeapon weapon)
	{
		WeaponState s = weapon.State;
		return s.Loaded && !s.Busy && !WeaponHold.ValidUser(weapon, s.User);
	}

	/// <summary>The owner: the ZDO to the caller when the weapon may fire, and the answer.</summary>
	private static void Asked(MannedWeapon weapon, long sender)
	{
		if (!weapon.Net.IsOwner())
		{
			return;
		}
		bool fit = weapon.Frame != null && Fit(weapon) && weapon.Free;
		ZDO zdo = weapon.Net.GetZDO();
		if (fit && zdo.GetOwner() != sender)
		{
			ZDOMan.instance.ForceSendZDO(sender, zdo.m_uid);
			zdo.SetOwner(sender);
		}
		weapon.Net.InvokeRPC(sender, weapon.Design.Keys.Called, fit);
	}

	/// <summary>The caller's machine, granted: the shot waits for the ZDO's ownership to arrive (it may come after the answer).</summary>
	private static void Answer(MannedWeapon weapon, bool granted)
	{
		if (granted)
		{
			weapon.calledAt = UnityEngine.Time.time;
		}
	}

	/// <summary>Each physics step on a weapon granted to this machine: fired once it owns the ZDO; given up after <see cref="Patience"/> seconds.</summary>
	public static void Tick(MannedWeapon weapon)
	{
		if (weapon.Net.IsValid() && weapon.Net.IsOwner() && Player.m_localPlayer is Player caller)
		{
			weapon.calledAt = -1f;
			WeaponReload.Called(weapon, caller);
		}
		else if (UnityEngine.Time.time - weapon.calledAt > Patience)
		{
			weapon.calledAt = -1f;
		}
	}

	private const float Patience = 2f;
}
