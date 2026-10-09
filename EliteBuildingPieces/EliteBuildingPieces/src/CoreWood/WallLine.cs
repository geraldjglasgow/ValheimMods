using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteBuildingPieces.CoreWood
{
    /// <summary>
    /// The stakewall and the core wood pieces stand on one line of snap points 2 m apart, so none may snap into another's
    /// place. The game refuses a snap only onto a piece of the same name at the same spot (Player.IsOverlappingOtherPiece),
    /// which let a stakewall snap straight into a core wood wall. This widens it to the whole family by footprint: a
    /// family piece does not snap where it would stand on a family piece's line and course and overlap it along the line
    /// (a stakewall into a core wood wall, a wall into half a large wall, ...). Beside, above or across one still snaps.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.IsOverlappingOtherPiece))]
    public static class WallLine
    {
        private const float Course = 0.5f;
        private const float Depth = 0.2f;
        private const float Touch = 0.1f;

        [HarmonyPostfix]
        private static void Postfix(Vector3 p, Quaternion rotation, string pieceName, List<Piece> pieces, ref bool __result)
        {
            float width = __result ? 0f : Width(pieceName);
            if (width <= 0f || pieces == null)
                return;
            foreach (Piece other in pieces)
            {
                if (other != null && Inside(p, rotation, width, other))
                {
                    __result = true;
                    return;
                }
            }
        }

        private static bool Inside(Vector3 p, Quaternion rotation, float width, Piece other)
        {
            float otherWidth = Width(other.gameObject.name);
            if (otherWidth <= 0f || Mathf.Abs(Vector3.Dot(rotation * Vector3.right, other.transform.right)) < 0.98f)
                return false;
            Vector3 offset = Quaternion.Inverse(rotation) * (other.transform.position - p);
            return Mathf.Abs(offset.y) < Course && Mathf.Abs(offset.z) < Depth &&
                   Mathf.Abs(offset.x) < (width + otherWidth) / 2f - Touch;
        }

        /// <summary>A family piece's width along its line in metres; 0 for any other piece.</summary>
        private static float Width(string name)
        {
            int clone = name.IndexOf("(Clone)", StringComparison.Ordinal);
            if (clone >= 0)
                name = name.Substring(0, clone);
            if (name == CoreWoodPieces.Wall)
                return 2f;
            foreach (CoreWoodPiece piece in CoreWoodPieces.All)
            {
                if (piece.Prefab == name)
                    return 2f * piece.Width;
            }
            return 0f;
        }
    }
}
