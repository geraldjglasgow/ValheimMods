using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The parts of a placed weapon the library turns and reads, found by the weapon in its own model
/// (<see cref="MannedWeapon.FindFrame"/>): <see cref="Yaw"/> turns about its up (and is what the game's doodad control
/// reports), <see cref="Pitch"/> tilts about its right on it; <see cref="Stand"/> is where the operator stands to aim and
/// where a player must be to take the weapon (on the yaw part, it swings round as the weapon turns; on the piece, it
/// stays), and the grips are where his fists hold (none: his hands as the game poses them).
/// </summary>
public sealed class WeaponFrame
{
	public WeaponFrame(Transform yaw, Transform pitch, Transform stand, Transform? gripLeft = null, Transform? gripRight = null) =>
		(Yaw, Pitch, Stand, GripLeft, GripRight) = (yaw, pitch, stand, gripLeft, gripRight);

	public Transform Yaw { get; }
	public Transform Pitch { get; }
	public Transform Stand { get; }
	public Transform? GripLeft { get; }
	public Transform? GripRight { get; }
}

/// <summary>
/// One read of a weapon's ZDO: who mans it, where it points (never aimed: its rest lay, <see cref="WeaponDesign.Rest"/>),
/// its reload (steps done, the step under way and how long it has run), its shots and how long since the last. A step
/// that has run its time counts as done (<see cref="Settled"/>) with no one writing it, so an operator letting go
/// mid-step, or leaving the game, leaves a weapon every peer draws the same.
/// </summary>
public readonly struct WeaponState
{
	public readonly long User;
	public readonly float Yaw, Pitch, StepTime, ShotAge;
	public readonly int Stage, Step, Shots;
	private readonly float[] steps;

	private WeaponState(ZDO zdo, WeaponDesign design)
	{
		WeaponKeys keys = design.Keys;
		steps = design.Reload.Steps;
		User = zdo.GetLong(keys.User, 0L);
		(float restYaw, float restPitch) = RestLay.Lay(design);
		Yaw = zdo.GetFloat(keys.Yaw, restYaw);
		Pitch = zdo.GetFloat(keys.Pitch, restPitch);
		Stage = zdo.GetInt(keys.Stage, 0);
		Step = zdo.GetInt(keys.Step, 0);
		StepTime = WeaponClock.Since(zdo.GetLong(keys.StepAt, 0L));
		Shots = zdo.GetInt(keys.Shots, 0);
		ShotAge = WeaponClock.Since(zdo.GetLong(keys.ShotAt, 0L));
	}

	public static WeaponState Read(ZDO zdo, WeaponDesign design) => new(zdo, design);

	/// <summary>How many steps the reload has (<see cref="Settled"/> equal to it: loaded).</summary>
	public int Steps => steps.Length;

	/// <summary>The steps done once the step under way has run its time.</summary>
	public int Settled => Running && StepTime >= steps[Step - 1] ? Step : Stage;

	/// <summary>Whether a step is still running.</summary>
	public bool Busy => Running && StepTime < steps[Step - 1];

	/// <summary>Loaded: every step done.</summary>
	public bool Loaded => Settled == steps.Length;

	private bool Running => Step > 0 && Step <= steps.Length;
}
