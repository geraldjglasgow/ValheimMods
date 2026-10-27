using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Port and starboard for a hull railing part (<see cref="KwRailText"/>): its text knows only its own left and right,
    /// looking along it from its start rib, so the railing windows turn the ship's sides into those letters and back by the
    /// way the part runs: to the bow (its left is port) or back (its left is starboard; the stern's part from the last rib to
    /// a flat stern runs back). The bow is the ship's own, read as the Cabin tool reads it (<see cref="KwCabinBow"/>: a rudder
    /// on the keel line marks the stern, else the way the masts face); a ship that does not tell has its bow the way the player
    /// looks. Pure but for reading the loaded pieces and the camera.
    /// </summary>
    public static class KwRailBow
    {
        /// <summary>The Sides row's choices, and their names in an edit's values, in the same order.</summary>
        public static readonly string[] Labels = { "Both", "Port", "Starboard" }, Names = { "both", "port", "starboard" };

        private const float Reach = 60f;
        private static readonly string Planking = KwToolPieces.PartPrefix + "planking", Rail = KwToolPieces.PartPrefix + KwBiomeCost.Rail;

        /// <summary>+1 when a part standing here and running this way runs to the bow, -1 when it runs back.</summary>
        public static float Runs(Vector3 position, Vector3 way)
        {
            Vector3 forward = new Vector3(way.x, 0f, way.z).normalized;
            List<Piece> near = new List<Piece>();
            Piece.GetAllPiecesInRadius(position, Reach, near);
            float bow = KwCabinBow.Way(near, position, forward, Middle(near, position, forward));
            if (bow == 0f && GameCamera.instance != null)
                bow = Vector3.Dot(GameCamera.instance.transform.forward, forward) >= 0f ? 1f : -1f;
            return bow == 0f ? 1f : bow;
        }

        // Halfway along the hull's planking and railing on the keel line, from the part's start; 0 with none.
        private static float Middle(List<Piece> near, Vector3 origin, Vector3 forward)
        {
            float low = float.MaxValue, high = float.MinValue;
            foreach (Piece piece in near)
            {
                string name = piece != null ? Utils.GetPrefabName(piece.gameObject) : "";
                ZNetView? view = name == Planking || name == Rail ? piece!.m_nview : null;
                Transform? at = view != null && view.IsValid() ? piece!.transform : null;
                if (at == null || !KwCabinHull.InLine(origin, forward, at.position, at.forward) || !KwSpan.TryParse(view!.GetZDO().GetString(KwGenerated.Key), out KwSpan span))
                    continue;
                float start = Vector3.Dot(at.position - origin, forward), end = start + Mathf.Sign(Vector3.Dot(at.forward, forward)) * span.Length;
                (low, high) = (Mathf.Min(low, Mathf.Min(start, end)), Mathf.Max(high, Mathf.Max(start, end)));
            }
            return low <= high ? (low + high) / 2f : 0f;
        }

        /// <summary>A part's letters for the ship's sides by name ("both", "port", "starboard"), as it runs (+1 to the bow).</summary>
        public static string Letters(string name, float runs)
        {
            if (name == Names[1])
                return runs >= 0f ? KwRailText.Left : KwRailText.Right;
            if (name == Names[2])
                return runs >= 0f ? KwRailText.Right : KwRailText.Left;
            return KwRailText.Both;
        }

        /// <summary>The ship's sides by name from a part's letters, as it runs (+1 to the bow).</summary>
        public static string Name(string letters, float runs)
        {
            string sides = KwRailText.Normal(letters);
            if (sides == KwRailText.Both)
                return Names[0];
            return (sides == KwRailText.Left) == (runs >= 0f) ? Names[1] : Names[2];
        }

        /// <summary>A name's place among the choices; both for one that is not among them.</summary>
        public static int Index(string name) => Math.Max(0, Array.IndexOf(Names, name));
    }
}
