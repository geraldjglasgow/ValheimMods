using DevBridge.Server;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DevBridge.Capture
{
    /// <summary>The finished frame with its UI as a texture, cropped and shrunk: what /screenshot and /burst take.</summary>
    internal static class FrameGrab
    {
        /// <summary>Refuses where nothing is drawn: a dedicated server has no graphics device.</summary>
        internal static void RequireGraphics()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new BridgeException("no graphics device here (dedicated server?)");
        }

        /// <summary>The whole game window, UI included; only complete after WaitForEndOfFrame.</summary>
        internal static Texture2D Take() => ScreenCapture.CaptureScreenshotAsTexture();

        /// <summary>crop=x,y,w,h in screen pixels from the top-left, clamped to the image; destroys the source.</summary>
        internal static Texture2D Crop(Texture2D source, string spec)
        {
            if (spec == null) return source;
            RectInt r = Bounds(source, spec);
            var result = new Texture2D(r.width, r.height, TextureFormat.RGBA32, false);
            result.SetPixels(source.GetPixels(r.x, source.height - r.y - r.height, r.width, r.height));
            result.Apply();
            Object.Destroy(source);
            return result;
        }

        /// <summary>Crop then shrink on the GPU, for /burst: no per-pixel copy on the CPU for every frame. Destroys the source.</summary>
        internal static Texture2D CropShrink(Texture2D source, string spec, int maxWidth)
        {
            if (spec == null) return Shrink(source, maxWidth);
            RectInt r = Bounds(source, spec);
            int width = maxWidth > 0 ? Mathf.Min(maxWidth, r.width) : r.width;
            int height = Mathf.Max(1, Mathf.RoundToInt(r.height * (float)width / r.width));
            RenderTexture cropped = Temp(r.width, r.height);
            Graphics.Blit(source, cropped, new Vector2(r.width / (float)source.width, r.height / (float)source.height),
                new Vector2(r.x / (float)source.width, (source.height - r.y - r.height) / (float)source.height));
            RenderTexture target = Downsample(cropped, width, height);
            RenderTexture.ReleaseTemporary(cropped);
            Texture2D result = Read(target);
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(source);
            return result;
        }

        private static RectInt Bounds(Texture2D source, string spec)
        {
            float[] n = Fmt.Numbers(spec, 4, "crop");
            int x = Mathf.Clamp((int)n[0], 0, source.width - 1), y = Mathf.Clamp((int)n[1], 0, source.height - 1);
            return new RectInt(x, y, Mathf.Clamp((int)n[2], 1, source.width - x), Mathf.Clamp((int)n[3], 1, source.height - y));
        }

        /// <summary>At most maxWidth wide with the same aspect (never enlarged); destroys the source when it shrinks.</summary>
        internal static Texture2D Shrink(Texture2D source, int maxWidth)
        {
            if (maxWidth <= 0 || source.width <= maxWidth) return source;
            int width = maxWidth, height = Mathf.Max(1, Mathf.RoundToInt(source.height * (float)maxWidth / source.width));
            source.filterMode = FilterMode.Bilinear;
            RenderTexture target = Downsample(source, width, height);
            Texture2D result = Read(target);
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(source);
            return result;
        }

        // Halving while more than twice too wide makes each step an even 2x2 average, so a large shrink (a 4K frame to a
        // 480 pixel cell) keeps thin lines and sparks that one bilinear step, reading 4 pixels of every 64, would drop.
        private static RenderTexture Downsample(Texture source, int width, int height)
        {
            RenderTexture held = null;
            Texture current = source;
            while (current.width > width * 2)
            {
                RenderTexture half = Temp(current.width / 2, Mathf.Max(1, current.height / 2));
                Graphics.Blit(current, half);
                if (held) RenderTexture.ReleaseTemporary(held);
                current = held = half;
            }
            RenderTexture target = Temp(width, height);
            Graphics.Blit(current, target);
            if (held) RenderTexture.ReleaseTemporary(held);
            return target;
        }

        private static RenderTexture Temp(int width, int height)
        {
            RenderTexture texture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static Texture2D Read(RenderTexture source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            return result;
        }
    }
}
