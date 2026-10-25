using UnityEngine;

namespace BuildCamera;

/// <summary>
/// The game's build rays start at the game camera but measure their reach from the player's eyes (<c>m_eye</c> against
/// <c>m_maxPlaceDistance</c>), which a camera flying away from the player never meets. While the camera is out, each of
/// those methods first casts the same ray itself (origin, direction, 50 m, mask): when the hit lies within the flight's
/// <see cref="CameraFlight.Reach"/> of the camera, the place distance is lifted for that one call so the game's own
/// check passes; otherwise it is made negative so the game finds nothing. The value the game (or another mod) had is
/// put back by a finalizer right after the call, so nothing outside the call ever sees the change, and nothing is left
/// to restore when the camera comes back.
/// </summary>
internal static class CameraReach
{
	private const float GameRayLength = 50f;
	private const float Unbounded = 100000f;

	/// <summary>Returns the value to put back, or NaN when the camera is not out for this player and nothing changed.</summary>
	public static float Open(Player player, int mask, float extra)
	{
		CameraFlight? flight = CameraTrip.Current;
		if (flight == null || !CameraTrip.IsOutFor(player) || GameCamera.instance == null)
		{
			return float.NaN;
		}
		float saved = player.m_maxPlaceDistance;
		float reach = flight.Reach(player) + extra;
		player.m_maxPlaceDistance = InReach(mask, reach) ? Unbounded : -Unbounded;
		return saved;
	}

	public static void Close(Player player, float saved)
	{
		if (!float.IsNaN(saved))
		{
			player.m_maxPlaceDistance = saved;
		}
	}

	private static bool InReach(int mask, float reach)
	{
		Transform view = GameCamera.instance.transform;
		return Physics.Raycast(view.position, view.forward, out RaycastHit hit, GameRayLength, mask) && hit.distance < reach;
	}
}
