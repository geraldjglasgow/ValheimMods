using System.Collections.Generic;
using UnityEngine;

namespace EliteBuildingPieces.CoreWood
{
    /// <summary>
    /// The model's anchors <c>snap_0</c>.. become the piece's snap points, laid out like the stakewall's: at each post,
    /// on the ground and 2 m up (the 2 m pieces at x = -1 and 1, the 4 m pieces also in the middle, at -2, 0 and 2).
    /// The game reads snap points only from the piece's own children, tagged <c>snappoint</c>; like the stakewall's they
    /// are inactive and named for the build hud (<c>$hud_snappoint_bottom 1</c>, <c>$hud_snappoint_mid 1</c>, ...).
    /// </summary>
    public static class CoreWoodSnaps
    {
        private const string Anchor = "snap_";
        // The stakewall's slanted back brace collider is tagged leaky, so a roof check looks past it; the walls' second
        // box is that brace.
        private const string Brace = "col_box_wall_01";

        public static void Lift(Transform root, Transform placed)
        {
            int bottom = 0, mid = 0;
            foreach (Transform anchor in Anchors(placed))
            {
                Vector3 local = root.InverseTransformPoint(anchor.position);
                anchor.SetParent(root, false);
                anchor.localPosition = local;
                anchor.localRotation = Quaternion.identity;
                anchor.gameObject.tag = "snappoint";
                anchor.gameObject.SetActive(false);
                anchor.name = local.y < 1f ? $"$hud_snappoint_bottom {++bottom}" : $"$hud_snappoint_mid {++mid}";
            }
            Transform brace = BundlePrefabs.GameMaterials.Find(placed, Brace);
            if (brace != null)
                brace.gameObject.tag = "leaky";
        }

        private static List<Transform> Anchors(Transform placed)
        {
            List<Transform> anchors = new List<Transform>();
            foreach (Transform part in placed.GetComponentsInChildren<Transform>(true))
            {
                if (part.name.StartsWith(Anchor))
                    anchors.Add(part);
            }
            anchors.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return anchors;
        }
    }
}
