namespace MannedWeapons;

/// <summary>
/// The reload and the trigger, on the operator's machine, which owns the weapon's ZDO while he holds it. The reload runs
/// by itself (<see cref="ReloadCycle"/>): each physics step a step that has run its time is written as done, and the
/// next begins when he carries ammunition (taken as its step says) and the weapon has rested after its last shot. The
/// trigger fires a loaded weapon (<see cref="MannedWeapon.Shoot"/>), then writes it empty and counts the shot, every
/// peer's cue for the shot's look and sound.
/// </summary>
internal static class WeaponReload
{
	public static void Tick(MannedWeapon weapon, Player player)
	{
		if (weapon.Frame == null || !weapon.Net.IsOwner())
		{
			return;   // ownership is on its way from the old owner
		}
		Step(weapon, player, weapon.State);
	}

	/// <summary>Finishes a step that ran its time; else starts the next one when the operator has ammunition.</summary>
	private static void Step(MannedWeapon weapon, Player player, in WeaponState s)
	{
		if (s.Busy)
		{
			return;
		}
		if (s.Step != 0)
		{
			Set(weapon, s.Settled, 0);
			return;
		}
		ItemDrop.ItemData? ammo = s.Stage == s.Steps || s.ShotAge < weapon.Design.Reload.Rest ? null : weapon.FindAmmo(player);
		if (ammo == null)
		{
			return;
		}
		weapon.TakeAmmo(player, ammo, s.Stage + 1);
		Set(weapon, s.Stage, s.Stage + 1);
		weapon.Net.GetZDO().Set(weapon.Design.Keys.StepAt, WeaponClock.Now());
	}

	/// <summary>The trigger: fire a loaded weapon; with nothing loaded and nothing to load, say so.</summary>
	public static void Trigger(MannedWeapon weapon, Player player)
	{
		if (weapon.Frame == null || !weapon.Net.IsValid() || !weapon.Net.IsOwner())
		{
			return;
		}
		WeaponState s = weapon.State;
		if (s.Loaded && !s.Busy)
		{
			if (weapon.Shoot(player))
			{
				Fired(weapon);
			}
		}
		else if (!s.Busy && weapon.FindAmmo(player) == null)
		{
			player.Message(MessageHud.MessageType.Center, weapon.Design.Words.NoAmmo);
		}
	}

	/// <summary>
	/// The owner, as the operator lets go: a step under way is done at once (or, without <see cref="ReloadCycle.FinishOnLetGo"/>,
	/// left to run out, every peer counting it done once it has, the next operator writing it), and nobody holds the weapon.
	/// </summary>
	public static void LetGo(MannedWeapon weapon)
	{
		WeaponState s = weapon.State;
		if (s.Step != 0 && weapon.Design.Reload.FinishOnLetGo)
		{
			Set(weapon, s.Step, 0);
		}
		weapon.Net.GetZDO().Set(weapon.Design.Keys.User, 0L);
	}

	private static void Fired(MannedWeapon weapon)
	{
		ZDO zdo = weapon.Net.GetZDO();
		WeaponKeys keys = weapon.Design.Keys;
		zdo.Set(keys.Stage, 0);
		zdo.Set(keys.Step, 0);
		zdo.Set(keys.Shots, zdo.GetInt(keys.Shots) + 1);
		zdo.Set(keys.ShotAt, WeaponClock.Now());
	}

	private static void Set(MannedWeapon weapon, int stage, int step)
	{
		ZDO zdo = weapon.Net.GetZDO();
		zdo.Set(weapon.Design.Keys.Stage, stage);
		zdo.Set(weapon.Design.Keys.Step, step);
	}
}
