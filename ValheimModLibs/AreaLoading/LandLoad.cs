using System;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine;

namespace AreaLoading;

/// <summary>
/// The land. After a long jump or while a respawn waits, the game loads the zones around the player one every 0.1 s
/// (<c>ZoneSystem.Update</c> calls the private <c>CreateLocalZones</c>, which spawns one zone per call), and
/// <c>ZNetScene</c> creates no nearby object until every zone of the simulation area is loaded
/// (<c>IsActiveAreaLoaded</c>): with a simulation distance of 4 that is about 57 zones, some 6 s before the first tree
/// appears, however quick the jump. While <see cref="AreaLoader.Busy"/> and the area is not loaded yet, this calls
/// <c>CreateLocalZones</c> again and again within <see cref="BudgetMs"/> of each frame, so the zones load in the game's
/// own order as fast as the terrain is ready (the game builds it on its worker thread; asking again only queues it).
/// The screen is black then, so the longer frames are not seen. Not on a server before it has placed its locations.
/// </summary>
internal static class LandLoad
{
	private const double BudgetMs = 20.0;

	private static readonly Stopwatch clock = new();
	private static Func<ZoneSystem, Vector3, bool>? createLocalZones;
	private static float startedAt = -1f;
	private static int zones;

	internal static void Bind()
	{
		createLocalZones = AccessTools.MethodDelegate<Func<ZoneSystem, Vector3, bool>>(AccessTools.Method(typeof(ZoneSystem), "CreateLocalZones"));
	}

	/// <summary>The <c>ZoneSystem.Update</c> postfix, every frame on every machine.</summary>
	internal static void Tick(ZoneSystem system)
	{
		if (createLocalZones == null || !AreaLoader.Busy || (ZNet.instance.IsServer() && !system.LocationsGenerated) || system.IsActiveAreaLoaded())
		{
			Report();
			return;
		}
		if (startedAt < 0f)
		{
			startedAt = Time.time;
		}
		Vector3 at = ZNet.instance.GetReferencePosition();
		clock.Restart();
		while (clock.Elapsed.TotalMilliseconds < BudgetMs && createLocalZones(system, at))
		{
			zones++;
		}
	}

	private static void Report()
	{
		if (startedAt < 0f)
		{
			return;
		}
		AreaLoader.Report($"loaded the land around you in {Time.time - startedAt:0.0} s ({zones} zones beyond the game's one per 0.1 s)");
		startedAt = -1f;
		zones = 0;
	}
}
