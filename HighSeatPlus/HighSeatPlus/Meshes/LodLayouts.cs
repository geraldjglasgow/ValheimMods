using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>One LOD group of a piece kind: its local size and the screen heights at which its levels end.</summary>
    internal struct LodInfo
    {
        public float Size;
        public float[] Heights;
    }

    /// <summary>How the renderers of one piece kind sit in its LOD groups, in GetComponentsInChildren order. A live
    /// piece can have more renderers than its prefab (hit and switch effects are parented to the piece and come last
    /// in that order), never fewer; the mesh of each renderer is kept so a piece whose order differs is caught.</summary>
    internal sealed class LodLayout
    {
        public int RendererCount;
        // Per renderer: a bit for each LOD level it is drawn in, 0 when no LOD group manages it, -1 when it can not be
        // combined (listed by two LOD groups, or by one with more than MaxLevels levels).
        public int[] Levels = Array.Empty<int>();
        public int[] Group = Array.Empty<int>();
        public int[] MeshIds = Array.Empty<int>();
        public LodInfo[] Groups = Array.Empty<LodInfo>();
        // The kind is never combined (PieceRules.OnlySafeComponents said no for its prefab).
        public bool Never;
    }

    /// <summary>
    /// The LOD groups of every piece kind, worked out once per prefab (keyed by the ZDO's prefab hash, read from the
    /// prefab ZNetScene holds) so a piece is never asked for its LODs (GetLODs allocates). Nearly every building piece
    /// has a LOD group of one or two levels: a detailed mesh, often then a simple box, culled once it is a few percent
    /// of the screen high. A level's reach (<see cref="Reach"/>) is its LOD group's world size over the level's screen
    /// height: the distance at which it ends at LOD bias 1 and a unit field of view. Kinds with more than
    /// <see cref="MaxLevels"/> levels (four in the game) are left to the game.
    /// </summary>
    internal static class LodLayouts
    {
        public const int MaxLevels = 2;
        private const float MinHeight = 0.0001f;

        private static readonly Dictionary<int, LodLayout> Layouts = new Dictionary<int, LodLayout>();
        private static readonly List<MeshRenderer> Renderers = new List<MeshRenderer>();
        private static readonly List<LODGroup> Groups = new List<LODGroup>();
        private static readonly Dictionary<Renderer, int> Index = new Dictionary<Renderer, int>();

        /// <summary>The layout of this piece's kind, or null when the piece has no ZDO.</summary>
        public static LodLayout For(WearNTear piece)
        {
            ZDO zdo = piece.m_nview.GetZDO();
            if (zdo == null)
            {
                return null;
            }
            int prefab = zdo.GetPrefab();
            if (Layouts.TryGetValue(prefab, out LodLayout layout))
            {
                return layout;
            }
            GameObject template = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            layout = Build(template != null ? template : piece.gameObject);
            Layouts[prefab] = layout;
            return layout;
        }

        /// <summary>The distance, at LOD bias 1 and a unit field of view, at which a level of this height ends.</summary>
        public static float Reach(float worldSize, float height)
        {
            return worldSize / Mathf.Max(height, MinHeight);
        }

        public static int MeshId(MeshRenderer renderer)
        {
            return renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null ? filter.sharedMesh.GetInstanceID() : 0;
        }

        public static void Clear()
        {
            Layouts.Clear();
        }

        private static LodLayout Build(GameObject template)
        {
            template.GetComponentsInChildren(true, Renderers);
            template.GetComponentsInChildren(true, Groups);
            int count = Renderers.Count;
            LodLayout layout = new LodLayout { RendererCount = count, Levels = new int[count], Group = new int[count] };
            layout.MeshIds = new int[count];
            layout.Groups = new LodInfo[Groups.Count];
            Index.Clear();
            for (int i = 0; i < count; i++)
            {
                Index[Renderers[i]] = i;
                layout.Group[i] = -1;
                layout.MeshIds[i] = MeshId(Renderers[i]);
            }
            for (int g = 0; g < Groups.Count; g++)
            {
                AddGroup(layout, g, Groups[g]);
            }
            layout.Never = !PieceRules.OnlySafeComponents(template);
            Index.Clear();
            Renderers.Clear();
            Groups.Clear();
            return layout;
        }

        private static void AddGroup(LodLayout layout, int g, LODGroup lodGroup)
        {
            LOD[] lods = lodGroup.GetLODs();
            float[] heights = new float[lods.Length];
            for (int level = 0; level < lods.Length; level++)
            {
                heights[level] = lods[level].screenRelativeTransitionHeight;
                Renderer[] listed = lods[level].renderers ?? Array.Empty<Renderer>();
                for (int r = 0; r < listed.Length; r++)
                {
                    Mark(layout, g, level, lods.Length, listed[r]);
                }
            }
            layout.Groups[g] = new LodInfo { Size = lodGroup.size, Heights = heights };
        }

        private static void Mark(LodLayout layout, int g, int level, int levels, Renderer renderer)
        {
            if (renderer == null || !Index.TryGetValue(renderer, out int i) || layout.Levels[i] < 0)
            {
                return;
            }
            if (levels > MaxLevels || (layout.Group[i] >= 0 && layout.Group[i] != g))
            {
                layout.Levels[i] = -1;
                return;
            }
            layout.Group[i] = g;
            layout.Levels[i] |= 1 << level;
        }
    }
}
