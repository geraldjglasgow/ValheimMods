using System;
using PatchGuard;

namespace MannedWeapons;

/// <summary>
/// For the weapon's code that runs inside a game call it must never break (the player's controls, his physics step): a
/// failure is reported under the mod's name (PatchGuard) and swallowed, so the game's own step still happens. The
/// arguments are passed in, so a static lambda allocates nothing.
/// </summary>
internal static class Safe
{
	public static void Run<T1, T2>(string context, Action<T1, T2> action, T1 first, T2 second)
	{
		try
		{
			action(first, second);
		}
		catch (Exception e)
		{
			Guard.Report(e, context);
		}
	}

	public static void Run<T1, T2, T3>(string context, Action<T1, T2, T3> action, T1 first, T2 second, T3 third)
	{
		try
		{
			action(first, second, third);
		}
		catch (Exception e)
		{
			Guard.Report(e, context);
		}
	}
}
