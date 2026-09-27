using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The two materials every preview visual shares: one for lines, one for see-through surfaces (points, volume,
    /// grid). Both use a shader that ships with the game (Sprites/Default is in the game's built-in extras, the
    /// internal coloured shader in the default resources) and take their colour from the vertices. Created once and
    /// kept across scene changes; when no candidate shader exists the visuals are simply not drawn.
    /// </summary>
    internal static class PreviewMaterials
    {
        private static readonly string[] Candidates = { "Sprites/Default", "Hidden/Internal-Colored", "UI/Default" };

        private static Material lines;
        private static Material surfaces;
        private static bool warned;

        /// <summary>Material for LineRenderers, drawn after the see-through surfaces. Null when no shader was found.</summary>
        public static Material Lines => Get(ref lines, 3110);

        /// <summary>Material for see-through meshes. Null when no shader was found.</summary>
        public static Material Surfaces => Get(ref surfaces, 3100);

        private static Material Get(ref Material material, int queue)
        {
            if (material != null)
                return material;
            Shader shader = FindShader();
            if (shader == null)
                return null;
            material = new Material(shader) { name = "EarthWright preview", renderQueue = queue, hideFlags = HideFlags.HideAndDontSave };
            Configure(material);
            return material;
        }

        private static Shader FindShader()
        {
            foreach (string name in Candidates)
            {
                Shader shader = Shader.Find(name);
                if (shader != null)
                    return shader;
            }
            if (!warned)
                Plugin.Log.LogWarning("No shader for the terrain preview found; the outline, points and volume are not drawn.");
            warned = true;
            return null;
        }

        /// <summary>The internal coloured shader needs blending and culling set; Sprites/Default ignores these.</summary>
        private static void Configure(Material material)
        {
            material.color = Color.white;
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_Cull"))
                material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            if (material.HasProperty("_ZWrite"))
                material.SetInt("_ZWrite", 0);
        }
    }
}
