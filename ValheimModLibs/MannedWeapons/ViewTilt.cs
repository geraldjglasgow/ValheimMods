using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// The operator's view tilted with his weapon (<see cref="WeaponArcs.ViewTiltStops"/>), local player only. It tilts
/// toward where the mouse puts it no faster than the weapon tilts (<see cref="WeaponArcs.TiltRate"/>), and never so far
/// that the weapon could not lay its shot on what the crosshair is then on: the view's ray is followed to what it is on
/// (<see cref="WeaponSight"/>) and the weapon laid on it as the aim lays it (<see cref="WeaponLay"/>, the shot's arc
/// counted); where it would have to tilt past its arc to reach that, the view is held on where the shot, laid at its
/// stop, is as far out as the ray reached (a few steps a frame, the camera swung about the eye with the view, so it
/// settles where the ray meets the shot's path: on the ground where the shot would land, or on the sky along the barrel).
/// So view and weapon stop together, measured from the plane the weapon stands on, a slope or a rolling deck too. The
/// game's pitch is degrees down.
/// </summary>
internal static class ViewTilt
{
	/// <summary>Steps a frame toward the stop, and how near (degrees) is the stop.</summary>
	private const int Tries = 4;
	private const float Near = 0.02f;

	private static float pitch;

	/// <summary>The view's pitch as last held (degrees down).</summary>
	internal static float Now => pitch;

	/// <summary>Taking hold: the view starts at the weapon's own tilt.</summary>
	public static void Begin(MannedWeapon weapon)
	{
		Vector3 way = weapon.Frame != null ? weapon.Frame.Pitch.forward : weapon.transform.forward;
		pitch = -Mathf.Asin(Mathf.Clamp(way.y, -1f, 1f)) * Mathf.Rad2Deg;
	}

	/// <summary>The view's pitch this frame, from the one the mouse asks for, with the view's yaw already set.</summary>
	public static float Step(MannedWeapon weapon, Player player, Quaternion yaw, float wanted, float dt)
	{
		WeaponArcs arcs = weapon.Design.Aim;
		pitch = Mathf.Clamp(Mathf.MoveTowards(pitch, wanted, arcs.TiltRate * dt), -arcs.LookUp, arcs.LookDown);
		if (GameCamera.instance != null && weapon.Frame != null)
		{
			pitch = Mathf.Clamp(Held(weapon, player, arcs, yaw, pitch), -arcs.LookUp, arcs.LookDown);
		}
		return pitch;
	}

	/// <summary>The pitch, or the nearest to it whose crosshair the weapon can still lay its shot on.</summary>
	private static float Held(MannedWeapon weapon, Player player, WeaponArcs arcs, Quaternion yaw, float look)
	{
		for (int i = 0; i < Tries; i++)
		{
			Vector3 ahead = yaw * Quaternion.Euler(look, 0f, 0f) * Vector3.forward;
			Vector3 eye = Camera(player, ahead);
			Sight sight = WeaponSight.Target(weapon, arcs, eye, ahead);
			(float turn, float need) = WeaponLay.Solve(weapon, sight, weapon.Aim.Yaw, weapon.Aim.Pitch);
			float stop = Mathf.Clamp(need, arcs.PitchDown, arcs.PitchUp);
			if (Mathf.Abs(need - stop) < Near)
			{
				return look;
			}
			look = Toward(weapon, eye, yaw, sight, arcs.ClampYaw(turn), stop);
		}
		return look;
	}

	/// <summary>Where the camera would be looking along `ahead`: the game's camera swung about the eye with the view.</summary>
	internal static Vector3 Camera(Player player, Vector3 ahead)
	{
		Transform camera = GameCamera.instance.transform;
		Vector3 eye = player.m_eye.position;
		return eye + Quaternion.FromToRotation(camera.forward, ahead) * (camera.position - eye);
	}

	/// <summary>The pitch that looks from `eye` at where the weapon's shot, laid at `turn` and its stop, is as far out along the ground as the sight.</summary>
	private static float Toward(MannedWeapon weapon, Vector3 eye, Quaternion yaw, in Sight sight, float turn, float stop)
	{
		ShotFlight flight = weapon.Flight;
		Vector3 from = WeaponLay.Launch(weapon, flight, turn, stop), run = sight.Point - from;
		Vector3 way = WeaponLay.Direction(weapon, turn, stop);
		Vector3 at = WeaponLay.Along(flight, from, way, new Vector2(run.x, run.z).magnitude, sight.Hit) - eye;
		float along = Vector3.Dot(at, yaw * Vector3.forward);
		return -Mathf.Atan2(at.y, Mathf.Max(0.01f, along)) * Mathf.Rad2Deg;
	}
}
