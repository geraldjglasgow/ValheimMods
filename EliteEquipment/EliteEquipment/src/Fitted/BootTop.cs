using System.Collections.Generic;
using EliteEquipment.Boots;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// How high a pair of boots reaches on a body, in metres: the top of its worn meshes (the boots' cut of their
    /// leggings, in the body's bind space) or of its paint on the body (the highest body vertex its paint covers),
    /// whichever is higher; 0 for no boots. Worked out once per body and pair.
    /// </summary>
    internal static class BootTop
    {
        private static readonly Dictionary<(Mesh, BootSet), float> known = new Dictionary<(Mesh, BootSet), float>();

        public static float Of(BodySurface body, BootSet boots)
        {
            if (boots == null)
                return 0f;
            if (!known.TryGetValue((body.Mesh, boots), out float top))
                known[(body.Mesh, boots)] = top = Mathf.Max(MeshTop(boots), PaintTop(body, boots));
            return top;
        }

        private static float MeshTop(BootSet boots)
        {
            Transform skin = boots.Item != null ? boots.Item.transform.Find(SkinSplit.Skin) : null;
            float top = 0f;
            if (skin == null)
                return top;
            foreach (SkinnedMeshRenderer part in skin.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                top = Mathf.Max(top, Highest(part.sharedMesh));
            return top;
        }

        /// <summary>The highest point the mesh's triangles reach (its z is up in bind space); 0 when it cannot be read.</summary>
        private static float Highest(Mesh mesh)
        {
            if (mesh == null || !mesh.isReadable)
                return 0f;
            Vector3[] vertices = mesh.vertices;
            float top = 0f;
            foreach (int i in mesh.triangles)
                top = Mathf.Max(top, vertices[i].z * BodySurface.Scale);
            return top;
        }

        private static float PaintTop(BodySurface body, BootSet boots)
        {
            Paint paint = PaintCut.Boots(boots);
            float top = 0f;
            for (int i = 0; paint != null && i < body.Uvs.Length; i++)
            {
                if (!body.Upper[i] && Painted(paint, body.Uvs[i]))
                    top = Mathf.Max(top, body.Rest[i].y);
            }
            return top;
        }

        private static bool Painted(Paint paint, Vector2 uv)
        {
            int x = Mathf.Clamp((int)(uv.x * paint.Width), 0, paint.Width - 1);
            int y = Mathf.Clamp((int)((1f - uv.y) * paint.Height), 0, paint.Height - 1);
            return paint.Pixels[paint.Index(x, y)].a > 8;
        }
    }
}
