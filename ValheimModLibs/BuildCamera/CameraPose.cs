using UnityEngine;

namespace BuildCamera;

/// <summary>Where the camera is and where it looks: a position, a yaw and a pitch (never past straight up or down).</summary>
internal static class CameraPose
{
	private const float MaxPitch = 89f;

	public static Vector3 Position { get; set; }
	public static float Yaw { get; private set; }
	public static float Pitch { get; private set; }

	public static Quaternion Rotation => Quaternion.Euler(Pitch, Yaw, 0f);

	/// <summary>Starts where the given view is, looking where it looks.</summary>
	public static void Start(Transform from)
	{
		Position = from.position;
		Vector3 angles = from.rotation.eulerAngles;
		Yaw = angles.y;
		Pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, angles.x), -MaxPitch, MaxPitch);
	}

	/// <summary>The game's look input in degrees (mouse or right stick, its sensitivity and inversion applied).</summary>
	public static void Look(Vector2 delta)
	{
		Yaw += delta.x;
		Pitch = Mathf.Clamp(Pitch - delta.y, -MaxPitch, MaxPitch);
	}
}
