using System;
using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Capture
{
    /// <summary>The /burst reply: where the sheet is, its sizes, each frame's number and times, the frame files and video.</summary>
    internal static class BurstReply
    {
        internal static Dictionary<string, object> Build(BurstOutput output)
        {
            List<Shot> shots = output.Shots;
            Shot last = shots[shots.Count - 1];
            var reply = new Dictionary<string, object>
            {
                ["sheet"] = output.Sheet,
                ["image"] = new[] { output.Width, output.Height },
                ["screen"] = shots[0].Screen,
                ["cell"] = new[] { shots[0].Image.Width, shots[0].Image.Height },
                ["columns"] = output.Columns,
                ["labels"] = output.GameTime ? "game time" : "real time",
                ["real_seconds"] = Seconds(last.Real),
                ["game_seconds"] = Seconds(last.Game),
                ["time_scale"] = shots.Select(s => Fmt.R(s.TimeScale)).Distinct().ToList(),
                ["frames"] = shots.Select(s => Frame(output, s)).ToList(),
            };
            if (output.Plan.Keep) reply["folder"] = output.Folder;
            if (output.Video != null) AddVideo(reply, output);
            return reply;
        }

        private static Dictionary<string, object> Frame(BurstOutput output, Shot shot)
        {
            var frame = new Dictionary<string, object>
            {
                ["index"] = shot.Index,
                ["frame"] = shot.Frame,
                ["real"] = Seconds(shot.Real),
                ["game"] = Seconds(shot.Game),
            };
            if (output.Plan.Keep) frame["file"] = output.FramePath(shot.Index);
            return frame;
        }

        // Frames can be milliseconds apart, finer than Fmt.R's hundredths.
        private static double Seconds(float value) => Math.Round(value, 3);

        private static void AddVideo(Dictionary<string, object> reply, BurstOutput output)
        {
            VideoJob job = output.Video;
            reply["fps"] = Fmt.R(output.Fps);
            if (!job.Done) reply["video_error"] = $"ffmpeg is still running; the video appears at {output.VideoPath} when it ends (a larger timeout= waits for it)";
            else if (job.Error != null) reply["video_error"] = job.Error;
            else reply["video"] = output.VideoPath;
            if (!job.Done || job.Error != null) reply["video_command"] = job.Command;
        }
    }
}
