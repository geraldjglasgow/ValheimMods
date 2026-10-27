using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Which of the kit's snap points may meet: keels join only at their joints, a keel's foot sits only on a keel rest (a
    /// keel block's), a keel's rib points (its "Keel top", which the rib tool reads) take nothing, a rudder's pin goes only
    /// on a stern post's rudder point, a cap only on a post's top, a keel block only on a guide's seat, guides only end to
    /// end, lined up and from further off (<see cref="KwGuideSnap"/>). Every other point (the fittings', the rails' ends, the
    /// game's own) meets any point but those. Without the rules a fitting could snap onto a
    /// keel's rib points and stand inside it. Only a kit piece's ghost snaps by these rules, by the game's own search (the
    /// nearest pair within reach, or the point the player picked with the snap key) with the refused pairs left out; the
    /// game's own pieces snap as they always do.
    /// </summary>
    [HarmonyPatch(typeof(Player), "FindClosestSnapPoints")]
    public static class KwSnapRules
    {
        private static readonly Dictionary<string, string[]> Meets = new Dictionary<string, string[]>
        {
            ["Keel joint"] = new[] { "Keel joint" },
            ["Keel foot"] = new[] { "Keel rest" },
            ["Keel rest"] = new[] { "Keel foot" },
            ["Keel top"] = new string[0],
            ["Rudder pin"] = new[] { "Rudder" },
            ["Rudder"] = new[] { "Rudder pin" },
            ["Post top"] = new[] { "Cap foot" },
            ["Cap foot"] = new[] { "Post top" },
            ["Block seat"] = new[] { "Cradle foot" },
            ["Cradle foot"] = new[] { "Block seat" },
            ["Guide end"] = new[] { "Guide end" },
        };

        /// <summary>Whether a snap point named a may meet one named b.</summary>
        public static bool Fits(string a, string b)
        {
            bool ruledA = Meets.TryGetValue(a, out string[] withA), ruledB = Meets.ContainsKey(b);
            return ruledA ? System.Array.IndexOf(withA, b) >= 0 : !ruledB;
        }

        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Transform ghost, float maxSnapDistance, ref Transform a, ref Transform b, List<Piece> pieces, ref bool __result)
        {
            if (!Utils.GetPrefabName(ghost.gameObject).StartsWith("KW_"))
                return true;
            Transform? mine = null, theirs = null;
            __result = Guard.Run("Broadside snapping", () => Find(__instance, ghost, maxSnapDistance, pieces, out mine, out theirs));
            a = mine!;
            b = theirs!;
            return false;
        }

        private static bool Find(Player player, Transform ghost, float reach, List<Piece> pieces, out Transform? a, out Transform? b)
        {
            List<Transform> mine = player.m_tempSnapPoints1, theirs = player.m_tempSnapPoints2;
            mine.Clear();
            ghost.GetComponent<Piece>().GetSnapPoints(mine);
            theirs.Clear();
            pieces.Clear();
            Piece.GetSnapPoints(ghost.position, 10f, theirs, pieces);
            a = b = null;
            if (player.m_manualSnapPoint < 0 && Utils.GetPrefabName(ghost.gameObject) == KwGuideSnap.Prefab && KwGuideSnap.Find(ghost, theirs, mine, out a, out b))
                return true;
            float best = float.MaxValue;
            for (int i = 0; i < mine.Count; i++)
            {
                if (player.m_manualSnapPoint >= 0 && i != player.m_manualSnapPoint)
                    continue;
                Transform? near = Nearest(mine[i], theirs, reach, out float distance);
                if (near == null || distance >= best)
                    continue;
                best = distance;
                a = mine[i];
                b = near;
            }
            return a != null;
        }

        // The nearest point within reach that this one may meet.
        private static Transform? Nearest(Transform point, List<Transform> others, float reach, out float distance)
        {
            Transform? closest = null;
            distance = float.MaxValue;
            foreach (Transform other in others)
            {
                float d = Vector3.Distance(other.position, point.position);
                if (d > reach || d >= distance || !Fits(point.name, other.name))
                    continue;
                closest = other;
                distance = d;
            }
            return closest;
        }
    }
}
