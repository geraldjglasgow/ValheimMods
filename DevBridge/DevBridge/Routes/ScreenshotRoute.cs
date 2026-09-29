using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DevBridge.Routes
{
    /// <summary>/screenshot: the finished frame, UI included, written to a PNG or JPG file.</summary>
    internal static class ScreenshotRoute
    {
        internal static void Register(Router router) => router.Add("/screenshot",
            "/screenshot?out=<file.png|.jpg>&maxWidth=1600&crop=x,y,w,h&quality=85\n" +
            "                       capture the game window with its UI; crop is in screen pixels from the top-left and is taken\n" +
            "                       before shrinking; replies with the file path, the screen size and the image size",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new BridgeException("no graphics device here (dedicated server?)");
            Async.Start(request, Capture(request));
        }

        private static IEnumerator Capture(BridgeRequest request)
        {
            yield return new WaitForEndOfFrame();
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            int[] screen = { shot.width, shot.height };
            Texture2D image = Shrink(Crop(shot, request.Get("crop")), request.Int("maxWidth", 1600));
            string path = Save(image, request.Get("out"), request.Int("quality", 85));
            request.Json(new Dictionary<string, object>
            {
                ["path"] = path,
                ["screen"] = screen,
                ["image"] = new[] { image.width, image.height },
            });
            Object.Destroy(image);
        }

        private static Texture2D Crop(Texture2D source, string spec)
        {
            if (spec == null) return source;
            float[] n = Fmt.Numbers(spec, 4, "crop");
            int x = Mathf.Clamp((int)n[0], 0, source.width - 1), y = Mathf.Clamp((int)n[1], 0, source.height - 1);
            int w = Mathf.Clamp((int)n[2], 1, source.width - x), h = Mathf.Clamp((int)n[3], 1, source.height - y);
            var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
            result.SetPixels(source.GetPixels(x, source.height - y - h, w, h));
            result.Apply();
            Object.Destroy(source);
            return result;
        }

        private static Texture2D Shrink(Texture2D source, int maxWidth)
        {
            if (maxWidth <= 0 || source.width <= maxWidth) return source;
            int width = maxWidth, height = Mathf.Max(1, Mathf.RoundToInt(source.height * (float)maxWidth / source.width));
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, target);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(source);
            return result;
        }

        private static string Save(Texture2D image, string requested, int quality)
        {
            string path = requested != null
                ? Fmt.WindowsPath(requested)
                : Path.Combine(Path.GetTempPath(), "DevBridge", $"shot-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png");
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string extension = Path.GetExtension(path).ToLowerInvariant();
            bool jpg = extension == ".jpg" || extension == ".jpeg";
            File.WriteAllBytes(path, jpg ? image.EncodeToJPG(Mathf.Clamp(quality, 1, 100)) : image.EncodeToPNG());
            return path;
        }
    }
}
