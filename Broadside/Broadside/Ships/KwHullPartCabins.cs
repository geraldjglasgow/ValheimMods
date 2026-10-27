using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin's surfaces in a launched ship's hull map (<see cref="KwHullPartMap"/>), so a shot holes the cabin where it
    /// strikes it: its raised hull sides (planking carried up to the roof, <see cref="KwOpeningSurfaces.Raised"/>) and its
    /// own walls (<see cref="KwCabinWalls"/>) as their faces lie, each point facing out of its face; its roof's top, facing
    /// up. Points at most <see cref="KwHullPartMap.Spacing"/> apart. From the cabin's saved text, the same on every peer. Pure.
    /// </summary>
    public static class KwHullPartCabins
    {
        public static void Add(KwHullPartMap map, int index, KwBpPiece piece)
        {
            if (!KwHouses.IsPart(piece.Prefab) || !KwCabinSpec.TryParse(KwShipParts.Saved(piece), out KwCabinSpec spec) || spec.Unbuilt != null)
                return;
            foreach (KwOpeningSurface side in KwOpeningSurfaces.Raised(spec))
                Face(map, index, piece, side);
            foreach (KwCabinWall wall in KwCabinWalls.Of(spec))
                Face(map, index, piece, new KwWallSurface(wall));
            foreach ((float a, float c, Vector2 sideA, Vector2 sideC) in KwCabinRoof.Pieces(KwCabinOutline.Roof(spec)))
                Roof(map, index, piece, spec.Top, a, c, sideA, sideC);
        }

        // A face's points: along its reach, and up it from its foot to its top at each place, Spacing apart.
        private static void Face(KwHullPartMap map, int index, KwBpPiece piece, KwOpeningSurface face)
        {
            (float from, float to) = face.Reach;
            int along = Mathf.Max(1, Mathf.CeilToInt((to - from) / KwHullPartMap.Spacing));
            for (int i = 0; i <= along; i++)
            {
                float u = Mathf.Lerp(from, to, i / (float)along);
                (float lo, float hi) = face.Range(Mathf.Clamp(u, from + 0.005f, to - 0.005f));
                int up = Mathf.Max(1, Mathf.CeilToInt((hi - lo) / KwHullPartMap.Spacing));
                for (int j = 0; j <= up && hi - lo > 0.01f; j++)
                {
                    float z = Mathf.Lerp(lo, hi, j / (float)up);
                    Vector3 n = face.Out(u, z);
                    if (n.sqrMagnitude > 0.5f)
                        map.Put(index, piece, face.Point(u, z, 0f), n, face);
                }
            }
        }

        // A roof piece's top: lines across it at both its ends and between, Spacing apart.
        private static void Roof(KwHullPartMap map, int index, KwBpPiece piece, float top, float a, float c, Vector2 sideA, Vector2 sideC)
        {
            int cuts = Mathf.Max(1, Mathf.CeilToInt((c - a) / KwHullPartMap.Spacing));
            for (int k = 0; k <= cuts; k++)
            {
                float f = k / (float)cuts;
                Vector2 side = Vector2.Lerp(sideA, sideC, f);
                map.Across(index, piece, Mathf.Lerp(a, c, f), side.x, side.y, top);
            }
        }
    }
}
