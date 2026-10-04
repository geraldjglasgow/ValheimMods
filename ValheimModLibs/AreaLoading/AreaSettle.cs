using System.Collections.Generic;
using UnityEngine;

namespace AreaLoading;

/// <summary>
/// Whether the objects around a point have all arrived. The game lands a jump or wakes a player once
/// <c>ZNetScene.IsAreaReady</c> holds: every object this machine knows in the 3x3 zones around the point exists. The
/// server knows every object, so that is the whole truth there. A client only knows what the server has sent so far,
/// and right after arriving it knows almost nothing, so the check can pass before the base does; the game's fixed
/// waits covered that, and a quickened wait no longer does. On a client the point is therefore settled only once the
/// number of known objects in those zones has not changed for <see cref="QuietSeconds"/>: the server sends the nearest
/// first, so a quiet spell means the zones are complete. One point is watched at a time.
/// </summary>
public static class AreaSettle
{
	private const float QuietSeconds = 0.5f;

	private static readonly List<ZDO> found = new();
	private static readonly SimulationDistance around = new(1, 0);
	private static Vector2s zone;
	private static int count = -1;
	private static float quietSince;

	public static bool Settled(Vector3 point)
	{
		if (ZNet.instance == null || ZNet.instance.IsServer() || ZDOMan.instance == null)
		{
			return true;
		}
		Vector2s at = ZoneSystem.GetZone(point);
		found.Clear();
		ZDOMan.instance.FindSectorObjects(at, around, found);
		if (at != zone || found.Count != count)
		{
			zone = at;
			count = found.Count;
			quietSince = Time.time;
			return false;
		}
		return Time.time - quietSince >= QuietSeconds;
	}
}
