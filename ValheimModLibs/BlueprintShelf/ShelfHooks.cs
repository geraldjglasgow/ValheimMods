using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WindowInput;

namespace BlueprintShelf;

/// <summary>
/// The library's ties to the game, once per merged copy, by hand (no attributes, so a host's <c>PatchAll</c> never picks
/// them up): a postfix on the private <c>ZNet.Awake</c> registers every shelf's RPCs on each new network's table; a prefix
/// and postfix on the private <c>ConnectPanel.Update</c> keep the game's connection panel as it was while a shelf window
/// is open (its F2 renames there); the windows join WindowInput; and a hidden object kept across scenes ticks every shelf
/// each frame (<see cref="ShelfTicker"/>, idle on a dedicated server).
/// </summary>
internal static class ShelfHooks
{
	private static readonly List<Shelf> shelves = new();
	private static bool patched;

	public static void Add(Shelf shelf, Harmony harmony)
	{
		shelves.Add(shelf);
		Install(harmony);
		GameWindow.Add(() => shelf.IsOpen, shelf.Window.Escape);
		if (ZNet.instance != null && ZRoutedRpc.instance != null)
		{
			shelf.Rpc.Register(ZRoutedRpc.instance);
		}
		ShelfTicker.Ensure();
	}

	private static void Install(Harmony harmony)
	{
		if (patched)
		{
			return;
		}
		patched = true;
		GameWindow.Install(harmony);
		harmony.Patch(AccessTools.Method(typeof(ZNet), "Awake"), postfix: new HarmonyMethod(typeof(ShelfHooks), nameof(NetworkAwake)));
		harmony.Patch(AccessTools.Method(typeof(ConnectPanel), "Update"),
			prefix: new HarmonyMethod(typeof(ShelfHooks), nameof(PanelBefore)), postfix: new HarmonyMethod(typeof(ShelfHooks), nameof(PanelAfter)));
	}

	/// <summary>The routed RPC table is new with every network, so the RPCs are registered on it each time.</summary>
	private static void NetworkAwake()
	{
		if (ZRoutedRpc.instance == null)
		{
			return;
		}
		foreach (Shelf shelf in shelves)
		{
			shelf.Run("RPCs", () => shelf.Rpc.Register(ZRoutedRpc.instance));
		}
	}

	private static void PanelBefore(ConnectPanel __instance, out bool __state) =>
		__state = __instance.m_root != null && __instance.m_root.gameObject.activeSelf;

	private static void PanelAfter(ConnectPanel __instance, bool __state)
	{
		if (__instance.m_root != null && AnyOpen())
		{
			__instance.m_root.gameObject.SetActive(__state);
		}
	}

	private static bool AnyOpen()
	{
		foreach (Shelf shelf in shelves)
		{
			if (shelf.IsOpen)
			{
				return true;
			}
		}
		return false;
	}

	public static void TickAll()
	{
		foreach (Shelf shelf in shelves)
		{
			shelf.Run("tick", shelf.Tick);
		}
	}
}

/// <summary>Ticks every shelf of this copy each frame, on a hidden object kept across scenes; off on a dedicated server.</summary>
internal sealed class ShelfTicker : MonoBehaviour
{
	private static ShelfTicker? instance;

	public static void Ensure()
	{
		if (instance != null)
		{
			return;
		}
		GameObject holder = new GameObject("BlueprintShelf");
		DontDestroyOnLoad(holder);
		instance = holder.AddComponent<ShelfTicker>();
	}

	private void Update()
	{
		if (ZNet.instance != null && ZNet.instance.IsDedicated())
		{
			enabled = false;
			return;
		}
		ShelfHooks.TickAll();
	}
}
