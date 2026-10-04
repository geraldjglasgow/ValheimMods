using HarmonyLib;

namespace AreaLoading;

/// <summary>
/// The two postfixes, by name since the game's methods are private: <c>ZoneSystem.Update()</c> (every frame on every
/// machine, after the game's own zone step) and <c>ZNetScene.CreateDestroyObjects()</c> (30 times a second, after the
/// game's own create and remove pass). Installed once per merged copy.
/// </summary>
internal static class LoadPatches
{
	private static bool patched;

	public static void Install(Harmony harmony)
	{
		if (patched)
		{
			return;
		}
		patched = true;
		LandLoad.Bind();
		ObjectLoad.Bind();
		harmony.Patch(AccessTools.Method(typeof(ZoneSystem), "Update"), postfix: new HarmonyMethod(typeof(LoadPatches), nameof(AfterZones)));
		harmony.Patch(AccessTools.Method(typeof(ZNetScene), "CreateDestroyObjects"), postfix: new HarmonyMethod(typeof(LoadPatches), nameof(AfterObjects)));
	}

	private static void AfterZones(ZoneSystem __instance) => LandLoad.Tick(__instance);

	private static void AfterObjects(ZNetScene __instance) => ObjectLoad.Hurry(__instance);
}
