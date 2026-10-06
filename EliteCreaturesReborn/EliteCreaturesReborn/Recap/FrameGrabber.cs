using System;
using System.Collections;
using PatchGuard;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesReborn.Recap
{
    /// <summary>
    /// Records the screen while the local player is alive (and a moment after a death): at the end of a frame, at most
    /// the chosen frames per second, the finished screen is shrunk on the GPU (<see cref="ScreenGrab"/>) and read back
    /// without stalling (<c>AsyncGPUReadback</c>) straight into a reused native buffer, which the encoder's thread copies
    /// out and encodes - nothing of the picture is copied on the game's own thread. A frame is skipped rather than queued
    /// when the GPU or the encoder falls behind. Nothing records while the recap window is open (it would record itself),
    /// while the game is paused, on a machine without graphics or with recording turned off.
    /// </summary>
    internal sealed class FrameGrabber : MonoBehaviour
    {
        private const int MaxInFlight = 3;
        private const int MaxPending = 6;

        private readonly WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
        private readonly ScreenGrab _grab = new ScreenGrab();
        private float _next;

        // A capture that failed once is reported once and not tried again this session.
        private static bool _broken;

        /// <summary>Pictures asked of the GPU and not yet read back; a recap waits for these too.</summary>
        public static int InFlight { get; private set; }

        private void OnEnable() => StartCoroutine(Loop());

        private void OnDisable() => _grab.Release();

        private IEnumerator Loop()
        {
            while (true)
            {
                yield return _endOfFrame;
                try
                {
                    Tick();
                }
                catch (Exception e)
                {
                    _broken = true;
                    Guard.Report(e, "death recap recording (stopped for this session; deaths are kept without video)");
                }
            }
        }

        private void Tick()
        {
            if (!ShouldRecord() || Time.unscaledTime < _next || InFlight >= MaxInFlight || FrameEncoder.Pending >= MaxPending)
            {
                return;
            }
            _next = Time.unscaledTime + 1f / Mathf.Clamp(RecapSettings.FramesPerSecond.Value, 5, 30);
            Request(_grab.Capture(Mathf.Clamp(RecapSettings.Height.Value, 180, 720)), Time.time);
        }

        // The picture is read straight into a buffer the encoder owns from then on; one the GPU never got is given back.
        private static void Request(RenderTexture video, float time)
        {
            int width = video.width;
            int height = video.height;
            NativeArray<byte> pixels = FrameEncoder.Rent(width * height * 4);
            InFlight++;
            try
            {
                AsyncGPUReadback.RequestIntoNativeArray(ref pixels, video, 0, TextureFormat.RGBA32,
                    request => Arrived(request, pixels, width, height, time));
            }
            catch
            {
                InFlight = Mathf.Max(0, InFlight - 1);
                FrameEncoder.Return(pixels);
                throw;
            }
        }

        private static void Arrived(AsyncGPUReadbackRequest request, NativeArray<byte> pixels, int width, int height, float time)
        {
            InFlight = Mathf.Max(0, InFlight - 1);
            if (request.hasError || !RecapSettings.Record.Value)
            {
                FrameEncoder.Return(pixels);
                return;
            }
            float keep = RecapSettings.Seconds.Value + RecapStore.Tail + 1f;
            FrameEncoder.Submit(pixels, width, height, time, keep);
        }

        private static bool ShouldRecord() =>
            RecapSettings.Record.Value
            && RecapStore.Recording
            && !Window.RecapWindow.IsOpen
            && !Game.IsPaused()
            && Supported;

        private static bool Supported =>
            !_broken
            && !FrameEncoder.Failed
            && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null
            && SystemInfo.supportsAsyncGPUReadback
            && JpegCodec.Available;
    }
}
