using System;
using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>A single aspect-preserving wood image covering the root canvas, anchored at its top-left.</summary>
    public static class TimberCoordinates
    {
        // No panel dimensions enter this calculation: growing a panel only reveals additional canvas coordinates.
        public static Vector2 Uv(Vector2 point, Rect canvas, Rect wood, Vector2 textureSize)
        {
            if (canvas.width <= 0f || canvas.height <= 0f || wood.width <= 0f || wood.height <= 0f
                || textureSize.x <= 0f || textureSize.y <= 0f)
                return Vector2.zero;
            float scale = Math.Max(canvas.width / wood.width, canvas.height / wood.height);
            return new Vector2(
                (wood.xMin + (point.x - canvas.xMin) / scale) / textureSize.x,
                (wood.yMax - (canvas.yMax - point.y) / scale) / textureSize.y);
        }
    }
}
