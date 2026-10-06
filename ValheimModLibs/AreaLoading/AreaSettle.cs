using System.Collections.Generic;
using UnityEngine;

namespace AreaLoading;

/// <summary>
/// Whether the objects around a point have all arrived. The game lands a jump or wakes a player once
/// <c>ZNetScene.IsAreaReady</c> holds: every object this machine knows in the 3x3 zones around the point exists. The
/// server knows every object, so that is the whole truth there. A client only knows what the server has sent so far,
/// and right after arriving it knows almost nothing, so the check can pass before the base does; the game's fixed
/// waits covered that, and a quickened wait no longer does. On a client the point is therefore settled only once the
/// objects known in those zones have not changed for <see cref="QuietSeconds"/>: the server sends the nearest first, so
/// a quiet spell means the zones are complete. The objects are folded into their count, the XOR of their ids and the sum
/// of their data revisions, so one object swapped for another, or one updated, counts as a change too. Callers ask
/// every physics step; the zones are looked at only every <see cref="CheckSeconds"/>, and in between the last answer
/// stands. One point is watched at a time.
/// </summary>
public static class AreaSettle
{
	private const float QuietSeconds = 0.5f;
	private const float CheckSeconds = 0.1f;

	private static readonly List<ZDO> found = new();
	private static readonly SimulationDistance around = new(1, 0);
	private static Vector2s zone;
	private static int count = -1;
	private static long ids;
	private static long revisions;
	private static float quietSince;
	private static float nextCheck;
	private static bool settled;

	public static bool Settled(Vector3 point)
	{
		if (ZNet.instance == null || ZNet.instance.IsServer() || ZDOMan.instance == null)
		{
			return true;
		}
		Vector2s at = ZoneSystem.GetZone(point);
		if (at == zone && Time.time < nextCheck)
		{
			return settled;
		}
		nextCheck = Time.time + CheckSeconds;
		settled = Look(at);
		return settled;
	}

	/// <summary>Folds the zones' objects and compares them with the last look.</summary>
	private static bool Look(Vector2s at)
	{
		found.Clear();
		ZDOMan.instance.FindSectorObjects(at, around, found);
		long idFold = 0L;
		long revisionSum = 0L;
		foreach (ZDO zdo in found)
		{
			idFold ^= zdo.m_uid.UserID * 31L + zdo.m_uid.ID;
			revisionSum += zdo.DataRevision;
		}
		int seen = found.Count;
		found.Clear();
		bool same = at == zone && seen == count && idFold == ids && revisionSum == revisions;
		if (!same)
		{
			zone = at;
			count = seen;
			ids = idFold;
			revisions = revisionSum;
			quietSince = Time.time;
			return false;
		}
		return Time.time - quietSince >= QuietSeconds;
	}
}
