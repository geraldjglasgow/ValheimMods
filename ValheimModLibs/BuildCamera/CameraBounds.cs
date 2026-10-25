using UnityEngine;

namespace BuildCamera;

/// <summary>Where the build camera may be. A mod with its own shape (around its stations, say) implements this.</summary>
public interface ICameraBounds
{
	/// <summary>The point itself when it is inside, else the nearest point inside.</summary>
	Vector3 Clamp(Vector3 point);
}

/// <summary>A ball: at most <see cref="Radius"/> metres from <see cref="Centre"/> in any direction.</summary>
public sealed class BallBounds : ICameraBounds
{
	public BallBounds(Vector3 centre, float radius)
	{
		Centre = centre;
		Radius = radius;
	}

	public Vector3 Centre { get; }
	public float Radius { get; }

	public Vector3 Clamp(Vector3 point) => Bounds.Within(Centre, point, Radius);
}

/// <summary>
/// A capsule: at most <see cref="Radius"/> metres from the nearest point of the line from <see cref="From"/> to
/// <see cref="To"/>. A row of things (a dock, a wall) is covered end to end, with the same reach all round it.
/// </summary>
public sealed class SegmentBounds : ICameraBounds
{
	public SegmentBounds(Vector3 from, Vector3 to, float radius)
	{
		From = from;
		To = to;
		Radius = radius;
	}

	public Vector3 From { get; }
	public Vector3 To { get; }
	public float Radius { get; }

	public Vector3 Clamp(Vector3 point) => Bounds.Within(Nearest(point), point, Radius);

	/// <summary>The point of the line nearest the given point.</summary>
	public Vector3 Nearest(Vector3 point)
	{
		Vector3 line = To - From;
		float length = line.sqrMagnitude;
		if (length < 1e-6f)
		{
			return From;
		}
		float along = Mathf.Clamp01(Vector3.Dot(point - From, line) / length);
		return From + line * along;
	}
}

internal static class Bounds
{
	/// <summary>The point, or the point on its line to the centre at the radius when it lies farther out.</summary>
	public static Vector3 Within(Vector3 centre, Vector3 point, float radius)
	{
		Vector3 offset = point - centre;
		return offset.sqrMagnitude <= radius * radius ? point : centre + offset.normalized * radius;
	}
}
