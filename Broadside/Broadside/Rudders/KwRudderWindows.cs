using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The part of a look's texture a rudder's part shows. Most looks tile (the planks, the table's fine wood, the dark
    /// and ash woods, the carved wood, the metals) and keep the kit's planar UVs, two metres a texture. Three are an
    /// item's atlas: the ancient bark's (bark on its left half, split logs on the right), the Yggdrasil wood's (its bark,
    /// then a band of moss) and the leather scraps' (a scrap on black). The planar UVs run negative aft of the stock and
    /// wrap onto the logs, the moss or the black, so a part in one of those shows its texture's clean window instead,
    /// spread over the part: across it along the level axis its face looks least along, up it by height (measured from
    /// the game's textures).
    /// </summary>
    public static class KwRudderWindows
    {
        private static readonly Dictionary<KwLook, Rect> Windows = new Dictionary<KwLook, Rect>
        {
            [KwLook.Bark] = Rect.MinMaxRect(0.02f, 0.08f, 0.55f, 0.9f),
            [KwLook.Yggdrasil] = Rect.MinMaxRect(0.02f, 0.02f, 0.66f, 0.98f),
            [KwLook.Leather] = Rect.MinMaxRect(0.3f, 0.35f, 0.65f, 0.8f),
        };

        /// <summary>A part's mesh in a look with a window: its UVs put in the window; any other look's left as made.</summary>
        public static void Fit(Mesh mesh, KwLook look)
        {
            if (!Windows.TryGetValue(look, out Rect window))
                return;
            Vector3[] points = mesh.vertices, normals = mesh.normals;
            Bounds box = mesh.bounds;
            Vector2[] uvs = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
                uvs[i] = Spot(points[i], normals[i], box, window);
            mesh.uv = uvs;
            mesh.RecalculateTangents();
        }

        // Unity axes: a face looking sideways (x) or up or down runs across along z, one looking fore or aft along x;
        // up is the height, or for a face looking up or down, x.
        private static Vector2 Spot(Vector3 p, Vector3 n, Bounds box, Rect window)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            float across = az > ax && az >= ay ? Share(p.x, box.min.x, box.max.x) : Share(p.z, box.min.z, box.max.z);
            float up = ay >= ax && ay >= az ? Share(p.x, box.min.x, box.max.x) : Share(p.y, box.min.y, box.max.y);
            return new Vector2(Mathf.Lerp(window.xMin, window.xMax, across), Mathf.Lerp(window.yMin, window.yMax, up));
        }

        private static float Share(float value, float min, float max) => max - min > 1e-4f ? (value - min) / (max - min) : 0.5f;
    }
}
