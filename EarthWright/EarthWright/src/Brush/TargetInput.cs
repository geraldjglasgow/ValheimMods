using System.Globalization;
using EarthWright.Actions;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// The target height keys, with any terrain entry selected (a ramp or road takes a locked, exact or copied floor
    /// height for its points): lock/release, height up/down (hold to repeat, Shift for bigger steps), back to feet, the
    /// mode cycle Feet → Aimed → Continued, and copying the aimed floor piece's height. Local player state only.
    /// </summary>
    public static class TargetInput
    {
        public static void Update()
        {
            if (Keys.Pressed(TargetKeys.Lock))
                ToggleLock();
            if (Keys.Pressed(TargetKeys.Feet))
                SetLive(TargetSource.Feet);
            if (Keys.Pressed(TargetKeys.Cycle))
                SetLive(NextLive(BrushState.TargetMode));
            if (Keys.Pressed(TargetKeys.Floor))
                CopyFloor();
            float shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? BrushSettings.FastMultiplier.Value : 1f;
            float step = TargetSettings.HeightStep.Value * shift;
            if (RepeatKey.Fire(TargetKeys.Up))
                Nudge(step);
            if (RepeatKey.Fire(TargetKeys.Down))
                Nudge(-step);
        }

        private static void ToggleLock()
        {
            if (TargetState.IsFixed)
            {
                TargetState.Release();
                Note(BrushWords.Released);
                return;
            }
            TargetState.SetFixed(TargetSource.Locked, BrushState.TargetHeight);
            Note(BrushWords.Locked + " " + Metres(BrushState.TargetHeight));
        }

        private static void SetLive(TargetSource mode)
        {
            TargetState.SetLive(mode);
            Note(BrushWords.TargetMode + " " + BrushWords.SourceName(mode));
        }

        private static TargetSource NextLive(TargetSource mode)
        {
            switch (mode)
            {
                case TargetSource.Feet: return TargetSource.Aimed;
                case TargetSource.Aimed: return TargetSource.Continued;
                default: return TargetSource.Feet;
            }
        }

        private static void CopyFloor()
        {
            if (!FloorPick.TryHeight(out float height))
            {
                Note(BrushWords.NoFloor);
                return;
            }
            TargetState.SetFixed(TargetSource.Floor, height);
            Note(BrushWords.FloorCopied + " " + Metres(height));
        }

        private static void Nudge(float delta)
        {
            TargetState.AdjustLocked(delta);
            BrushAnnounce.Value(BrushWords.Target + " " + Metres(TargetState.Fixed));
        }

        private static void Note(string text)
        {
            if (TargetSettings.LockMessage.Value)
                Messages.Center(text);
        }

        private static string Metres(float height) => height.ToString("0.00", CultureInfo.InvariantCulture) + " m";
    }
}
