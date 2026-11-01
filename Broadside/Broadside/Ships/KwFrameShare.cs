using System.Diagnostics;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A frame's share of time for a ship's work that comes in bursts: a ship loaded with holes and rig damage (every peer
    /// that loads it cuts them all at once, as when many players arrive and a server that runs their areas loads their
    /// ships), a volley's hits. The work goes on a piece at a time while this frame's share lasts and the rest waits for the
    /// next frame; the first piece of a frame always goes on, so nothing is ever stuck. A piece is one hull mesh's step
    /// (<see cref="KwHolePart"/>: its long cut runs off the main thread), one rig look or one sail
    /// (<see cref="KwRigDamageLooks"/>), or one hole's boxes (<see cref="KwHoleBoxes"/>). Two shares, so neither waits on
    /// the other: <see cref="Looks"/> (<see cref="LooksMs"/> ms a frame) for what is drawn (holes cut through the hull's
    /// look, the rig's damage drawn), <see cref="Boxes"/> (<see cref="BoxesMs"/> ms) for the colliders opened through the
    /// holes, which a dedicated server has too. Main thread only.
    /// </summary>
    public sealed class KwFrameShare
    {
        public const double LooksMs = 3.0, BoxesMs = 2.0;
        public static readonly KwFrameShare Looks = new KwFrameShare(LooksMs), Boxes = new KwFrameShare(BoxesMs);

        private readonly long budget;
        private int frame = -1;
        private long spent;

        private KwFrameShare(double ms) => budget = (long)(ms * Stopwatch.Frequency / 1000.0);

        /// <summary>Whether another piece of work fits in this frame: always the first of a frame, then while its share lasts.</summary>
        public bool Open()
        {
            if (frame != Time.frameCount)
                (frame, spent) = (Time.frameCount, 0L);
            return spent < budget;
        }

        /// <summary>A piece of work's time (stopwatch ticks, from <see cref="Stopwatch.GetTimestamp"/>) counted against this frame's share.</summary>
        public void Spend(long ticks) => spent += ticks;
    }
}
