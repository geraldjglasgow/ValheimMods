using System.Collections.Generic;
using HarmonyLib;

namespace RecipeCounts;

/// <summary>
/// Starred eggs, and any other item of a single quality, count for recipes. The game's private
/// <c>Player.HaveRequirementItems</c>, behind every "can craft" check (the recipe list, the craft button, multi-craft
/// and crafting itself), counts each requirement at every quality from 1 to the item's <c>m_maxQuality</c> and keeps
/// the largest. An egg has a maximum quality of 1 but carries its hen's level as its quality (a one-star hen lays
/// quality 2 eggs), so a starred egg never counted, while paying (<c>Player.ConsumeResources</c>, quality -1) takes any
/// quality. While that method runs for a recipe that needs every ingredient, <c>Inventory.CountItems(name, 1)</c> for
/// an item whose maximum quality is 1 counts every unit, in any inventory, so a mod that adds chest contents through
/// <c>CountItems</c> counts them too. A recipe that needs only one of its ingredients pays from one quality the game
/// picks, so it keeps the game's count. Installed once per merged copy; the private method is reached by name. With
/// several copies the first one answers and the others see the original skipped.
/// </summary>
public static class AnyQuality
{
	/// <summary>Whether items of a shared name have a single quality, learned from the first one seen.</summary>
	private static readonly Dictionary<string, bool> singleQuality = new();

	private static bool patched;
	private static bool counting;

	public static void Install(Harmony harmony)
	{
		if (patched)
		{
			return;
		}
		patched = true;
		harmony.Patch(AccessTools.Method(typeof(Player), "HaveRequirementItems"),
			prefix: new HarmonyMethod(typeof(AnyQuality), nameof(Open)),
			finalizer: new HarmonyMethod(typeof(AnyQuality), nameof(Close)));
		harmony.Patch(AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems)),
			prefix: new HarmonyMethod(typeof(AnyQuality), nameof(Count)));
	}

	private static void Open(Recipe piece) => counting = piece != null && !piece.m_requireOnlyOneIngredient;

	private static void Close() => counting = false;

	private static bool Count(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result,
		bool __runOriginal)
	{
		if (!__runOriginal)
		{
			return false;
		}
		if (!counting || quality != 1 || !SingleQuality(__instance, name))
		{
			return true;
		}
		__result = __instance.CountItems(name, -1, matchWorldLevel);
		return false;
	}

	/// <summary>False for a name not in this inventory: the game's count is 0 either way.</summary>
	private static bool SingleQuality(Inventory inventory, string name)
	{
		if (name == null)
		{
			return false;
		}
		if (singleQuality.TryGetValue(name, out bool single))
		{
			return single;
		}
		foreach (ItemDrop.ItemData item in inventory.GetAllItems())
		{
			if (item.m_shared.m_name == name)
			{
				single = item.m_shared.m_maxQuality <= 1;
				singleQuality[name] = single;
				return single;
			}
		}
		return false;
	}
}
