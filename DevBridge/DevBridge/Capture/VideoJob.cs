using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Capture
{
    /// <summary>A burst's numbered frames made into an MP4 or GIF by ffmpeg on a background thread, so the game never waits.</summary>
    internal sealed class VideoJob
    {
        internal readonly string Command;
        private readonly string ffmpeg, arguments, temporary;
        private volatile bool done;

        internal bool Done => done;
        internal string Error { get; private set; }

        private VideoJob(string ffmpeg, string arguments, string temporary)
        {
            this.ffmpeg = ffmpeg;
            this.arguments = arguments;
            this.temporary = temporary;
            Command = $"\"{ffmpeg}\" {arguments}";
        }

        /// <summary>Starts ffmpeg on folder's frame-NN.png; a folder that is not kept is deleted when it ends.</summary>
        internal static VideoJob Start(string ffmpeg, string folder, float fps, string output, bool keepFrames)
        {
            var job = new VideoJob(ffmpeg, Arguments(folder, fps, output), keepFrames ? null : folder);
            new Thread(job.Run) { IsBackground = true, Name = "DevBridge ffmpeg" }.Start();
            return job;
        }

        // A GIF gets a palette made from its own frames; H.264 needs even sizes and the common 4:2:0 pixel format.
        private static string Arguments(string folder, float fps, string output)
        {
            string filter = Path.GetExtension(output).ToLowerInvariant() == ".gif"
                ? "split[a][b];[a]palettegen[p];[b][p]paletteuse"
                : "scale=trunc(iw/2)*2:trunc(ih/2)*2,format=yuv420p";
            string rate = fps.ToString("0.###", CultureInfo.InvariantCulture);
            string input = Path.Combine(folder, "frame-%02d.png");
            return $"-y -nostdin -hide_banner -loglevel error -framerate {rate} -start_number 0 -i \"{input}\" -vf \"{filter}\" \"{output}\"";
        }

        private void Run()
        {
            try
            {
                Error = Execute();
            }
            catch (Exception error)
            {
                Error = $"{error.GetType().Name}: {error.Message}";
            }
            finally
            {
                Tidy();
                done = true;
            }
        }

        private string Execute()
        {
            var info = new ProcessStartInfo(ffmpeg, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            using (Process process = Process.Start(info))
            {
                string errors = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0 ? null : $"ffmpeg exited with code {process.ExitCode}: {Fmt.Clip(errors.Trim(), 800)}";
            }
        }

        private void Tidy()
        {
            if (temporary == null) return;
            try
            {
                Directory.Delete(temporary, true);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                // Temporary frames left in %TEMP% are harmless.
            }
        }

        internal static bool Supported(string video)
        {
            string kind = video.ToLowerInvariant();
            string extension = kind == "mp4" || kind == "gif" ? "." + kind : Path.GetExtension(kind);
            return extension == ".mp4" || extension == ".gif";
        }

        /// <summary>video=mp4 or gif puts it beside the sheet under the sheet's name; otherwise the path given.</summary>
        internal static string PathFor(string video, string sheet)
        {
            string kind = video.ToLowerInvariant();
            if (kind == "mp4" || kind == "gif") return Path.ChangeExtension(sheet, "." + kind);
            string path = Path.GetFullPath(Fmt.WindowsPath(video));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }

        /// <summary>fps=, or the rate the frames were taken at so the video plays at real speed.</summary>
        internal static float Rate(float fps, List<Shot> shots)
        {
            if (fps > 0f) return Mathf.Clamp(fps, 0.5f, 120f);
            float span = shots[shots.Count - 1].Real;
            return span > 0f ? Mathf.Clamp((shots.Count - 1) / span, 0.5f, 120f) : 10f;
        }

        /// <summary>ffmpeg= if given, else ffmpeg.exe on the game's PATH or on the user's and machine's PATH as saved now.</summary>
        internal static string Find(string given)
        {
            if (given != null)
            {
                string path = Path.GetFullPath(Fmt.WindowsPath(given));
                return File.Exists(path) ? path : throw new BridgeException($"no ffmpeg at {path}");
            }
            foreach (string folder in SearchPath())
            {
                string candidate = Path.Combine(folder, "ffmpeg.exe");
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        // The game inherits PATH from whatever started Steam, often before ffmpeg was installed; the saved user and
        // machine values (read from the registry) are what a new shell would see.
        private static IEnumerable<string> SearchPath()
        {
            var targets = new[] { EnvironmentVariableTarget.Process, EnvironmentVariableTarget.User, EnvironmentVariableTarget.Machine };
            foreach (EnvironmentVariableTarget target in targets)
                foreach (string entry in PathOf(target).Split(Path.PathSeparator))
                {
                    string folder = Environment.ExpandEnvironmentVariables(entry.Trim().Trim('"'));
                    if (folder.Length > 0 && folder.IndexOfAny(Path.GetInvalidPathChars()) < 0) yield return folder;
                }
        }

        private static string PathOf(EnvironmentVariableTarget target)
        {
            try
            {
                return Environment.GetEnvironmentVariable("PATH", target) ?? "";
            }
            catch (Exception)
            {
                return ""; // a missing or unreadable registry key only means one place fewer to look
            }
        }
    }
}
