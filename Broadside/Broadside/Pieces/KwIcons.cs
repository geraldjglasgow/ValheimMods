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
        public static int RenderAll(IEnumerable<GameObject> prefabs)
        {
            if (!CanRender)
                return 0;
            int set = 0;
            KwIconRig? rig = null;
            try
            {
                foreach (GameObject prefab in prefabs)
                    set += Apply(ref rig, prefab) ? 1 : 0;
            }
            finally
            {
                rig?.Dispose();
            }
            return set;
        }

        // The painted icon, or one rendered (the camera made on the first piece that needs one).
        private static bool Apply(ref KwIconRig? rig, GameObject prefab)
        {
            Piece? piece = prefab.GetComponent<Piece>();
            Transform? model = prefab.transform.Find("model");
            // the mesh on the model itself, or under it (the blueprint table's workshop model)
            MeshFilter? filter = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            if (piece == null || filter == null || filter.sharedMesh == null)
                return false;
            Sprite? icon = KwPaintedIcons.For(prefab.name);
            if (icon == null)
                icon = Rendered(ref rig, prefab);
            if (icon != null)
                piece.m_icon = icon;
            return icon != null;
        }

        private static Sprite? Rendered(ref KwIconRig? rig, GameObject prefab)
        {
            try
            {
                rig ??= new KwIconRig();
                Sprite? icon = rig.Shoot(prefab.transform, prefab.name);
                if (icon == null)
                    Log.Warn($"Broadside: nothing of {prefab.name} draws; it keeps its icon.");
                return icon;
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the icon of {prefab.name} could not be rendered ({e.Message}); it keeps its icon.");
                return null;
            }
        }
    }
}
