using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A decoration the tool may place: its definition and metadata (<see cref="KwDockDef.Decor"/>), the piece made from it,
    /// and its solid boxes (the piece's own non-trigger box colliders but the placing box, each as a matrix from the piece's
    /// root, read once) for the overlap test. Only dockside decorations with metadata whose piece was made are offered.
    /// </summary>
    public sealed class KwDecorKind
    {
        private static readonly List<KwDecorKind> kinds = new List<KwDecorKind>();
        private static int madeCount = -1;
        private List<(Matrix4x4 Pose, Vector3 Centre, Vector3 Size)>? solids;

        private KwDecorKind(KwDockDef def, GameObject prefab)
        {
            (Def, Fit, Prefab, Piece) = (def, def.Decor!, prefab, prefab.GetComponent<Piece>());
        }

        public KwDockDef Def { get; }
        public KwDecorFit Fit { get; }
        public GameObject Prefab { get; }
        public Piece Piece { get; }

        /// <summary>The prefab's name.</summary>
        public string Name => Def.Prefab;

        /// <summary>A decoration by its id (the prefab without <c>BS_dock_</c>); null when it is not in the game (yet).</summary>
        public static KwDecorKind? Find(string id)
        {
            string prefab = KwDockside.Prefix + id;
            foreach (KwDecorKind kind in All())
            {
                if (kind.Name == prefab)
                    return kind;
            }
            return null;
        }

        /// <summary>The decorations with metadata, in the hammer's order; gathered again when more are made.</summary>
        public static IReadOnlyList<KwDecorKind> All()
        {
            if (KwDockside.All.Count == madeCount)
                return kinds;
            kinds.Clear();
            foreach (GameObject prefab in KwDockside.All)
            {
                KwDockDef? def = Array.Find(KwDockside.Defs, d => d.Prefab == prefab.name);
                if (def?.Decor != null && prefab.GetComponent<Piece>() != null)
                    kinds.Add(new KwDecorKind(def, prefab));
            }
            madeCount = KwDockside.All.Count;
            return kinds;
        }

        /// <summary>The solid boxes, each as (pose from the root, its centre, its size).</summary>
        public List<(Matrix4x4 Pose, Vector3 Centre, Vector3 Size)> Solids => solids ??= Read();

        private List<(Matrix4x4, Vector3, Vector3)> Read()
        {
            List<(Matrix4x4, Vector3, Vector3)> boxes = new List<(Matrix4x4, Vector3, Vector3)>();
            Matrix4x4 root = Prefab.transform.worldToLocalMatrix;
            foreach (BoxCollider box in Prefab.GetComponentsInChildren<BoxCollider>(true))
            {
                if (!box.isTrigger && box.enabled && box.gameObject.name != KwPrefabs.PlaceName)
                    boxes.Add((root * box.transform.localToWorldMatrix, box.center, box.size));
            }
            return boxes;
        }
    }
}
