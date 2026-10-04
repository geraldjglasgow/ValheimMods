using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Capture
{
    /// <summary>Captured images written as PNG or JPG files, chosen by the file's extension.</summary>
    internal static class ImageFiles
    {
        /// <summary>The requested path (Git Bash paths accepted) or a new %TEMP%\DevBridge\prefix-time.png; its folder is made.</summary>
        internal static string PathFor(string requested, string prefix)
        {
            string path = requested != null
                ? Fmt.WindowsPath(requested)
                : Path.Combine(Path.GetTempPath(), "DevBridge", $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png");
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }

        internal static string Save(Texture2D image, string requested, int quality, string prefix)
        {
            string path = PathFor(requested, prefix);
            Write(image, path, quality);
            return path;
        }

        /// <summary>JPG for a .jpg or .jpeg path, PNG for anything else.</summary>
        internal static void Write(Texture2D image, string path, int quality)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            bool jpg = extension == ".jpg" || extension == ".jpeg";
            File.WriteAllBytes(path, jpg ? image.EncodeToJPG(Mathf.Clamp(quality, 1, 100)) : image.EncodeToPNG());
        }

        /// <summary>Writes a canvas through a texture made for it and destroyed straight after.</summary>
        internal static void Write(Canvas canvas, string path, int quality)
        {
            var image = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false);
            try
            {
                image.SetPixels32(canvas.Pixels);
                image.Apply(false);
                Write(image, path, quality);
            }
            finally
            {
                Object.Destroy(image);
            }
        }
    }
}
