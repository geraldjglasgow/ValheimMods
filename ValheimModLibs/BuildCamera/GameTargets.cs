using System;
using System.Collections.Generic;
using HarmonyLib;

namespace BuildCamera;

/// <summary>
/// Every game method the camera patches or calls by name and every private field it reads, looked up before anything
/// is patched, so a game update that renames one leaves the camera off (and says which) rather than half working.
/// </summary>
internal static class GameTargets
{
	private static readonly (Type Type, string Name)[] Methods =
	{
		(typeof(GameCamera), "GetCameraPosition"), (typeof(GameCamera), "UpdateListner"),
		(typeof(LightLod), "GetLightReferencePoint"), (typeof(Menu), "Update"),
		(typeof(Player), "Update"), (typeof(Player), "TakeInput"), (typeof(Player), "SetMouseLook"), (typeof(Player), "SetControls"),
		(typeof(Player), "PieceRayTest"), (typeof(Player), "RemovePiece"), (typeof(Player), "CopyPiece"),
		(typeof(Player), "UpdateWearNTearHover"),
	};

	private static readonly (Type Type, string Name)[] Fields =
	{
		(typeof(Player), "m_placeRayMask"), (typeof(Player), "m_placeWaterRayMask"), (typeof(Player), "m_removeRayMask"),
		(typeof(Player), "m_placementGhost"), (typeof(GameCamera), "m_distance"), (typeof(GameCamera), "m_camera"),
		(typeof(GameCamera), "m_listner"),
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
