using UnityEngine;

namespace BuildCamera;

/// <summary>
/// Every mod merges its own copy of this library, so the scene is the only thing two copies share: while a camera is
/// out, an empty child named <see cref="Name"/> sits under the game camera, and no other copy brings its camera out
/// over it. It is removed at once (not at the end of the frame) when the camera comes back.
/// </summary>
internal static class CameraMark
{
	public const string Name = "BuildCamera_out";

	private static GameObject? mine;

	public static bool Taken(GameCamera game)
	{
		Transform found = game.transform.Find(Name);
		return found != null && found.gameObject != mine;
	}

	public static void Set(GameCamera game)
	{
		mine = new GameObject(Name);
		mine.transform.SetParent(game.transform, false);
	}

	public static void Clear()
	{
		if (mine != null)
		{
			Object.DestroyImmediate(mine);
		}
		mine = null;
	}
}
