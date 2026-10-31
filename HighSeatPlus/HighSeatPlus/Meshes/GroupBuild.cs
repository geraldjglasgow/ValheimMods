using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>How a build's LOD group is laid out: its level count (0 for a group without one), the largest detail
    /// and cull reaches of its pieces, the middle of their own LOD reference points and the radius around it.</summary>
    internal struct GroupShape
    {
        public int Levels;
        public float Detail;
        public float Cull;
        public float Radius;
        public Vector3 Center;
    }

    /// <summary>
    /// The combined meshes of one group as drawn: a root object at the cell's centre (created switched off, shown by
    /// <see cref="SetShown"/> in the same frame its pieces are turned off) with one renderer per LOD level, material,
    /// vertex channel set and renderer settings, and for LOD-managed pieces a LOD group of size 1 whose reference point
    /// is the middle of the pieces' own (<see cref="GroupLod"/> sets its heights). The meshes are this mod's own and
    /// destroyed with the build; the objects have no colliders, so nothing that casts rays or looks for pieces ever
    /// meets them.
    /// </summary>
    internal sealed class GroupBuild
    {
        private const string RootName = "HighSeatPlus combined";
        private const string PartName = "combined part";

        public readonly GameObject Root;
        public readonly int Vertices;

        private readonly List<Mesh> _meshes;
        private readonly List<MeshRenderer> _renderers;
        private readonly LODGroup _lod;
        private readonly LOD[] _lods;
        private readonly GroupShape _shape;

        public GroupBuild(GameObject root, List<Mesh> meshes, List<MeshRenderer> renderers, LODGroup lod, LOD[] lods, GroupShape shape, int vertices)
        {
            Root = root;
            _meshes = meshes;
            _renderers = renderers;
            _lod = lod;
            _lods = lods;
            _shape = shape;
            Vertices = vertices;
        }

        /// <summary>A switched-off root object for a group at <paramref name="origin"/>.</summary>
        public static GameObject MakeRoot(Vector3 origin)
        {
            GameObject root = new GameObject(RootName);
            root.SetActive(false);
            if (CombinedMeshes.Container != null)
            {
                root.transform.SetParent(CombinedMeshes.Container, false);
            }
            root.transform.position = origin;
            return root;
        }

        /// <summary>A renderer under the root drawing <paramref name="mesh"/> with <paramref name="material"/>.</summary>
        public static MeshRenderer MakeRenderer(GameObject root, Mesh mesh, Material material, int look)
        {
            GameObject part = new GameObject(PartName);
            part.transform.SetParent(root.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            RenderRules.Apply(renderer, look);
            return renderer;
        }

        public void SetShown(bool shown)
        {
            if (Root != null && Root.activeSelf != shown)
            {
                Root.SetActive(shown);
            }
        }

        /// <summary>Whether any camera drew this build last frame (shadows count, as for the game's own pieces).</summary>
        public bool Visible()
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                if (_renderers[i] != null && _renderers[i].isVisible)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateLod()
        {
            if (_lod == null || _lods == null)
            {
                return;
            }
            GroupLod.Fill(_lods, _shape.Detail, _shape.Cull, _shape.Radius);
            _lod.SetLODs(_lods);
        }

        public void Destroy()
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                if (_meshes[i] != null)
                {
                    Object.Destroy(_meshes[i]);
                }
            }
            _meshes.Clear();
            if (Root != null)
            {
                Object.Destroy(Root);
            }
        }
    }
}
