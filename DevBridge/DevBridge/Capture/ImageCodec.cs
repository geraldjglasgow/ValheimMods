using System.Reflection;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace DevBridge.Capture
{
    /// <summary>
    /// ImageConversion calls this build cannot name directly: their overloads take netstandard 2.1 types (spans), so the
    /// compiler refuses every call by name. Found once by reflection, by their byte[] forms.
    /// </summary>
    internal static class ImageCodec
    {
        private static readonly MethodInfo LoadMethod = typeof(ImageConversion).GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
        private static readonly MethodInfo PngMethod = typeof(ImageConversion).GetMethod("EncodeArrayToPNG",
            new[] { typeof(byte[]), typeof(GraphicsFormat), typeof(uint), typeof(uint), typeof(uint) });

        /// <summary>A PNG or JPG file's bytes into the texture (resized to fit); false when they are not an image.</summary>
        internal static bool Load(Texture2D texture, byte[] bytes) => (bool)Need(LoadMethod, "LoadImage").Invoke(null, new object[] { texture, bytes });

        /// <summary>Raw pixels as a PNG; safe off the main thread.</summary>
        internal static byte[] Png(byte[] raw, GraphicsFormat format, uint width, uint height) =>
            (byte[])Need(PngMethod, "EncodeArrayToPNG").Invoke(null, new object[] { raw, format, width, height, 0u });

        private static MethodInfo Need(MethodInfo method, string name) =>
            method ?? throw new BridgeException($"this Unity has no ImageConversion.{name}(byte[] ...)");
    }
}
