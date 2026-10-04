using System;
using System.Collections.Generic;
using HarmonyLib;

namespace AreaLoading;

/// <summary>
/// Quick area loading: while the mod says the local player is arriving somewhere (<see cref="When"/>: a portal jump
/// behind the loading screen, a respawn), the land around them loads as fast as the machine allows
/// (<see cref="LandLoad"/>) and the objects follow zone by zone (<see cref="ObjectLoad"/>), instead of the game's one
/// zone every 0.1 s and no object until the whole simulation area is in. Never on a dedicated server or before the
/// connection is up. <see cref="Install"/> once per mod: the two postfixes go in once per merged copy of this library,
/// by hand, so <c>PatchAll</c> never picks them up. Each mod's copy hurries only for its own reasons.
/// </summary>
public static class AreaLoader
{
	private static readonly List<Func<bool>> reasons = new();
	private static Action<string>? log;

	public static void Install(Harmony harmony, Action<string>? report = null)
	{
		log = report;
		LoadPatches.Install(harmony);
	}

	/// <summary>A reason to hurry: checked every frame, so it must be cheap. One that throws counts as no.</summary>
	public static void When(Func<bool> busy) => reasons.Add(busy);

	/// <summary>A registered reason holds now, on a machine that draws the world and is connected.</summary>
	public static bool Busy
	{
		get
		{
			if (ZNet.instance == null || ZNet.instance.IsDedicated() || ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
			{
				return false;
			}
			foreach (Func<bool> reason in reasons)
			{
				if (Holds(reason))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static bool Holds(Func<bool> reason)
	{
		try
		{
			return reason();
		}
		catch (Exception)
		{
			return false;
		}
	}

	internal static void Report(string text) => log?.Invoke(text);
}
