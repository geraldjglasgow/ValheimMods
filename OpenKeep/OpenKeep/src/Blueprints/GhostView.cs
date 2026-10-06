using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The preview of a blueprint: plain copies of its pieces' looks (<see cref="PieceShapes"/>) under one root that
    /// stands at the frame, so moving or turning the blueprint moves one transform. Only this machine draws them:
    /// nothing is networked, nothing collides. Copies are made within a time budget each frame (<see cref="FrameBudget"/>); a blueprint of more than
    /// <see cref="BlueprintRules.FullPreviewLimit"/> pieces shows only what stands near the ground.
    /// </summary>
    public static class GhostView
    {
        private static GameObject root;
        private static Blueprint shown;
        private static List<BlueprintPiece> todo = new List<BlueprintPiece>();
        private static int made;

        /// <summary>The blueprint shown is large and only its ground floor is drawn.</summary>
        public static bool OutlineOnly { get; private set; }

        public static void Show(Blueprint bp, BuildFrame frame)
        {
            if (bp != shown || root == null)
                Rebuild(bp);
            root.transform.SetPositionAndRotation(frame.Origin, frame.Rotation);
            if (!root.activeSelf)
                root.SetActive(true);
            double until = FrameBudget.Until();
            while (made < todo.Count && FrameBudget.Left(until))
                Copy(todo[made++]);
        }

        public static void Hide()
        {
            if (root != null && root.activeSelf)
                root.SetActive(false);
        }

        /// <summary>Destroys every copy (the blueprint changed on disk, or the player left the world).</summary>
        public static void Clear()
        {
            if (root != null)
                Object.Destroy(root);
            root = null;
            shown = null;
            todo.Clear();
            made = 0;
        }

        private static void Rebuild(Blueprint bp)
        {
            Clear();
            shown = bp;
            root = new GameObject("OpenKeep Blueprint Preview");
            OutlineOnly = bp.Pieces.Count > BlueprintRules.FullPreviewLimit;
            todo = OutlineOnly ? bp.Pieces.FindAll(p => p.Y <= BlueprintRules.OutlineHeight) : new List<BlueprintPiece>(bp.Pieces);
        }

        private static void Copy(BlueprintPiece p)
        {
            PieceShape shape = PieceShapes.Of(p.Prefab);
            if (shape?.Template == null)
                return;
            GameObject copy = Object.Instantiate(shape.Template, root.transform, false);
            copy.transform.localPosition = new Vector3(p.X, p.Y, p.Z);
            copy.transform.localRotation = Quaternion.Euler(0f, p.Yaw, 0f);
        }
    }
}
