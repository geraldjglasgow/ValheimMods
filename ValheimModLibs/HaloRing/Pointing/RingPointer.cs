using UnityEngine;

namespace HaloRing.Pointing;

/// <summary>Where the player points, as an offset from the screen centre: the gamepad stick (a full push reaches the
/// rim) while a gamepad is in use and allowed, else the mouse.</summary>
internal static class RingPointer
{
	public static Vector2 Offset(bool gamepad, RingStick stick, float outerRadius)
	{
		if (gamepad && ZInput.IsGamepadActive())
			return Stick(stick) * outerRadius;
		return (Vector2)UnityEngine.Input.mousePosition - new Vector2(Screen.width, Screen.height) / 2f;
	}

	private static Vector2 Stick(RingStick stick) => stick == RingStick.Left
		? new Vector2(ZInput.GetJoyLeftStickX(), ZInput.GetJoyLeftStickY())
		: new Vector2(ZInput.GetJoyRightStickX(), ZInput.GetJoyRightStickY());
}
