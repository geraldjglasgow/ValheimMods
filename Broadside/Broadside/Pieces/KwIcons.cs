using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// Hammer icons for the kit's pieces, set when the pieces are made (and when a saved ship's hammer entry gets its
    /// look). A piece with a painted icon wears it (<see cref="KwPaintedIcons"/>: the tools, the blueprint table, the keel
    /// block and its guide, the hatches, the post caps and the rudders); any other (a saved ship) gets
    /// one rendered from its own drawn meshes and materials (<see cref="KwIconRig"/>). Clients only: a dedicated server
    /// has no graphics device. A piece whose icon fails keeps the one it has (the wood floor's), and a failure never
    /// stops the pieces being made.
    /// </summary>
    public static class KwIcons
    {
        public static bool CanRender => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        /// <summary>Sets the icon of every prefab that has a "model" child with a mesh; returns how many were set.</summary>
        public static int RenderAll(IEnumerable<GameObject> prefabs) => RenderAll(prefabs, null);

        /// <summary>
        /// As <see cref="RenderAll(IEnumerable{GameObject})"/>, the icons kept by <see cref="KwIconCache"/> under this
        /// source: what they are drawn from besides this build of the mod (a saved ship's file), null for none.
        /// </summary>
        public static int RenderAll(IEnumerable<GameObject> prefabs, string? source)
        {
            if (!CanRender)
                return 0;
            int set = 0;
            KwIconRig? rig = null;
            try
            {
                foreach (GameObject prefab in prefabs)
                    set += Apply(ref rig, prefab, source) ? 1 : 0;
            }
            finally
            {
                rig?.Dispose();
            }
            return set;
        }

        // The painted icon, or one kept or rendered (the camera made on the first piece that needs one).
        private static bool Apply(ref KwIconRig? rig, GameObject prefab, string? source)
        {
            Piece? piece = prefab.GetComponent<Piece>();
            Transform? model = prefab.transform.Find("model");
            // the mesh on the model itself, or under it (the blueprint table's workshop model)
            MeshFilter? filter = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            if (piece == null || filter == null || filter.sharedMesh == null)
                return false;
            Sprite? icon = KwPaintedIcons.For(prefab.name);
            if (icon == null)
                icon = Icon(ref rig, prefab.transform, prefab.name, source);
            if (icon != null)
                piece.m_icon = icon;
            return icon != null;
        }

        /// <summary>
        /// An icon of what draws under a prefab: the one kept from an earlier launch (<see cref="KwIconCache"/>), else
        /// rendered now (the rig made when first needed) and kept; null when nothing draws or the render fails.
        /// </summary>
        public static Sprite? Icon(ref KwIconRig? rig, Transform prefab, string name, string? source)
        {
            Sprite? kept = KwIconCache.Find(name, source);
            if (kept != null)
                return kept;
            try
            {
                rig ??= new KwIconRig();
                Sprite? icon = rig.Shoot(prefab, name, out Color32[]? pixels);
                if (icon == null)
                    Log.Warn($"Broadside: nothing of {name} draws; it keeps its icon.");
                else if (pixels != null)
                    KwIconCache.Keep(name, source, pixels, KwIconRig.Size);
                return icon;
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the icon of {name} could not be rendered ({e.Message}); it keeps its icon.");
                return null;
            }
        }
    }
}
