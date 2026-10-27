using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// Everything a kind of manned weapon is but its model and its own look: the names it keeps its state and calls under
/// (<see cref="Keys"/>), how far and how fast it turns (<see cref="Aim"/>), its reload (<see cref="Reload"/>), how its
/// operator walks round it and takes hold of it (<see cref="Body"/>), the words it shows (<see cref="Words"/>) and who
/// may take it. One per kind, shared by every copy; made before the first copy wakes and not changed after.
/// </summary>
public sealed class WeaponDesign
{
	public WeaponDesign(string name, WeaponKeys keys)
	{
		Name = name;
		Keys = keys;
		(HoldContext, TriggerContext, SteerContext) = (name + " hold", name + " trigger", name + " steer");
	}

	/// <summary>The weapon's name in the log, where a failure in its controls or reload is reported ("bone ballista").</summary>
	public string Name { get; }

	public WeaponKeys Keys { get; }

	public WeaponArcs Aim { get; set; } = new();

	public ReloadCycle Reload { get; set; } = new(1f);

	/// <summary>
	/// The lay the weapon goes back to whenever nobody mans it (<see cref="WeaponRest"/>): its operator let go, walked off,
	/// died or left the game; a new one starts there. Null (the default): it stays where its last operator left it.
	/// </summary>
	public WeaponRest? Rest { get; set; }

	public OperatorBody Body { get; set; } = new();

	public WeaponWords Words { get; set; } = new();

	/// <summary>The most <see cref="UseRange"/> may be: inside the game's own reach of a doodad control (5 m, measured from the stand point too).</summary>
	public const float MostUse = 4.5f;

	private float use = MostUse, holder = 6f;

	/// <summary>
	/// How near the stand point (<see cref="WeaponFrame.Stand"/>) a player must be to take the weapon, metres, at most
	/// <see cref="MostUse"/>: from anywhere within it, in front of the weapon too, he is brought to the stand point
	/// (<see cref="QuickStep"/>), and the hover offers the use key exactly there.
	/// </summary>
	public float UseRange
	{
		get => use;
		set => use = Mathf.Min(value, MostUse);
	}

	/// <summary>How near the stand point its operator must be for its owner to count him as one (a dead or departed one never is), metres; never less than half a metre past <see cref="UseRange"/>.</summary>
	public float HolderRange
	{
		get => Mathf.Max(holder, use + 0.5f);
		set => holder = value;
	}

	internal readonly string HoldContext, TriggerContext, SteerContext;
}

/// <summary>
/// The weapon's words, as the game's localization keys ("$piece_...") or plain text: its name, its hover's loaded and
/// empty, the use key's take hold and let go, and the messages for having nothing to load, its being in use and its
/// stand point being out of reach from where the player is (swimming, up a step too high, something in the way).
/// </summary>
public sealed class WeaponWords
{
	public string Name { get; set; } = "";
	public string Loaded { get; set; } = "";
	public string Empty { get; set; } = "";
	public string Hold { get; set; } = "";
	public string LetGo { get; set; } = "";
	public string NoAmmo { get; set; } = "";
	public string InUse { get; set; } = "$msg_inuse";
	public string Unreachable { get; set; } = "You cannot reach it from here";
}

/// <summary>
/// A weapon's reload: steps run one after another, each lasting its seconds, every peer drawing them from the moment
/// the ZDO says each began; once the last has run the weapon is loaded, and a shot empties it. The cycle starts by
/// itself whenever the operator carries ammunition and the weapon has rested <see cref="Rest"/> seconds after a shot; one
/// item of <see cref="AmmoType"/> is taken from his inventory as step <see cref="TakeAt"/> begins (from 1). A weapon that
/// takes more, or other things, overrides <see cref="MannedWeapon.FindAmmo"/> and <see cref="MannedWeapon.TakeAmmo"/>.
/// When the operator lets go mid-step the step is done at once (<see cref="FinishOnLetGo"/>, the ballista's string
/// drawn), or runs on to its end by itself, so letting go and taking hold again never cuts a long reload short.
/// </summary>
public sealed class ReloadCycle
{
	public ReloadCycle(params float[] steps) => Steps = steps;

	/// <summary>Seconds each step takes, in order.</summary>
	public float[] Steps { get; }

	public float Rest { get; set; } = 0.5f;

	/// <summary>The game's ammunition type (<c>m_ammoType</c>) taken from the operator's inventory.</summary>
	public string AmmoType { get; set; } = "";

	public int TakeAt { get; set; } = 1;

	/// <summary>Whether a step under way is done at once when the operator lets go (true), or runs on to its end by itself.</summary>
	public bool FinishOnLetGo { get; set; } = true;
}

/// <summary>
/// A weapon's rest lay (<see cref="WeaponDesign.Rest"/>), degrees as the aim's: <see cref="Yaw"/> right of where it was
/// placed, <see cref="Pitch"/> up from the plane it stands on, each held within its arcs. Unmanned, its owner turns it
/// back there at its own turn and tilt rates (<see cref="RestLay"/>), every peer following the ZDO, so a gunner's last
/// lay never stays on it; a weapon never aimed reads it as its lay.
/// </summary>
public sealed class WeaponRest
{
	public WeaponRest(float yaw = 0f, float pitch = 0f) => (Yaw, Pitch) = (yaw, pitch);

	public float Yaw { get; }

	public float Pitch { get; }
}

/// <summary>Where the operator's elbows point while his hands are on the grips.</summary>
public enum ElbowHint
{
	/// <summary>At a fixed point out to his side, down and a little back from the shoulder.</summary>
	Beside,

	/// <summary>Out, back and down, square to the line from the shoulder to the fist (it never flips as a fist comes back to the hip).</summary>
	Square,
}

/// <summary>
/// The operator's body at the weapon. His feet: they stop <see cref="Arrive"/> from their spot and start again only once
/// it has moved <see cref="Restart"/> from where they stopped, ease in over the last <see cref="Slow"/>, and give up on a
/// spot they cannot reach after <see cref="GiveUp"/> seconds slower than <see cref="Still"/>; he faces the way he walks
/// while walking up faster than <see cref="Walking"/>, then the way the weapon points turned <see cref="Facing"/> degrees
/// to its left, turning at <see cref="TurnRate"/>. His hands: on the grips over the last stride up (from
/// <see cref="Reaching"/> to <see cref="Reached"/> from the spot), easing on and off at <see cref="Ease"/> a second.
/// </summary>
public sealed class OperatorBody
{
	public float Arrive { get; set; } = 0.06f;
	public float Restart { get; set; } = 0.15f;
	public float Slow { get; set; } = 0.25f;
	public float Walking { get; set; } = 0.5f;
	public float TurnRate { get; set; } = 720f;
	public float GiveUp { get; set; } = 0.6f;
	public float Still { get; set; } = 0.1f;
	public float Facing { get; set; }
	public float Reached { get; set; } = 0.2f;
	public float Reaching { get; set; } = 0.6f;
	public float Ease { get; set; } = 8f;
	public ElbowHint Elbows { get; set; } = ElbowHint.Beside;
}
