using System;

namespace BuildCamera;

/// <summary>
/// One trip out of the build camera, as the mod wants it: the bounds it flies in, how fast, how far it builds, what keeps
/// it out of the ground and the water, and what ends it. The mod may change the numbers while it is out (a config edit);
/// they are read every frame.
/// </summary>
public sealed class CameraFlight
{
	public CameraFlight(ICameraBounds bounds)
	{
		Bounds = bounds;
	}

	/// <summary>Where the camera may be: every move is pulled back inside.</summary>
	public ICameraBounds Bounds { get; }

	/// <summary>Metres per second.</summary>
	public float Speed { get; set; } = 10f;

	/// <summary>How much faster while the game's Run is held (Shift, or the gamepad's run).</summary>
	public float RunMultiplier { get; set; } = 3f;

	/// <summary>
	/// Metres from the camera that placing, removing, copying and repairing reach, measured along the game's own build
	/// ray (50 m long, so 50 or more reaches all of it). Asked for each ray with the player's own place distance still
	/// in <c>m_maxPlaceDistance</c>; a piece's extra placement distance is added for placing.
	/// </summary>
	public Func<Player, float> Reach { get; set; } = _ => 50f;

	/// <summary>Swept against the ground and rock (layers <c>terrain</c> and <c>static_solid</c>), sliding along them.</summary>
	public bool Collide { get; set; } = true;

	/// <summary>Held above the terrain's surface as well. Off for a place with caves built into the ground.</summary>
	public bool AboveGround { get; set; }

	/// <summary>Held above the water's surface, waves included, as the game camera is.</summary>
	public bool AboveWater { get; set; } = true;

	/// <summary>Esc (or the gamepad's menu button), when it would open the game menu, brings the camera back instead.</summary>
	public bool EscapeLeaves { get; set; } = true;

	/// <summary>The camera comes back when the player's body takes damage, so the player sees what hurts it.</summary>
	public bool LeaveWhenHurt { get; set; } = true;

	/// <summary>The mod's own rule, asked after every update of the player: false brings the camera back (<see cref="CameraEnd.HostRule"/>).</summary>
	public Func<Player, bool>? Stays { get; set; }

	/// <summary>Called once the camera is back, with why.</summary>
	public Action<CameraEnd>? Ended { get; set; }
}

/// <summary>Why the build camera came back.</summary>
public enum CameraEnd
{
	/// <summary><see cref="FreeCamera.Leave"/>, or another <see cref="FreeCamera.Enter"/>.</summary>
	Asked,
	Escape,
	Died,
	Teleported,
	Hurt,

	/// <summary>Something else took the view: a seat with its own camera (a ship's helm), the game's free fly, a new game camera.</summary>
	ViewTaken,

	/// <summary><see cref="CameraFlight.Stays"/> said no.</summary>
	HostRule,
}
