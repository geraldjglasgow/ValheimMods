using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace BuildCamera;

/// <summary>
/// The local player's free build camera. <see cref="Install"/> once in the mod's Awake (the patches go in once per merged
/// copy, by hand, so a host's <c>PatchAll</c> never picks them up; on a dedicated server nothing is patched), then
/// <see cref="Enter"/> with a <see cref="CameraFlight"/> (the bounds it flies in and the rest of the mod's choices) and
/// <see cref="Leave"/> to bring it back. While it is out the view flies with the game's movement keys and look, the
/// player's body stays where it stood at rest, and the game's own placement ghost, build, remove, copy and repair aim
/// along the camera's view and reach from it (<see cref="CameraFlight.Reach"/>). Only this machine knows: nothing is
/// written to a ZDO or sent, and pieces go through the game's own networked placement.
/// </summary>
public static class FreeCamera
{
	private static bool installed;

	/// <summary>
	/// Installs the patches once per merged copy. Returns the game members it could not find (then nothing is patched and
	/// <see cref="Enter"/> always refuses); empty when all is well or on a machine that draws nothing.
	/// </summary>
	public static IReadOnlyList<string> Install(Harmony harmony)
	{
		if (installed || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			return new List<string>();
		}
		List<string> missing = GameTargets.Missing();
		if (missing.Count > 0)
		{
			return missing;
		}
		installed = true;
		CameraPatches.Install(harmony);
		ReachPatches.Install(harmony);
		EscapePatch.Install(harmony);
		return missing;
	}

	/// <summary>
	/// Brings the camera out for the local player, starting where the game camera is (pulled into the bounds), looking
	/// where it looks. False when it cannot: not the local player, dead, teleporting, seated with its own camera, the
	/// game's free fly on, no game camera, or another mod's build camera out. A trip already out is ended first.
	/// </summary>
	public static bool Enter(Player player, CameraFlight flight) => installed && CameraTrip.Begin(player, flight);

	/// <summary>Brings the camera back to the player (<see cref="CameraEnd.Asked"/>); nothing when it is not out.</summary>
	public static void Leave()
	{
		if (CameraTrip.Current != null)
		{
			CameraTrip.End(CameraEnd.Asked);
		}
	}

	/// <summary>The camera is out for the local player.</summary>
	public static bool IsOut => CameraTrip.Current != null;

	/// <summary>The camera is out and belongs to this player.</summary>
	public static bool IsOutFor(Player player) => CameraTrip.IsOutFor(player);

	/// <summary>The trip out now, or null.</summary>
	public static CameraFlight? Flight => CameraTrip.Current;

	/// <summary>Where the camera is while out (its last place once back).</summary>
	public static Vector3 Position => CameraPose.Position;

	/// <summary>Where the camera looks while out.</summary>
	public static Quaternion Rotation => CameraPose.Rotation;
}
