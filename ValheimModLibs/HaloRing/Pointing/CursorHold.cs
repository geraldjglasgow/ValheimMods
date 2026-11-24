using UnityEngine;

namespace HaloRing.Pointing;

/// <summary>The cursor's lock and visibility saved once as a ring opens and put back exactly on close. One per copy of
/// the library is enough: only one ring of a copy is open at a time (<see cref="OpenRings"/>).</summary>
internal static class CursorHold
{
	private static CursorLockMode savedLockState;
	private static bool savedVisible;
	private static bool saved;

	public static void Suspend()
	{
		if (saved)
			return;
		savedLockState = ZCursor.LockState;
		savedVisible = ZCursor.IsVisible;
		saved = true;
	}

	public static void Restore()
	{
		if (!saved)
			return;
		saved = false;
		ZCursor.LockState = savedLockState;
		if (savedVisible)
			ZCursor.Show();
		else
			ZCursor.Hide();
	}
}
