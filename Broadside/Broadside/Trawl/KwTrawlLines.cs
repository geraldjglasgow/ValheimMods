using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The trawl's ropes as line renderers in the game's rope (the cargo crane's look, <see cref="KwCargoModel.Line"/>), their
    /// points in world space: straight through given points (the lead from the drum to the sheave), or hanging between two
    /// points in a sag (the tow rope: a parabola as deep as the sag in its middle). Screens only.
    /// </summary>
    public static class KwTrawlLines
    {
        /// <summary>The points along a hanging rope.</summary>
        public const int Points = 10;

        public static void Through(LineRenderer line, Vector3[] points)
        {
            line.positionCount = points.Length;
            line.SetPositions(points);
        }

        /// <summary>The rope from one point to another, sagging by <paramref name="sag"/> metres in its middle, into the buffer.</summary>
        public static void Sagged(LineRenderer line, Vector3[] buffer, Vector3 from, Vector3 to, float sag)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                float u = i / (float)(buffer.Length - 1);
                buffer[i] = Vector3.Lerp(from, to, u) + Vector3.down * (sag * 4f * u * (1f - u));
            }
            Through(line, buffer);
        }
    }
}
