using UnityEngine;
using UnityEngine.UI;

namespace HaloRing.Rendering;

/// <summary>
/// A generated annulus wedge, inner to outer radius, start to end angle (screen degrees, as in
/// <see cref="Pointing.SelectionMath"/>), a vertex pair every two degrees; a sliced sprite cannot make a clean wedge.
/// Vertices are relative to <see cref="OriginOffset"/>, so the segment's root can sit at its own centroid and scale
/// the wedge about that point.
/// </summary>
internal sealed class WedgeGraphic : Graphic
{
	private const float DegreesPerStep = 2f;

	public float InnerRadius;
	public float OuterRadius;
	public float StartAngle;
	public float EndAngle;
	public Vector2 OriginOffset;

	public void Apply(float innerRadius, float outerRadius, float startAngle, float endAngle, Vector2 originOffset)
	{
		InnerRadius = innerRadius;
		OuterRadius = outerRadius;
		StartAngle = startAngle;
		EndAngle = endAngle;
		OriginOffset = originOffset;
		SetVerticesDirty();
	}

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
		float span = EndAngle - StartAngle;
		if (span <= 0f || OuterRadius <= InnerRadius)
			return;
		int steps = Mathf.Max(1, Mathf.CeilToInt(span / DegreesPerStep));
		for (int i = 0; i <= steps; i++)
		{
			float angle = (StartAngle + span * i / steps) * Mathf.Deg2Rad;
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			vh.AddVert(dir * InnerRadius - OriginOffset, color, Vector2.zero);
			vh.AddVert(dir * OuterRadius - OriginOffset, color, Vector2.zero);
			if (i == 0)
				continue;
			int baseIndex = (i - 1) * 2;
			vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 3);
			vh.AddTriangle(baseIndex, baseIndex + 3, baseIndex + 2);
		}
	}
}
