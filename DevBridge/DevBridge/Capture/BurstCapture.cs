using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Capture
{
    /// <summary>Takes a burst's frames at the end of the frames its plan picks, each shrunk to its cell at once.</summary>
    internal static class BurstCapture
    {
        internal static IEnumerator Run(BurstPlan plan, List<Shot> shots)
        {
            if (plan.Delay > 0f) yield return new WaitForSecondsRealtime(plan.Delay);
            for (int index = 0; index < plan.Count; index++)
            {
                for (int waited = 0; index > 0 && !Due(plan, shots[0], index, waited); waited++) yield return null;
                yield return new WaitForEndOfFrame();
                shots.Add(Take(plan, index));
            }
            Rebase(shots);
        }

        /// <summary>
        /// Checked once per new frame before its end: every= counts rendered frames, otherwise the frame is due once its
        /// start time reaches the next step of seconds=. At least one new frame always passes, so no frame is taken twice.
        /// </summary>
        private static bool Due(BurstPlan plan, Shot first, int index, int waited)
        {
            if (plan.Every > 0) return waited >= plan.Every;
            return waited >= 1 && Time.unscaledTime - first.Real >= index * plan.Interval;
        }

        // A 4K frame is 33 MB: it is cropped and shrunk to its cell straight away and only the cell's pixels are kept.
        private static Shot Take(BurstPlan plan, int index)
        {
            Texture2D frame = FrameGrab.Take();
            int[] screen = { frame.width, frame.height };
            Texture2D cell = FrameGrab.CropShrink(frame, plan.Crop, plan.Cell);
            // The frame's own scale: by end of frame Time.timeScale already holds the next frame's (the game and /time set it in Update).
            float scale = Time.unscaledDeltaTime > 0f ? Time.deltaTime / Time.unscaledDeltaTime : Time.timeScale;
            var shot = new Shot
            {
                Index = index, Frame = Time.frameCount, Real = Time.unscaledTime, Game = Time.time, TimeScale = scale,
                Screen = screen, Image = new Canvas(cell.GetPixels32(), cell.width, cell.height).Opaque(),
            };
            Object.Destroy(cell);
            return shot;
        }

        /// <summary>Counts frames and seconds from the first shot.</summary>
        private static void Rebase(List<Shot> shots)
        {
            int frame = shots[0].Frame;
            float real = shots[0].Real, game = shots[0].Game;
            foreach (Shot shot in shots)
            {
                shot.Frame -= frame;
                shot.Real -= real;
                shot.Game -= game;
            }
        }
    }
}
