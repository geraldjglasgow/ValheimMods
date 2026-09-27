using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// Makes drafts from the tools' points and the current settings: the player's profile, the server's shoulder, end
    /// blend and soft join lengths, and the surface paint.
    /// </summary>
    public static class DraftFactory
    {
        /// <summary>
        /// A ramp from <paramref name="start"/> to <paramref name="end"/>, <paramref name="width"/> metres wide, centred
        /// (side 0) or all to the left (side &gt; 0) or right (side &lt; 0) of the line.
        /// </summary>
        public static PathDraft Ramp(Vector3 start, Vector3 end, int side, bool blendEnds, float width)
        {
            PathDraft draft = new PathDraft
            {
                Profile = PathSettings.Profile.Value,
                JoinLength = PathSettings.SoftJoinLength.Value,
                Shape = Shape(width, side, blendEnds),
                Paint = PathSelection.Paint(PathSettings.RampPaint.Value),
            };
            draft.Points.Add(start);
            draft.Points.Add(end);
            return draft;
        }

        /// <summary>A road through the waypoints, as wide as the brush, with the given surface paint.</summary>
        public static PathDraft Road(IEnumerable<Vector3> waypoints, PaintOp paint)
        {
            PathDraft draft = new PathDraft
            {
                IsRoad = true,
                Shape = Shape(PathSelection.BrushWidth(), 0, PathSettings.RoadBlendEnds.Value),
                Paint = paint,
            };
            draft.Points.AddRange(waypoints);
            return draft;
        }

        private static PathShape Shape(float width, int side, bool blendEnds)
        {
            return PathShape.Make(width, side, PathSettings.ShoulderWidth.Value, PathSettings.EndBlendLength.Value, blendEnds);
        }
    }
}
