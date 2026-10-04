using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>The resources every gate's sheet shares: one material on an unlit, alpha-blended, two-sided shader the
    /// game already has loaded, and a small tileable ripple texture made here, so nothing ships as an asset. Made on
    /// first use, destroyed on world unload.</summary>
    internal static class SeaGateSurfaceLook
    {
        private const int TextureSize = 64;
        private const float TwoPi = Mathf.PI * 2f;

        /// <summary>Unlit, alpha-blended, vertex-coloured and without back-face culling, best first.</summary>
        private static readonly string[] ShaderNames =
        {
            "Legacy Shaders/Particles/Alpha Blended",
            "Sprites/Default",
            "UI/Default"
        };

        private static Material material;
        private static Texture2D texture;
        private static bool searched;

        /// <summary>The shared sheet material; null when none of the shaders is loaded.</summary>
        internal static Material SheetMaterial
        {
            get
            {
                if (material == null && !searched)
                {
                    searched = true;
                    material = NewMaterial();
                }
                return material;
            }
        }

        internal static void Forget()
        {
            if (material != null)
                Object.Destroy(material);
            if (texture != null)
                Object.Destroy(texture);
            material = null;
            texture = null;
            searched = false;
        }

        private static Material NewMaterial()
        {
            Shader shader = FindShader();
            if (shader == null)
            {
                Plugin.Log.LogWarning("Sea gate surface: no unlit transparent shader found; gates show only the portal effect.");
                return null;
            }
            texture = NewTexture();
            Material made = new Material(shader) { name = "WF_SeaGateSheet", mainTexture = texture };
            if (made.HasProperty("_TintColor"))
                made.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));  // the legacy particle shaders double it
            if (made.HasProperty("_Color"))
                made.SetColor("_Color", Color.white);
            return made;
        }

        private static Shader FindShader()
        {
            foreach (string name in ShaderNames)
            {
                Shader shader = Shader.Find(name);
                if (shader != null)
                    return shader;
            }
            return null;
        }

        /// <summary>White with a rippling alpha. Every term has whole-number frequencies in u and v, so it tiles.</summary>
        private static Texture2D NewTexture()
        {
            Texture2D made = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: true)
            {
                name = "WF_SeaGateRipple",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[TextureSize * TextureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                float u = (float)(i % TextureSize) / TextureSize;
                float v = (float)(i / TextureSize) / TextureSize;
                float value = Ripple(u, v);
                pixels[i] = new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 1f, value * value));
            }
            made.SetPixels(pixels);
            made.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            return made;
        }

        /// <summary>0 to 1: two warped wave trains crossing, plus a slow diagonal swell.</summary>
        private static float Ripple(float u, float v)
        {
            float a = Mathf.Sin(TwoPi * (2f * u + 0.35f * Mathf.Sin(TwoPi * v)));
            float b = Mathf.Sin(TwoPi * (3f * v + 0.3f * Mathf.Sin(TwoPi * 2f * u)));
            float c = Mathf.Sin(TwoPi * (u + v) + 0.5f * Mathf.Sin(TwoPi * 3f * v));
            return Mathf.Clamp01(0.5f + 0.3f * a * b + 0.2f * c);
        }
    }
}
