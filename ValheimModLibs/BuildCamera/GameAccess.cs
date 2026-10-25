using System;
using HarmonyLib;
using UnityEngine;

namespace BuildCamera;

/// <summary>
/// The game's private members the camera reads, reached by name (the library is built against the game's assembly as it
/// is, so a consumer needs no publicizer for its sake): the player's input check, its three build ray masks and its
/// placement ghost; the game camera's zoom distance, camera and sound listener. Made on first use, which comes only
/// after <see cref="GameTargets"/> has found every one of them.
/// </summary>
internal static class GameAccess
{
	private static readonly Func<Player, bool> takeInput =
		AccessTools.MethodDelegate<Func<Player, bool>>(AccessTools.Method(typeof(Player), "TakeInput"));
	private static readonly AccessTools.FieldRef<Player, int> placeMask = AccessTools.FieldRefAccess<Player, int>("m_placeRayMask");
	private static readonly AccessTools.FieldRef<Player, int> waterMask = AccessTools.FieldRefAccess<Player, int>("m_placeWaterRayMask");
	private static readonly AccessTools.FieldRef<Player, int> removeMask = AccessTools.FieldRefAccess<Player, int>("m_removeRayMask");
	private static readonly AccessTools.FieldRef<Player, GameObject> ghost = AccessTools.FieldRefAccess<Player, GameObject>("m_placementGhost");
	private static readonly AccessTools.FieldRef<GameCamera, float> distance = AccessTools.FieldRefAccess<GameCamera, float>("m_distance");
	private static readonly AccessTools.FieldRef<GameCamera, Camera> view = AccessTools.FieldRefAccess<GameCamera, Camera>("m_camera");
	private static readonly AccessTools.FieldRef<GameCamera, AudioListener> listener = AccessTools.FieldRefAccess<GameCamera, AudioListener>("m_listner");

	/// <summary>The game's own "this player takes input now" (no chat, console, inventory, map, menu or window of a mod).</summary>
	public static bool TakeInput(Player player) => player != null && takeInput(player);

	/// <summary>The placement ray's mask (with water for a piece that cares about it), as <c>PieceRayTest</c> picks it.</summary>
	public static int PlaceMask(Player player, bool water) => water ? waterMask(player) : placeMask(player);

	/// <summary>The remove, copy and repair rays' mask.</summary>
	public static int RemoveMask(Player player) => removeMask(player);

	public static GameObject? Ghost(Player player) => ghost(player);

	/// <summary>The game camera's zoom distance behind the player.</summary>
	public static ref float Distance(GameCamera camera) => ref distance(camera);

	public static Camera? View(GameCamera camera) => view(camera);

	public static AudioListener? Listener(GameCamera camera) => listener(camera);
}
