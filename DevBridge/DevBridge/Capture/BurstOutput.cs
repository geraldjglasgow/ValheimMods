using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DevBridge.Capture
{
    /// <summary>A captured burst on disk: the contact sheet, the numbered frames (keep=1 or video=), and the video.</summary>
    internal sealed class BurstOutput
    {
        internal readonly BurstPlan Plan;
        internal readonly List<Shot> Shots;
        internal string Sheet, Folder, VideoPath;
        internal int Width, Height, Columns;
        internal bool GameTime;
        internal float Fps;
        internal VideoJob Video;

        internal BurstOutput(BurstPlan plan, List<Shot> shots)
        {
            Plan = plan;
            Shots = shots;
        }

        /// <summary>Labels give game time when it ran apart from real time: slow motion, a pause, a long hitch.</summary>
        internal void WriteSheet()
        {
            Shot first = Shots[0];
            GameTime = Shots.Exists(s => Mathf.Abs(s.Game - s.Real) > 0.05f);
            Columns = Plan.ColumnsFor(new Vector2Int(first.Image.Width, first.Image.Height));
            Canvas sheet = ContactSheet.Compose(Shots, Columns, GameTime);
            Width = sheet.Width;
            Height = sheet.Height;
            Sheet = ImageFiles.PathFor(Plan.Out, "burst");
            ImageFiles.Write(sheet, Sheet, Plan.Quality);
        }

        /// <summary>One unlabelled PNG per frame, a game frame apart so a long run does not stall the game in one go.</summary>
        internal IEnumerator WriteFrames()
        {
            Folder = FramesFolder();
            foreach (Shot shot in Shots)
            {
                ImageFiles.Write(shot.Image, FramePath(shot.Index), 100);
                yield return null;
            }
        }

        internal string FramePath(int index) => Path.Combine(Folder, $"frame-{index:00}.png");

        /// <summary>keep=1 keeps them beside the sheet in a folder named after it; for video= alone a temporary one.</summary>
        private string FramesFolder()
        {
            string folder = Plan.Keep
                ? Path.Combine(Path.GetDirectoryName(Sheet), Path.GetFileNameWithoutExtension(Sheet) + "-frames")
                : Path.Combine(Path.GetTempPath(), "DevBridge", $"video-{DateTime.Now:yyyyMMdd-HHmmss-fff}");
            Directory.CreateDirectory(folder);
            // An earlier, longer burst's frames would otherwise be read into the video.
            foreach (string old in Directory.GetFiles(folder, "frame-*.png")) File.Delete(old);
            return folder;
        }

        internal void StartVideo()
        {
            VideoPath = VideoJob.PathFor(Plan.Video, Sheet);
            Fps = VideoJob.Rate(Plan.Fps, Shots);
            Video = VideoJob.Start(Plan.Ffmpeg, Folder, Fps, VideoPath, Plan.Keep);
        }
    }
}
