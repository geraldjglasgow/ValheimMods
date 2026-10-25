using UnityEngine;

namespace BuildCamera;

/// <summary>
/// The trip out now: whose camera it is, the mod's <see cref="CameraFlight"/>, the game camera it took over and that
/// camera's zoom when it began (the game still zooms on the wheel while the view is away; it is put back). It counts as
/// out only while its player is still the local player, so a logout or a new character ends it without a clean-up call.
/// </summary>
internal static class CameraTrip
{
	private static Player? owner;
	private static CameraFlight? flight;
	private static GameCamera? camera;
	private static float zoom;
	private static bool hurt;

	public static CameraFlight? Current => owner != null && owner == Player.m_localPlayer ? flight : null;

	public static bool IsOutFor(Player player) => Current != null && player == owner;

	public static bool Begin(Player player, CameraFlight wanted)
	{
		if (Current != null)
		{
			End(CameraEnd.Asked);
		}
		GameCamera game = GameCamera.instance;
		if (player != Player.m_localPlayer || game == null || ViewTaken(player, game) != null || CameraMark.Taken(game))
		{
			return false;
		}
		(owner, flight, camera, hurt) = (player, wanted, game, false);
		zoom = GameAccess.Distance(game);
		CameraPose.Start(game.transform);
		CameraPose.Position = CameraGround.Hold(wanted.Bounds.Clamp(CameraPose.Position), wanted);
		player.m_onDamaged += OnHurt;
		CameraMark.Set(game);
		return true;
	}

	public static void End(CameraEnd reason)
	{
		CameraFlight? ended = flight;
		if (owner != null)
		{
			owner.m_onDamaged -= OnHurt;
		}
		if (camera != null)
		{
			GameAccess.Distance(camera) = zoom;
		}
		CameraMark.Clear();
		(owner, flight, camera) = (null, null, null);
		ended?.Ended?.Invoke(reason);
	}

	/// <summary>After the local player's update: every way a trip ends by itself.</summary>
	public static void Tick(Player player)
	{
		if (!IsOutFor(player))
		{
			return;
		}
		CameraEnd? end = Why(player);
		if (end != null)
		{
			End(end.Value);
		}
	}

	private static CameraEnd? Why(Player player)
	{
		if (player.IsDead())
		{
			return CameraEnd.Died;
		}
		if (player.IsTeleporting())
		{
			return CameraEnd.Teleported;
		}
		if (hurt && flight!.LeaveWhenHurt)
		{
			return CameraEnd.Hurt;
		}
		CameraEnd? taken = ViewTaken(player, GameCamera.instance);
		return taken ?? (flight!.Stays != null && !flight.Stays(player) ? CameraEnd.HostRule : null);
	}

	// A seat with its own camera, the game's free fly, the player in a cutscene or teleporting, or a new game camera.
	private static CameraEnd? ViewTaken(Player player, GameCamera game)
	{
		bool own = camera == null || game == camera;
		if (game == null || !own || player.IsDead() || player.IsTeleporting() || player.InCutscene())
		{
			return CameraEnd.ViewTaken;
		}
		return player.GetAttachCameraPoint() != null || GameCamera.InFreeFly() ? CameraEnd.ViewTaken : null;
	}

	private static void OnHurt(float damage, Character attacker) => hurt = true;
}
