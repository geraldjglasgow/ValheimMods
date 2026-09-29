using System;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>Optional local art-test file. Poll and decode only on Unity's main thread, while a Timber panel is visible.</summary>
    public static class BackgroundPreview
    {
        public static string FilePath => Path.Combine(Paths.ConfigPath, "PackPanel.ArtTest", "background.png");
        public static Texture2D Texture { get; private set; }
        public static int Version { get; private set; }

        private static float nextPoll;
        private static long observedTime = -1, observedLength = -1;
        private static long loadedTime = -1, loadedLength = -1;
        private static string lastError;
        private static readonly MethodInfo Decode = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
            ?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });

        public static void Poll()
        {
            float now = Time.realtimeSinceStartup;
            if (now < nextPoll)
                return;
            nextPoll = now + 0.5f;
            Texture2D candidate = null;
            try
            {
                FileInfo file = new FileInfo(FilePath);
                if (!file.Exists)
                {
                    observedTime = loadedTime = observedLength = loadedLength = -1;
                    lastError = null;
                    if (Texture != null)
                    {
                        Replace(null);
                        Plugin.Log.LogInfo("Background test PNG removed; using embedded wood.");
                    }
                    return;
                }
                long time = file.LastWriteTimeUtc.Ticks, length = file.Length;
                if (Texture != null && time == loadedTime && length == loadedLength)
                    return;
                // Wait for two matching observations so an editor can finish writing the file.
                if (time != observedTime || length != observedLength)
                {
                    observedTime = time;
                    observedLength = length;
                    return;
                }
                if (length < 24 || length > 64 * 1024 * 1024)
                    throw new InvalidDataException("Expected a complete PNG smaller than 64 MB.");
                byte[] bytes = File.ReadAllBytes(file.FullName);
                file.Refresh();
                if (!file.Exists || file.Length != length || file.LastWriteTimeUtc.Ticks != time)
                    return;
                PreviewPng.Validate(bytes);
                candidate = new Texture2D(2, 2, TextureFormat.RGBA32, true)
                {
                    name = "PackPanel_background_preview",
                    filterMode = FilterMode.Trilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                if (Decode == null || !(bool)Decode.Invoke(null, new object[] { candidate, bytes, true }))
                    throw new InvalidDataException("Unity could not decode this PNG.");
                Replace(candidate);
                candidate = null;
                loadedTime = time;
                loadedLength = length;
                lastError = null;
                Plugin.Log.LogInfo($"Reloaded background test PNG: {FilePath} ({Texture.width}x{Texture.height})");
            }
            catch (Exception error)
            {
                string message = error.GetBaseException().Message;
                if (lastError != message)
                {
                    lastError = message;
                    Plugin.Log.LogWarning($"Background test PNG not loaded; keeping the last working background: {message}");
                }
                nextPoll = now + 2f;
            }
            finally
            {
                if (candidate != null)
                    UnityEngine.Object.Destroy(candidate);
            }
        }

        private static void Replace(Texture2D replacement)
        {
            Texture2D previous = Texture;
            Texture = replacement;
            Version++;
            if (previous != null)
                UnityEngine.Object.Destroy(previous);
        }
    }
}
