using HarmonyLib;
using UnityEngine;

namespace BuildCamera;

/// <summary>
/// The four build rays (<see cref="CameraReach"/>), all private, by name: <c>Player.PieceRayTest</c> (the placement ghost
/// of every build tool: the game's mask for the selected piece, with water for water-aware pieces, and the piece's extra
/// placement distance) and <c>Player.RemovePiece</c>, <c>Player.CopyPiece</c>, <c>Player.UpdateWearNTearHover</c> (the
/// remove mask, no extra). The prefixes run first (<see cref="Priority.First"/>), so another prefix on the same method
/// that measures with <c>m_maxPlaceDistance</c> (Keelwright's whole-ship removal) sees the lifted value too.
/// </summary>
internal static class ReachPatches
{
	public static void Install(Harmony harmony)
	{
		Patch(harmony, "PieceRayTest", nameof(PlacePrefix));
		Patch(harmony, "RemovePiece", nameof(RemovePrefix));
		Patch(harmony, "CopyPiece", nameof(RemovePrefix));
		Patch(harmony, "UpdateWearNTearHover", nameof(RemovePrefix));
	}

	private static void Patch(Harmony harmony, string method, string prefix)
	{
		HarmonyMethod first = new(typeof(ReachPatches), prefix) { priority = Priority.First };
		HarmonyMethod finalizer = new(typeof(ReachPatches), nameof(Finalizer));
		harmony.Patch(AccessTools.Method(typeof(Player), method), prefix: first, finalizer: finalizer);
	}

	// Whether the camera is out comes first, so a call while it is not costs one test.
	private static void PlacePrefix(Player __instance, bool water, out float __state) => __state = CameraTrip.IsOutFor(__instance)
		? CameraReach.Open(__instance, GameAccess.PlaceMask(__instance, water), ExtraDistance(__instance))
		: float.NaN;

	private static void RemovePrefix(Player __instance, out float __state) => __state = CameraTrip.IsOutFor(__instance)
		? CameraReach.Open(__instance, GameAccess.RemoveMask(__instance), 0f)
		: float.NaN;

	private static void Finalizer(Player __instance, float __state) => CameraReach.Close(__instance, __state);

	private static float ExtraDistance(Player player)
	{
		GameObject? ghost = GameAccess.Ghost(player);
		Piece? piece = ghost != null ? ghost.GetComponent<Piece>() : null;
		return piece != null ? piece.m_extraPlacementDistance : 0f;
	}
}
