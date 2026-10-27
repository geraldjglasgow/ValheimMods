using System.Text;
using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// A weapon's hover, on the local player's machine: it offers the use key exactly when pressing it would work
/// (<see cref="WeaponHold.Barred"/>; what could stop him being brought to the stand point is looked at no more than four
/// times a second, for the weapon hovered), says why not when something stops him, and shows nothing of the key when he
/// is too far or busy. The weapon words it (<see cref="MannedWeapon.HoverText"/>; <see cref="Plain"/> by default).
/// </summary>
internal static class WeaponHover
{
	private const float Every = 0.25f;

	private static MannedWeapon? looked;
	private static float lookedAt;
	private static string? barred;

	public static string Text(MannedWeapon weapon)
	{
		if (!weapon.Net.IsValid())
		{
			return "";
		}
		WeaponState state = weapon.State;
		Player? me = Player.m_localPlayer;
		bool mine = me != null && weapon.HeldBy(me);
		bool takeable = mine || !WeaponHold.ValidUser(weapon, state.User);
		string? why = mine || !takeable || me == null || weapon.Frame == null ? null : Barred(weapon, me);
		return Localization.instance.Localize(weapon.HoverText(state, mine, takeable, why));
	}

	/// <summary>The plain hover: name, loaded or empty, then the use key's let go or take hold, or (greyed) why not.</summary>
	public static string Plain(MannedWeapon weapon, in WeaponState s, bool mine, bool takeable, string? barred)
	{
		WeaponWords words = weapon.Design.Words;
		StringBuilder text = new(words.Name);
		text.Append(s.Loaded ? " (" + words.Loaded + ")" : " (" + words.Empty + ")");
		if (mine || (takeable && barred == null))
		{
			text.Append("\n[<color=yellow><b>$KEY_Use</b></color>] ").Append(mine ? words.LetGo : words.Hold);
		}
		else if (takeable && !string.IsNullOrEmpty(barred))
		{
			text.Append("\n<color=#888888>").Append(barred).Append("</color>");
		}
		return text.ToString();
	}

	/// <summary>Why he may not take it from here, looked at again a few times a second.</summary>
	private static string? Barred(MannedWeapon weapon, Player me)
	{
		if (weapon != looked || Time.time - lookedAt > Every)
		{
			(looked, lookedAt, barred) = (weapon, Time.time, WeaponHold.Barred(weapon, me));
		}
		return barred;
	}
}
