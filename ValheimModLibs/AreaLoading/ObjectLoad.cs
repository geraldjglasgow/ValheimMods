using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace AreaLoading;

/// <summary>
/// The objects. <c>ZNetScene.CreateDestroyObjects</c> runs 30 times a second and creates at most 100 nearby objects
/// each time behind a loading screen (a hundredth of those waiting when more), and none at all until every zone of the
/// simulation area is loaded. While <see cref="AreaLoader.Busy"/>, this creates more of the objects the game has just
/// listed (its private <c>m_tempCurrentObjects</c>), within <see cref="BudgetMs"/> per run, in the game's own order
/// (its private <c>ZDOCompare</c>: by type, then nearest to the player first) through the game's own private
/// <c>CreateObject</c>, but only in zones whose land is loaded: the whole-area gate is what keeps an object from being
/// made where there is no ground yet, and this keeps that promise zone by zone. So the objects around the target
/// appear as soon as their own land has, and the far edge of the area finishes after the landing.
/// </summary>
internal static class ObjectLoad
{
	private const double BudgetMs = 15.0;

	private static readonly List<ZDO> pending = new();
	private static readonly Stopwatch clock = new();
	private static FieldInfo? listed;
	private static Func<ZNetScene, ZDO, GameObject>? create;
	private static Comparison<ZDO>? order;

	internal static void Bind()
	{
		// A readonly field: read by plain reflection, and only while hurrying.
		listed = AccessTools.Field(typeof(ZNetScene), "m_tempCurrentObjects");
		create = AccessTools.MethodDelegate<Func<ZNetScene, ZDO, GameObject>>(AccessTools.Method(typeof(ZNetScene), "CreateObject"));
		order = AccessTools.MethodDelegate<Comparison<ZDO>>(AccessTools.Method(typeof(ZNetScene), "ZDOCompare"));
	}

	/// <summary>The <c>ZNetScene.CreateDestroyObjects</c> postfix, 30 times a second on every machine.</summary>
	internal static void Hurry(ZNetScene scene)
	{
		if (listed == null || create == null || order == null || !AreaLoader.Busy || ZoneSystem.instance == null)
		{
			return;
		}
		if (listed.GetValue(scene) is not List<ZDO> near)
		{
			return;
		}
		Collect(near, ZNet.instance.GetReferencePosition());
		pending.Sort(order);
		clock.Restart();
		foreach (ZDO zdo in pending)
		{
			if (clock.Elapsed.TotalMilliseconds >= BudgetMs)
			{
				break;
			}
			create(scene, zdo);
		}
		pending.Clear();
	}

	/// <summary>The listed objects not made yet whose zone has its land and is ready for their type, with the game's sort key.</summary>
	private static void Collect(List<ZDO> near, Vector3 at)
	{
		pending.Clear();
		ZoneSystem zones = ZoneSystem.instance;
		foreach (ZDO zdo in near)
		{
			if (zdo.Created)
			{
				continue;
			}
			Vector2s sector = zdo.GetSector();
			if (!zones.IsZoneLoaded(sector) || !zones.IsZoneReadyForType(sector, zdo.Type))
			{
				continue;
			}
			zdo.m_tempSortValue = Utils.DistanceSqr(at, zdo.GetPosition());
			pending.Add(zdo);
		}
	}
}
