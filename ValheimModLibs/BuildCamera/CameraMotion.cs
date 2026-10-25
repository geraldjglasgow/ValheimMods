using UnityEngine;

namespace BuildCamera;

/// <summary>
/// One frame of flying, with the game's own bindings as the game reads them (<c>ZInput</c>): the movement keys and the
/// left stick pan the camera flat along where it looks (so forward never dives), Jump and the right trigger raise it,
/// Crouch and the left trigger lower it, Run (or the gamepad's run) speeds it up. Only while the game takes the player's
/// input and no build or radial menu is open. The move is pulled into the flight's bounds, swept against the ground,
/// then held above the floors; the floors are held every frame, so rising water lifts a camera at rest too.
/// </summary>
internal static class CameraMotion
{
	public static void Step(Player player, CameraFlight flight, float dt)
	{
		Vector3 from = CameraPose.Position;
		Vector3 to = from;
		if (Steering(player))
		{
			Vector3 move = Direction() * Speed(flight) * dt;
			if (move.sqrMagnitude > 1e-8f)
			{
				to = CameraGround.Move(from, flight.Bounds.Clamp(from + move), flight);
			}
		}
		CameraPose.Position = CameraGround.Hold(to, flight);
	}

	private static bool Steering(Player player) =>
		GameAccess.TakeInput(player) && !Hud.IsPieceSelectionVisible() && !Hud.InRadial();

	private static Vector3 Direction()
	{
		Vector3 local = Vector3.zero;
		local.z += (ZInput.GetButton("Forward") ? 1f : 0f) - (ZInput.GetButton("Backward") ? 1f : 0f);
		local.x += (ZInput.GetButton("Right") ? 1f : 0f) - (ZInput.GetButton("Left") ? 1f : 0f);
		Vector2 stick = ZInput.GetJoyLeftStick();
		local.x += stick.x;
		local.z += stick.y;
		local.y = Vertical();
		return Quaternion.Euler(0f, CameraPose.Yaw, 0f) * Vector3.ClampMagnitude(local, 1f);
	}

	private static float Vertical()
	{
		float up = ZInput.GetJoyRTrigger() - ZInput.GetJoyLTrigger();
		up += (ZInput.GetButton("Jump") ? 1f : 0f) - (ZInput.GetButton("Crouch") ? 1f : 0f);
		return Mathf.Clamp(up, -1f, 1f);
	}

	private static float Speed(CameraFlight flight)
	{
		bool fast = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun");
		return flight.Speed * (fast ? flight.RunMultiplier : 1f);
	}
}
