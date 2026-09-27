using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// Highlights the building pieces standing inside the brush footprint with the game's own
    /// <c>WearNTear.Highlight()</c> (the support colours the hammer shows). The game clears a highlight after 0.2
    /// seconds, so refreshing a few times a second keeps them lit exactly while they are inside, with no cleanup.
    /// Only this player sees it.
    /// </summary>
    internal static class PieceHighlight
    {
        private const float Interval = 0.15f;
        private const int MaxPieces = 256;

        private static readonly Collider[] hits = new Collider[1024];
        private static readonly HashSet<WearNTear> seen = new HashSet<WearNTear>();
        private static int pieceMask;
        private static float next;

        public static void Update()
        {
            BrushStroke stroke = PreviewFrame.Stroke;
            if (!PreviewFrame.BrushVisible || stroke == null || !HudSettings.HighlightPieces.Value || Time.time < next)
                return;
            next = Time.time + Interval;
            if (pieceMask == 0)
                pieceMask = LayerMask.GetMask("piece", "piece_nonsolid");
            FootprintSpec spec = stroke.Height == HeightOp.None ? PreviewFrame.PaintSpec : PreviewFrame.HeightSpec;
            int count = Physics.OverlapSphereNonAlloc(stroke.Center, stroke.Reach, hits, pieceMask);
            seen.Clear();
            for (int i = 0; i < count && seen.Count < MaxPieces; i++)
            {
                WearNTear piece = hits[i] != null ? hits[i].GetComponentInParent<WearNTear>() : null;
                if (piece == null || !seen.Add(piece))
                    continue;
                Vector3 at = piece.transform.position;
                if (spec.Covers(at.x, at.z))
                    piece.Highlight();
            }
        }
    }
}
