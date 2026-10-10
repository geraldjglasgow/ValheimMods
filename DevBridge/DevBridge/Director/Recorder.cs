using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DevBridge.Capture;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.Rendering;

namespace DevBridge.Director
{
    /// <summary>
    /// Records the game window to a video file offline: while it runs every rendered frame is one frame of the film
    /// (Time.captureDeltaTime), read back from the GPU and encoded by ffmpeg with NVENC, the sound beside it as a WAV;
    /// when it stops the two are muxed. The game runs as slowly as it must, and the film is smooth regardless.
    /// </summary>
    internal static class Recorder
    {
        private static Take take;
        private static Dictionary<string, object> last;

        internal static bool Active => take != null;

        internal static Dictionary<string, object> Start(RecordPlan plan)
        {
            FrameGrab.RequireGraphics();
            if (take != null) throw new BridgeException($"already recording {take.Plan.Base}; stop it first");
            take = new Take(plan);
            DevBridgePlugin.Instance.StartCoroutine(Loop(take));
            return take.Describe();
        }

        /// <summary>Ends the recording; the mux runs in the background and its result shows in Status.</summary>
        internal static Dictionary<string, object> Stop()
        {
            if (take == null) return last ?? new Dictionary<string, object> { ["recording"] = false };
            Take ending = take;
            take = null;
            last = ending.Finish();
            return last;
        }

        internal static Dictionary<string, object> Status()
        {
            if (take != null) return take.Describe();
            if (last == null) return new Dictionary<string, object> { ["recording"] = false };
            last["mux"] = Take.MuxState(last);
            return last;
        }

        private static IEnumerator Loop(Take recording)
        {
            var end = new WaitForEndOfFrame();
            while (take == recording)
            {
                yield return end;
                if (take == recording) recording.CaptureFrame();
            }
        }
    }

    /// <summary>Where and how to record: base path (without extension), frame rate, quality, sound, output size.</summary>
    internal sealed class RecordPlan
    {
        internal string Base;
        internal int Fps = 60;
        internal int Quality = 16;
        internal bool Audio = true;
        internal int Width, Height;
        internal string Ffmpeg;

        internal static RecordPlan From(BridgeRequest request)
        {
            var plan = new RecordPlan
            {
                Base = Fmt.WindowsPath(request.Require("out")), Fps = Mathf.Clamp(request.Int("fps", 60), 10, 120),
                Quality = Mathf.Clamp(request.Int("quality", 16), 0, 51), Audio = !request.Has("audio") || request.Flag("audio"),
            };
            plan.Size(request.Get("size"));
            plan.Ffmpeg = VideoJob.Find(request.Get("ffmpeg")) ?? throw new BridgeException("recording needs ffmpeg.exe on the PATH, or ffmpeg=<path>");
            return plan;
        }

        /// <summary>size=WxH scales the video in ffmpeg; by default it is the window's own size.</summary>
        internal void Size(string spec)
        {
            if (string.IsNullOrEmpty(spec)) return;
            string[] parts = spec.ToLowerInvariant().Split('x');
            if (parts.Length != 2 || !int.TryParse(parts[0], out Width) || !int.TryParse(parts[1], out Height))
                throw new BridgeException("size= takes WIDTHxHEIGHT, such as 1920x1080");
            Width &= ~1;
            Height &= ~1;
        }

        /// <summary>The output size: size= if given, else the window scaled to at most 2560 wide; always even.</summary>
        internal void Fit(int screenWidth, int screenHeight)
        {
            if (Width > 0 && Height > 0) return;
            float scale = Mathf.Min(1f, 2560f / screenWidth);
            Width = Mathf.RoundToInt(screenWidth * scale) & ~1;
            Height = Mathf.RoundToInt(screenHeight * scale) & ~1;
        }

        internal string Rate => Fps.ToString(CultureInfo.InvariantCulture);
        internal string Video => Base + ".video.mp4";
        internal string Wav => Base + ".wav";
        internal string Final => Base + ".mp4";

        internal void PrepareFolder() => Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Final)));
    }
}
