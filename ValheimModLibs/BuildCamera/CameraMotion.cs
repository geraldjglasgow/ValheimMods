using UnityEngine;

namespace BuildCamera;

/// <summary>
/// One frame of flying, with the game's own bindings as the game reads them (<c>ZInput</c>): the movement keys and the
/// left stick pan the camera flat along where it looks (so forward never dives), Jump and the right trigger raise it,
/// Crouch and the left trigger lower it, Run (or the gamepad's run) speeds it up. Only while the game takes the player's
/// input (or the flight's <see cref="CameraFlight.FliesWhile"/> says it may fly anyway) and no build or radial menu is open. The move is pulled into the flight's bounds, swept against the ground,
/// then held above the floors; the floors are held every frame, so rising water lifts a camera at rest too. The mouse
/// wheel is the game's (it turns the piece in hand), but while the flight's <see cref="CameraFlight.WheelMoves"/> says
/// so (a mod's sizes window is open, say) it moves the camera along its view, in or out as the game's zoom would
/// (<see cref="CameraFlight.WheelSpeed"/>), also while that window holds the player's input.
/// </summary>
internal static class CameraMotion
{
	public static void Step(Player player, CameraFlight flight, float dt)
	{
		Vector3 from = CameraPose.Position;
		Vector3 to = from;
		if (Steering(player, flight))
		{
			Vector3 move = Direction() * Speed(flight) * dt;
			if (move.sqrMagnitude > 1e-8f)
			{
				to = CameraGround.Move(from, flight.Bounds.Clamp(from + move), flight);
			}
		}
		to = Dolly(player, flight, to);
		CameraPose.Position = CameraGround.Hold(to, flight);
	}

	// The wheel in or out along the view, while the flight lets it.
	private static Vector3 Dolly(Player player, CameraFlight flight, Vector3 from)
	{
		float wheel = ZInput.GetMouseScrollWheel();
		if (Mathf.Abs(wheel) < 1e-4f || flight.WheelSpeed <= 0f || flight.WheelMoves == null || !flight.WheelMoves(player))
		{
			return from;
		}
		Vector3 ahead = CameraPose.Rotation * Vector3.forward;
		return CameraGround.Move(from, flight.Bounds.Clamp(from + ahead * (wheel * flight.WheelSpeed)), flight);
	}

	// While the game takes the player's input, or while the flight lets it fly anyway (a mod's window open, say).
	private static bool Steering(Player player, CameraFlight flight) =>
		(GameAccess.TakeInput(player) || (flight.FliesWhile != null && flight.FliesWhile(player))) && !Hud.IsPieceSelectionVisible() && !Hud.InRadial();

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
