using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// Whether a piece still looks exactly as it did when its parts were copied: the same new, worn or broken look,
    /// no MaterialMan value and no foreign property block, and every part still there, switched on, shown, turned off
    /// only if this mod turned it off, with the same mesh, the same materials and the same place in the world. Used
    /// before a hidden build is shown again without rebuilding, before a member is copied into a new build, and by
    /// <see cref="CellGroups.Sweep"/>, the slow pass that catches what no patch sees (another mod swapping a material,
    /// moving a piece or switching a child off). All reads are property reads; the material list is reused.
    /// </summary>
    internal static class PartChecks
    {
        private static readonly List<Material> Current = new List<Material>();

        public static bool Valid(PieceEntry entry)
        {
            WearNTear piece = entry.Piece;
            if (entry.Removed || piece == null || entry.Parts.Count == 0)
            {
                return false;
            }
            if (PieceRules.StateBits(piece) != entry.StateBits || PieceRules.LooksChanged(piece))
            {
                return false;
            }
            for (int i = 0; i < entry.Parts.Count; i++)
            {
                Part part = entry.Parts[i];
                if (!Valid(entry, piece, in part))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool Valid(PieceEntry entry, WearNTear piece, in Part part)
        {
            MeshRenderer renderer = part.Renderer;
            if (renderer == null || !renderer.enabled || renderer.forceRenderingOff != entry.Forced)
            {
                return false;
            }
            if (!renderer.gameObject.activeInHierarchy || part.Filter == null || !ReferenceEquals(part.Filter.sharedMesh, part.Mesh))
            {
                return false;
            }
            if (renderer.localToWorldMatrix != part.Matrix || PieceRules.ForeignBlock(piece, renderer))
            {
                return false;
            }
            return SameMaterials(entry, in part, renderer);
        }

        private static bool SameMaterials(PieceEntry entry, in Part part, MeshRenderer renderer)
        {
            renderer.GetSharedMaterials(Current);
            bool same = Current.Count == part.MaterialCount;
            for (int i = 0; same && i < Current.Count; i++)
            {
                same = ReferenceEquals(Current[i], entry.Materials[part.MaterialStart + i]);
            }
            Current.Clear();
            return same;
        }
    }
}
