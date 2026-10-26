using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The game's private members the library reads and writes, reached by name (the library is built against the game's
/// assembly as it is, so a consumer needs no publicizer for its sake): the player's look (yaw, pitch, direction, the eye
/// turned to it), his movement, his model, putting his hand items back, and whether he takes input. Made on first use,
/// which comes only after <see cref="GameTargets"/> has found every one of them.
/// </summary>
internal static class GameAccess
{
	private static readonly AccessTools.FieldRef<Character, Quaternion> lookYaw = AccessTools.FieldRefAccess<Character, Quaternion>("m_lookYaw");
	private static readonly AccessTools.FieldRef<Player, float> lookPitch = AccessTools.FieldRefAccess<Player, float>("m_lookPitch");
	private static readonly AccessTools.FieldRef<Character, Vector3> lookDir = AccessTools.FieldRefAccess<Character, Vector3>("m_lookDir");
	private static readonly AccessTools.FieldRef<Character, Vector3> moveDir = AccessTools.FieldRefAccess<Character, Vector3>("m_moveDir");
	private static readonly AccessTools.FieldRef<Character, GameObject> visual = AccessTools.FieldRefAccess<Character, GameObject>("m_visual");
	private static readonly Action<Character> updateEye = AccessTools.MethodDelegate<Action<Character>>(AccessTools.Method(typeof(Character), "UpdateEyeRotation"));
	private static readonly Action<Humanoid, bool, bool> showHands = AccessTools.MethodDelegate<Action<Humanoid, bool, bool>>(AccessTools.Method(typeof(Humanoid), "ShowHandItems"));
	private static readonly Func<Character, bool> takeInput = AccessTools.MethodDelegate<Func<Character, bool>>(AccessTools.Method(typeof(Character), "TakeInput"));

	public static ref Quaternion LookYaw(Player player) => ref lookYaw(player);

	public static ref float LookPitch(Player player) => ref lookPitch(player);

	public static ref Vector3 LookDir(Player player) => ref lookDir(player);

	public static ref Vector3 MoveDir(Player player) => ref moveDir(player);

	public static GameObject? Visual(Player player) => visual(player);

	/// <summary>The eye turned to the look (the player's own override).</summary>
	public static void UpdateEyeRotation(Player player) => updateEye(player);

	/// <summary>The hand items put away on taking hold come back, with the game's animation.</summary>
	public static void ShowHandItems(Player player) => showHands(player, false, true);

	/// <summary>The game's own "this player takes input now" (no chat, console, inventory, map, menu or mod window).</summary>
	public static bool TakeInput(Player player) => takeInput(player);
}

/// <summary>
/// Every game member the library patches or reaches by name, looked up before anything is patched, so a game update that
/// renames one leaves the weapons unmanned (and says which) rather than half working.
/// </summary>
internal static class GameTargets
{
	private static readonly (Type Type, string Name)[] Methods =
	{
		(typeof(Player), "SetControls"), (typeof(Player), "SetMouseLook"), (typeof(Character), "UpdateRotation"),
		(typeof(Character), "UpdateEyeRotation"), (typeof(Humanoid), "ShowHandItems"), (typeof(Character), "TakeInput"),
	};

	private static readonly (Type Type, string Name)[] Fields =
	{
		(typeof(Character), "m_lookYaw"), (typeof(Player), "m_lookPitch"), (typeof(Character), "m_lookDir"),
		(typeof(Character), "m_moveDir"), (typeof(Character), "m_visual"),
	};

	/// <summary>"Type.Member" for each one the game does not have; empty when all are there.</summary>
	public static List<string> Missing()
	{
		List<string> missing = new();
		foreach ((Type type, string name) in Methods)
		{
			if (AccessTools.Method(type, name) == null)
			{
				missing.Add(type.Name + "." + name);
			}
		}
		foreach ((Type type, string name) in Fields)
		{
			if (AccessTools.Field(type, name) == null)
			{
				missing.Add(type.Name + "." + name);
			}
		}
		return missing;
	}
}
