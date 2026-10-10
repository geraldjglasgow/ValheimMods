using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DevBridge.Director
{
    /// <summary>
    /// One recording. Each frame: the finished screen (UI included) scaled on the GPU into the output size, read back
    /// asynchronously in order and piped to ffmpeg (NVENC). The game is held to the film's rate (vsync off, frame limit
    /// = fps) and every frame advances the game by exactly one film frame (captureDeltaTime), so the picture is smooth
    /// through hitches; the listener's sound is laid on the frames when the take ends.
    /// </summary>
    internal sealed class Take
    {
        private static volatile string muxState;

        internal readonly RecordPlan Plan;
        private readonly FfmpegPipe pipe;
        private readonly AudioTap audio;
        private readonly RenderTexture screen, output;
        private readonly Queue<AsyncGPUReadbackRequest> inFlight = new Queue<AsyncGPUReadbackRequest>();
        private readonly Stopwatch watch = Stopwatch.StartNew();
        private readonly float previousCapture;
        private readonly int previousVSync, previousLimit;
        private int frames, written, dropped;
        private string wav;

        internal Take(RecordPlan plan)
        {
            Plan = plan;
            plan.PrepareFolder();
            plan.Fit(Screen.width, Screen.height);
            screen = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            output = new RenderTexture(plan.Width, plan.Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { filterMode = FilterMode.Bilinear };
            pipe = new FfmpegPipe(plan.Ffmpeg, Arguments(plan), plan.Width * plan.Height * 4);
            (previousCapture, previousVSync, previousLimit) = (Time.captureDeltaTime, QualitySettings.vSyncCount, Application.targetFrameRate);
            Time.captureDeltaTime = 1f / plan.Fps;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = plan.Fps;
            audio = plan.Audio ? AudioTap.Start(plan.Fps) : null;
        }

        // Raw RGBA in; BT.709 4:2:0 out, NVENC at constant quality.
        private static string Arguments(RecordPlan plan) =>
            $"-y -hide_banner -loglevel error -f rawvideo -pix_fmt rgba -s {plan.Width}x{plan.Height} -framerate {plan.Rate} -i pipe:0 " +
            "-vf \"scale=out_color_matrix=bt709:out_range=tv,format=yuv420p\" -c:v h264_nvenc -preset p6 -tune hq -rc vbr " +
            $"-cq {plan.Quality} -b:v 0 -maxrate 300M -bufsize 300M -colorspace bt709 -color_primaries bt709 -color_trc bt709 " +
            $"-movflags +faststart \"{plan.Video}\"";

        /// <summary>At the end of a frame: the image copied and scaled on the GPU, its readback queued, finished ones sent on.</summary>
        internal void CaptureFrame()
        {
            // Another end-of-frame grab (Elite Creatures Reborn's death recap) can leave a small target and viewport set,
            // which would shrink this copy into a corner: start from the whole back buffer.
            RenderTexture.active = null;
            GL.Viewport(new Rect(0f, 0f, Screen.width, Screen.height));
            ScreenCapture.CaptureScreenshotIntoRenderTexture(screen);
            bool flip = !SystemInfo.graphicsUVStartsAtTop;
            RenderTexture active = RenderTexture.active;
            Graphics.Blit(screen, output, new Vector2(1f, flip ? -1f : 1f), new Vector2(0f, flip ? 1f : 0f));
            RenderTexture.active = active;
            inFlight.Enqueue(AsyncGPUReadback.Request(output, 0, TextureFormat.RGBA32));
            audio?.Mark();
            frames++;
            Drain(3);
        }

        /// <summary>Sends finished readbacks in order; waits for the oldest while more than `keep` are in flight.</summary>
        private void Drain(int keep)
        {
            while (inFlight.Count > 0)
            {
                AsyncGPUReadbackRequest request = inFlight.Peek();
                if (!request.done && inFlight.Count <= keep) return;
                if (!request.done) request.WaitForCompletion();
                inFlight.Dequeue();
                if (request.hasError) { dropped++; continue; }
                byte[] frame = pipe.Rent();
                request.GetData<byte>().CopyTo(frame);
                pipe.Push(frame);
                written++;
            }
        }

        /// <summary>Gives the clock and frame rate back, closes the encoder, writes the sound, starts the mux.</summary>
        internal Dictionary<string, object> Finish()
        {
            Time.captureDeltaTime = previousCapture;
            (QualitySettings.vSyncCount, Application.targetFrameRate) = (previousVSync, previousLimit);
            Drain(0);
            string error = pipe.Finish(60000);
            try { wav = audio?.Finish(Plan.Wav); }
            catch (Exception failure) { (wav, error) = (null, error ?? "sound: " + failure.Message); }
            Object.Destroy(screen);
            Object.Destroy(output);
            Dictionary<string, object> info = Describe();
            (info["recording"], info["error"]) = (false, error);
            muxState = error != null ? "skipped: the video failed" : "running";
            if (error == null) new Thread(Mux) { IsBackground = true, Name = "DevBridge mux" }.Start();
            info["mux"] = muxState;
            return info;
        }

        private void Mux()
        {
            try
            {
                if (wav == null) File.Copy(Plan.Video, Plan.Final, true);
                else Run($"-y -hide_banner -loglevel error -i \"{Plan.Video}\" -i \"{wav}\" -map 0:v -map 1:a -c:v copy -c:a aac -b:a 320k -ac 2 -movflags +faststart \"{Plan.Final}\"");
                muxState = "done";
            }
            catch (Exception error)
            {
                muxState = "failed: " + error.Message;
            }
        }

        private void Run(string arguments)
        {
            var info = new ProcessStartInfo(Plan.Ffmpeg, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            using (Process process = Process.Start(info))
            {
                string errors = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new IOException($"ffmpeg exited with code {process.ExitCode}: {Fmt.Clip(errors.Trim(), 600)}");
            }
        }

        internal static string MuxState(Dictionary<string, object> info) => muxState ?? (string)info["mux"];

        internal Dictionary<string, object> Describe()
        {
            float real = (float)watch.Elapsed.TotalSeconds, film = frames / (float)Plan.Fps;
            return new Dictionary<string, object>
            {
                ["recording"] = true, ["file"] = Plan.Final, ["size"] = $"{Plan.Width}x{Plan.Height}", ["fps"] = Plan.Fps,
                ["frames"] = frames, ["written"] = written, ["dropped"] = dropped, ["seconds"] = Fmt.R(film),
                ["realSeconds"] = Fmt.R(real), ["realtime"] = Fmt.R(real > 0f ? film / real : 0f),
                ["audioSeconds"] = audio != null ? Fmt.R(audio.Seconds) : (object)null, ["audioJumps"] = audio?.Jumps,
                ["encoder"] = pipe.Failure,
            };
        }
    }
}
