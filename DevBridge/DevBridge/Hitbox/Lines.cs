using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DevBridge.Hitbox
{
    /// <summary>
    /// Short-lived world-space lines, drawn over everything (no depth test) so a hit shape shows through the creature and
    /// the ground. They live under one root in the world scene, so logging out removes them.
    /// </summary>
    internal static class Lines
    {
        private static GameObject root;
        private static Material material;

        internal static void Draw(IList<Vector3> points, Color color, float seconds, float width = 0.04f)
        {
            if (points.Count < 2) return;
            var go = new GameObject("hitbox");
            go.transform.SetParent(Root(), false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            (line.useWorldSpace, line.sharedMaterial, line.widthMultiplier) = (true, Material(), width);
            (line.startColor, line.endColor) = (color, color);
            (line.shadowCastingMode, line.receiveShadows) = (ShadowCastingMode.Off, false);
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);
            Object.Destroy(go, seconds);
        }

        /// <summary>A flat circle round a point, for a body's outline on the ground.</summary>
        internal static Vector3[] Ring(Vector3 centre, float radius, int steps = 24)
        {
            var points = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float angle = i * Mathf.PI * 2f / steps;
                points[i] = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }
            return points;
        }

        /// <summary>The same over-everything line material, for lines kept and updated elsewhere (/overlay's pool).</summary>
        internal static Material Shared => Material();

        private static Transform Root()
        {
            if (!root) root = new GameObject("DevBridge_Hitbox");
            return root.transform;
        }

        // The engine's own line shader, which the game build keeps: vertex colours, alpha blended, depth test off.
        private static Material Material()
        {
            if (material) return material;
            material = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave, renderQueue = 5000 };
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_ZTest", (int)CompareFunction.Always);
            return material;
        }
    }
}
