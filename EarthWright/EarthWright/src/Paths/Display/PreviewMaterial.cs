using UnityEngine;
using UnityEngine.Rendering;

namespace EarthWright.Paths
{
    /// <summary>
    /// The unlit, vertex-coloured, transparent material of the ramp and road preview. "Through the ground" uses the
    /// UI shader with its depth test switched off, so a line that cuts into a hill stays visible; otherwise the sprite
    /// shader, drawn where it is not hidden. Both shaders ship with every Unity build; when neither is found, the
    /// particle shaders are tried, and null (nothing drawn) is the last resort.
    /// </summary>
    public static class PreviewMaterial
    {
        private static Material onTop;
        private static Material inWorld;
        private static bool searched;

        public static Material Get() => PathSettings.PreviewOnTop.Value ? OnTop() ?? InWorld() : InWorld();

        private static Material OnTop()
        {
            if (onTop != null)
                return onTop;
            Shader shader = Shader.Find("UI/Default");
            if (shader == null)
                return null;
            onTop = new Material(shader) { name = "EarthWright.PathPreview.OnTop", renderQueue = 4000 };
            onTop.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            return onTop;
        }

        private static Material InWorld()
        {
            if (inWorld != null || searched)
                return inWorld;
            searched = true;
            Shader shader = Find("Sprites/Default", "Particles/Standard Unlit", "Legacy Shaders/Particles/Alpha Blended");
            if (shader == null)
            {
                Plugin.Log.LogWarning("No unlit shader found for the ramp and road preview; the preview lines are not drawn.");
                return null;
            }
            inWorld = new Material(shader) { name = "EarthWright.PathPreview", renderQueue = 3100 };
            return inWorld;
        }

        private static Shader Find(params string[] names)
        {
            foreach (string name in names)
            {
                Shader shader = Shader.Find(name);
                if (shader != null)
                    return shader;
            }
            return null;
        }
    }
}
