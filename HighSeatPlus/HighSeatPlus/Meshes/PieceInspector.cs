using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// Looks at a settled piece and decides whether it is combined, and with what: every shown mesh renderer that
    /// <see cref="RenderRules"/> accepts becomes a part, with the LOD levels its LOD group draws it in. A renderer a LOD
    /// group manages must be accepted or the whole piece stays with the game: the game would switch that renderer by
    /// the piece's own distance and the combined mesh by the group's, and the two would show at once or not at all. A
    /// renderer no LOD group manages can be left out on its own and keeps drawing itself (an overlay, a renderer someone
    /// else switched off, another mod's property block, a shader that can not be merged). A mesh that is not readable
    /// can not be copied: the dark wood roofs, the ashwood floors and stairs and some furniture ship without a CPU copy,
    /// so they stay with the game. A piece's parts must all be LOD-managed or all not, so it belongs to one group; the
    /// one kind that mixes both stays with the game. The piece's reaches (<see cref="LodLayouts.Reach"/>: where its
    /// detailed level ends and where it is culled) are kept for the group's LOD heights. Each part keeps its own
    /// renderer settings (<see cref="RenderRules.LookBits"/>): the game's LOD levels differ in layer and shadow casting
    /// within one piece. The renderer lists are reused, so a look allocates nothing but the piece's first parts.
    /// </summary>
    internal static class PieceInspector
    {
        private static readonly List<MeshRenderer> Renderers = new List<MeshRenderer>();
        private static readonly List<LODGroup> LodGroups = new List<LODGroup>();
        private static readonly List<Material> Found = new List<Material>();
        // The first part set the entry's key.
        private static bool _keyed;
        // A renderer was left out for a passing reason (switched off, hidden, another mod's property block).
        private static bool _passing;

        /// <summary>Fills the entry's parts, key, LOD point and state when the answer is Combine. The entry's parts
        /// must not be turned off when it is called.</summary>
        public static Verdict Inspect(PieceEntry entry)
        {
            WearNTear piece = entry.Piece;
            if (piece == null || entry.Removed || piece.m_nview == null || !piece.m_nview.IsValid())
            {
                return Verdict.Never;
            }
            LodLayout layout = LodLayouts.For(piece);
            if (layout == null || layout.Never)
            {
                return Verdict.Never;
            }
            Verdict verdict = PieceRules.CheckPiece(piece);
            return verdict == Verdict.Combine ? Collect(entry, piece, layout) : verdict;
        }

        private static Verdict Collect(PieceEntry entry, WearNTear piece, LodLayout layout)
        {
            piece.GetComponentsInChildren(true, Renderers);
            piece.GetComponentsInChildren(true, LodGroups);
            entry.Parts.Clear();
            entry.Materials.Clear();
            entry.DetailReach = 0f;
            entry.CullReach = 0f;
            _keyed = false;
            _passing = false;
            bool fewer = Renderers.Count < layout.RendererCount || LodGroups.Count < layout.Groups.Length;
            Verdict verdict = fewer ? Verdict.Never : Verdict.Combine;
            for (int i = 0; i < layout.RendererCount && verdict == Verdict.Combine; i++)
            {
                verdict = AddRenderer(entry, piece, layout, i);
            }
            Renderers.Clear();
            LodGroups.Clear();
            return Conclude(entry, piece, verdict);
        }

        // No part at all: later when a renderer was out for a passing reason, otherwise never.
        private static Verdict Conclude(PieceEntry entry, WearNTear piece, Verdict verdict)
        {
            if (verdict == Verdict.Combine && entry.Parts.Count > 0)
            {
                Finish(entry, piece);
                return Verdict.Combine;
            }
            entry.Parts.Clear();
            entry.Materials.Clear();
            if (verdict != Verdict.Combine)
            {
                return verdict;
            }
            return _passing ? Verdict.Later : Verdict.Never;
        }

        // Combine means "carry on": the renderer became a part or is left to draw itself.
        private static Verdict AddRenderer(PieceEntry entry, WearNTear piece, LodLayout layout, int i)
        {
            MeshRenderer renderer = Renderers[i];
            int levels = layout.Levels[i];
            if (!renderer.gameObject.activeInHierarchy)
            {
                return Verdict.Combine;
            }
            bool hidden = !renderer.enabled || renderer.forceRenderingOff || PieceRules.ForeignBlock(piece, renderer);
            if (hidden || PieceRules.IsOverlay(piece, renderer))
            {
                _passing |= hidden;
                return levels == 0 ? Verdict.Combine : Verdict.Later;
            }
            if (levels < 0 || layout.MeshIds[i] != LodLayouts.MeshId(renderer))
            {
                return levels == 0 ? Verdict.Combine : Verdict.Never;
            }
            if (RenderRules.Check(renderer, Found, out MeshFilter filter) != Verdict.Combine)
            {
                return levels == 0 ? Verdict.Combine : Verdict.Never;
            }
            return AddPart(entry, layout, i, renderer, filter, levels);
        }

        private static Verdict AddPart(PieceEntry entry, LodLayout layout, int i, MeshRenderer renderer, MeshFilter filter, int levels)
        {
            int lod = levels == 0 ? 0 : 1;
            if (_keyed && entry.Key.Lod != lod)
            {
                return Verdict.Never;
            }
            int drawn = levels == 0 ? 1 : Reaches(entry, layout, i, levels);
            entry.Key = new GroupKey(0, 0, 0, lod);
            _keyed = true;
            entry.Parts.Add(new Part
            {
                Renderer = renderer, Filter = filter, Mesh = filter.sharedMesh, Matrix = renderer.localToWorldMatrix,
                Levels = drawn, Look = RenderRules.LookBits(renderer),
                MaterialStart = entry.Materials.Count, MaterialCount = Found.Count
            });
            entry.Materials.AddRange(Found);
            return Verdict.Combine;
        }

        // The reaches of the renderer's LOD group at its scale, kept as the largest over the piece's parts; the first
        // LOD part also gives the point the piece's own LOD distance is measured from. A renderer of a one-level LOD
        // group is drawn in both levels of the combined group (it has no simpler look of its own).
        private static int Reaches(PieceEntry entry, LodLayout layout, int i, int levels)
        {
            int g = layout.Group[i];
            LODGroup lodGroup = LodGroups[g];
            Vector3 scale = lodGroup.transform.lossyScale;
            float size = layout.Groups[g].Size * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            float[] heights = layout.Groups[g].Heights;
            if (!_keyed)
            {
                entry.LodPoint = lodGroup.transform.TransformPoint(lodGroup.localReferencePoint);
            }
            entry.CullReach = Mathf.Max(entry.CullReach, LodLayouts.Reach(size, heights[heights.Length - 1]));
            if (heights.Length == 1)
            {
                return 3;
            }
            entry.DetailReach = Mathf.Max(entry.DetailReach, LodLayouts.Reach(size, heights[0]));
            return levels;
        }

        private static void Finish(PieceEntry entry, WearNTear piece)
        {
            Vector3 position = piece.transform.position;
            if (entry.Key.Lod == 0)
            {
                entry.LodPoint = position;
            }
            float size = CellGroups.CellSize;
            int x = Mathf.FloorToInt(position.x / size);
            int y = Mathf.FloorToInt(position.y / size);
            int z = Mathf.FloorToInt(position.z / size);
            entry.Key = new GroupKey(x, y, z, entry.Key.Lod);
            entry.StateBits = PieceRules.StateBits(piece);
        }
    }
}
