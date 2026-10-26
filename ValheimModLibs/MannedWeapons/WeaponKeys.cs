using System;
using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The names a weapon keeps its state and its calls under, made from one prefix: ZDO keys `prefix_user` (the operator's
/// player ID), `_yaw` and `_pitch` (degrees), the reload's `_stage` (steps done), `_step` (the step under way, from 1; 0
/// none) and `_step_at` (network ticks), `_shots` and `_shot_at`; RPCs `prefix_request`, `_release` (a player ID) and
/// `_answer` (granted). The three reload keys can be named otherwise, so a weapon that already kept them under its own
/// names keeps its saved state.
/// </summary>
public sealed class WeaponKeys
{
	public WeaponKeys(string prefix, string stage = "stage", string step = "step", string stepAt = "step_at")
	{
		Prefix = prefix;
		(User, Yaw, Pitch) = (Hash("user"), Hash("yaw"), Hash("pitch"));
		(Stage, Step, StepAt) = (Hash(stage), Hash(step), Hash(stepAt));
		(Shots, ShotAt) = (Hash("shots"), Hash("shot_at"));
		(Request, Release, Answer) = (prefix + "_request", prefix + "_release", prefix + "_answer");
	}

	public string Prefix { get; }
	public int User { get; }
	public int Yaw { get; }
	public int Pitch { get; }
	public int Stage { get; }
	public int Step { get; }
	public int StepAt { get; }
	public int Shots { get; }
	public int ShotAt { get; }
	public string Request { get; }
	public string Release { get; }
	public string Answer { get; }

	private int Hash(string name) => (Prefix + "_" + name).GetStableHashCode();
}

/// <summary>The network clock, which every peer reads alike: what the reload's and the shot's moments are kept in.</summary>
public static class WeaponClock
{
	/// <summary>Now, in ticks (0 outside a world).</summary>
	public static long Now() => ZNet.instance != null ? ZNet.instance.GetTime().Ticks : 0L;

	/// <summary>Seconds from `ticks` to now; very long for a moment never set.</summary>
	public static float Since(long ticks) => ticks <= 0L ? 9999f : Mathf.Min(9999f, (Now() - ticks) / (float)TimeSpan.TicksPerSecond);
}
