using UnityEngine;

namespace MannedWeapons;

/// <summary>
/// How a weapon's shot flies, for laying the weapon so the shot lands on what the crosshair is on
/// (<see cref="MannedWeapon.Flight"/>, <see cref="WeaponLay"/>): where it leaves (<see cref="From"/>, metres in the pitch
/// part's own frame; it leaves along the pitch part's forward), at <see cref="Speed"/> metres a second (0: a straight
/// line), falling <see cref="Gravity"/> metres a second squared (the game projectile's <c>m_gravity</c>; 0 flies
/// straight), and whether it carries on with the motion of the body the weapon stands on (<see cref="Carried"/>, a ship's:
/// the lay then leads it off).
/// </summary>
public readonly struct ShotFlight
{
	public ShotFlight(Vector3 from, float speed = 0f, float gravity = 0f, bool carried = false) =>
		(From, Speed, Gravity, Carried) = (from, speed, gravity, carried);

	public Vector3 From { get; }

	public float Speed { get; }

	public float Gravity { get; }

	public bool Carried { get; }

	/// <summary>Whether it flies in an arc (a speed and a gravity), not a straight line.</summary>
	public bool Falls => Speed > 0.01f && Gravity > 0f;

	/// <summary>A flight leaving from a point of the world, on the weapon's pitch part as it stands now.</summary>
	public static ShotFlight At(WeaponFrame frame, Vector3 point, float speed = 0f, float gravity = 0f, bool carried = false) =>
		new(Quaternion.Inverse(frame.Pitch.rotation) * (point - frame.Pitch.position), speed, gravity, carried);
}
