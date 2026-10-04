using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Capture
{
    /// <summary>What one /burst call asks for, read and checked before the first frame is taken.</summary>
    internal sealed class BurstPlan
    {
        internal const int MaxFrames = 64;
        private const int MaxSide = 8192;
        private const float MaxPixels = 4096f * 4096f;

        internal int Count, Every, Cell, Columns, Quality;
        internal float Seconds, Delay, Fps;
        internal string Crop, Out, Video, Ffmpeg;
        internal bool Keep;

        /// <summary>Real seconds between frames spread over seconds= (the first at 0, the last at seconds).</summary>
        internal float Interval => Count > 1 ? Seconds / (Count - 1) : 0f;

        internal static BurstPlan From(BridgeRequest request)
        {
            if (request.Has("every") && request.Has("seconds")) throw new BridgeException("give seconds= or every=, not both");
            BurstPlan plan = Read(request);
            if (plan.Crop != null) Fmt.Numbers(plan.Crop, 4, "crop");
            plan.CheckSheet();
            plan.CheckTime((float)request.Patience.TotalSeconds);
            if (plan.Video != null) plan.CheckVideo(request.Get("ffmpeg"));
            return plan;
        }

        private static BurstPlan Read(BridgeRequest request) => new BurstPlan
        {
            Count = Mathf.Clamp(request.Int("frames", 12), 1, MaxFrames),
            Every = request.Has("every") ? Mathf.Clamp(request.Int("every", 1), 1, 600) : 0,
            Seconds = Mathf.Clamp(request.Float("seconds", 2f), 0f, 300f),
            Delay = Mathf.Clamp(request.Float("delay", 0f), 0f, 300f),
            Cell = Mathf.Clamp(request.Int("cell", 480), 128, 1920),
            Columns = request.Int("cols", 0),
            Crop = request.Get("crop"),
            Out = request.Get("out"),
            Quality = request.Int("quality", 85),
            Keep = request.Flag("keep"),
            Video = request.Get("video"),
            Fps = request.Float("fps", 0f),
        };

        /// <summary>The cell a frame of this screen size shrinks to after the crop: cell= wide at most, never enlarged.</summary>
        internal Vector2Int CellFor(int screenWidth, int screenHeight)
        {
            float w = screenWidth, h = screenHeight;
            if (Crop != null)
            {
                float[] n = Fmt.Numbers(Crop, 4, "crop");
                w = Mathf.Clamp(n[2], 1f, w);
                h = Mathf.Clamp(n[3], 1f, h);
            }
            int width = Mathf.Min(Cell, (int)w);
            return new Vector2Int(width, Mathf.Max(1, Mathf.RoundToInt(h * width / w)));
        }

        internal int ColumnsFor(Vector2Int cell) => Columns > 0 ? Mathf.Min(Columns, Count) : ContactSheet.Columns(Count, cell.x, cell.y);

        /// <summary>Refuses a sheet too big to build, judged from the window as it is now.</summary>
        private void CheckSheet()
        {
            if (Screen.width <= 0 || Screen.height <= 0) throw new BridgeException("the game window has no size (minimised?)");
            Vector2Int cell = CellFor(Screen.width, Screen.height);
            int columns = ColumnsFor(cell);
            Vector2Int size = ContactSheet.Size(columns, (Count + columns - 1) / columns, cell);
            if (size.x > MaxSide || size.y > MaxSide || (float)size.x * size.y > MaxPixels)
                throw new BridgeException($"the sheet would be {size.x}x{size.y} pixels: lower cell=, frames= or cols=");
        }

        /// <summary>The HTTP call waits timeout= (default 30) or seconds=, plus 5; a longer burst would answer too late.</summary>
        private void CheckTime(float patience)
        {
            float frame = Mathf.Max(Time.unscaledDeltaTime, 1f / 240f);
            float needed = Delay + (Every > 0 ? Count * Every * frame : Seconds) + Count * 0.05f + 3f;
            if (needed > patience)
                throw new BridgeException($"this burst takes about {Mathf.CeilToInt(needed)} s, longer than the call waits: add timeout={Mathf.CeilToInt(needed) + 10}");
        }

        private void CheckVideo(string ffmpeg)
        {
            if (!VideoJob.Supported(Video)) throw new BridgeException("video= takes a .mp4 or .gif path, or just mp4 or gif to put it beside the sheet");
            Ffmpeg = VideoJob.Find(ffmpeg) ?? throw new BridgeException("video= needs ffmpeg.exe on the PATH, or ffmpeg=<path to ffmpeg.exe>");
        }
    }
}
