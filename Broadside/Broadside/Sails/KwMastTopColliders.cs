using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a launched ship's crew stands on aloft (<see cref="KwShipColliders"/> has the rest): each mast's crow's nest and
    /// fighting top (<see cref="KwMastTops"/>) as boxes, since a moving ship's colliders must be convex. A crow's nest's floor
    /// is two square slabs, one turned an eighth round, which together cover its round floor; its rail a ring of upright
    /// boards. A fighting top's floor is one slab over its outline's box; its rail or wall upright boards along its open
    /// sides. Built from the masts' saved texts in the ship's frame, on the ship's own layer, on every peer; each box knows
    /// its mast by its index in the blueprint (<see cref="KwMastTopOf"/>), so a mast changed at sea has its own taken away
    /// and made again (<see cref="Remove"/>, <see cref="KwSeaRigLayers"/>).
    /// </summary>
    public static class KwMastTopColliders
    {
        private const float Thin = 0.06f;

        public static void Add(Transform holder, KwBlueprint blueprint)
        {
            for (int i = 0; i < blueprint.Pieces.Count; i++)
                Add(holder, blueprint.Pieces[i], i);
        }

        /// <summary>One mast's platforms' boxes (nothing for any other piece, or a mast with none).</summary>
        public static void Add(Transform holder, KwBpPiece piece, int index)
        {
            if (piece.Prefab != KwSailTool.Made || !KwMastSpec.TryParse(KwShipParts.Saved(piece), out KwMastSpec mast))
                return;
            int first = holder.childCount;
            if (mast.Tops.Nest)
                Nest(holder, piece, mast);
            if (mast.Tops.HasTop)
                Top(holder, piece, mast);
            for (int i = first; i < holder.childCount; i++)
                holder.GetChild(i).gameObject.AddComponent<KwMastTopOf>().Index = index;
        }

        /// <summary>A mast's platforms' boxes taken away at once (the ship's body loses their colliders this frame).</summary>
        public static void Remove(Transform holder, int index)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                Transform child = holder.GetChild(i);
                if (child.TryGetComponent(out KwMastTopOf of) && of.Index == index)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void Nest(Transform holder, KwBpPiece piece, KwMastSpec mast)
        {
            float at = mast.Tops.NestAt, rn = KwMastTops.NestRadius(mast.Mast);
            foreach (float turn in new[] { 0f, 45f })
                Box(holder, piece, new Vector3(0f, 0f, at - 0.05f), new Vector3(1.9f * rn, 0.1f, 1.9f * rn), turn);
            List<Vector2> rail = KwMastTopShapes.Circle(rn - 0.04f, 8);
            rail.Add(rail[0]);
            Boards(holder, piece, rail, at, KwMastTops.NestRail);
        }

        private static void Top(Transform holder, KwBpPiece piece, KwMastSpec mast)
        {
            float at = mast.Tops.TopAt;
            List<Vector2> outline = KwMastTopShapes.Outline(mast, mast.Tops);
            Vector2 min = outline[0], max = outline[0];
            foreach (Vector2 p in outline)
                (min, max) = (Vector2.Min(min, p), Vector2.Max(max, p));
            Vector2 mid = (min + max) / 2f, size = max - min;
            Box(holder, piece, new Vector3(mid.x, mid.y, at - 0.05f), new Vector3(size.y, 0.1f, size.x), 0f);
            Boards(holder, piece, outline, at, mast.Tops.Top == 1 ? KwMastTops.TopRail : 0.65f);
        }

        // An upright board along each segment of a line seen from above, from a floor up by a height.
        private static void Boards(Transform holder, KwBpPiece piece, List<Vector2> line, float floor, float high)
        {
            for (int i = 0; i + 1 < line.Count; i++)
            {
                Vector2 p = line[i], q = line[i + 1], run = q - p;
                float yaw = Mathf.Atan2(-run.y, run.x) * Mathf.Rad2Deg;
                Vector3 centre = new Vector3((p.x + q.x) / 2f, (p.y + q.y) / 2f, floor + high / 2f);
                Box(holder, piece, centre, new Vector3(Thin, high, run.magnitude + 0.04f), yaw);
            }
        }

        // A box at a kit point of the mast piece, sized in Unity axes (across, up, along), turned about the up axis.
        private static void Box(Transform holder, KwBpPiece piece, Vector3 kit, Vector3 size, float yaw)
        {
            GameObject box = new GameObject("top") { layer = holder.gameObject.layer };
            box.transform.SetParent(holder, false);
            box.transform.SetLocalPositionAndRotation(piece.Position + piece.Rotation * KwMesh.ToUnity(kit), piece.Rotation * Quaternion.Euler(0f, yaw, 0f));
            box.AddComponent<BoxCollider>().size = size;
        }
    }

    /// <summary>Which mast a platform's box belongs to: its index in the ship's blueprint.</summary>
    public sealed class KwMastTopOf : MonoBehaviour
    {
        public int Index = -1;
    }
}
