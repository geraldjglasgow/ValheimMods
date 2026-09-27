namespace EarthWright.Brush
{
    /// <summary>
    /// The target mode the player chose and, for the fixed modes (Locked, Exact, Floor), the height they fixed.
    /// Local to the player's session: it is reset when the local player changes (logout, another character), so a
    /// lock never carries over.
    /// </summary>
    public static class TargetState
    {
        /// <summary>The fixed height of the Locked, Exact and Floor modes (absolute world height).</summary>
        public static float Fixed;

        /// <summary>The live mode to return to when a lock is released.</summary>
        private static TargetSource lastLive = TargetSource.Feet;

        public static TargetSource Mode
        {
            get => BrushState.TargetMode;
            private set => BrushState.TargetMode = value;
        }

        public static bool IsFixed => IsFixedMode(Mode);

        public static bool IsFixedMode(TargetSource mode) => mode == TargetSource.Locked || mode == TargetSource.Exact || mode == TargetSource.Floor;

        public static void Reset()
        {
            Mode = StartMode();
            lastLive = Mode;
            Fixed = 0f;
        }

        public static TargetSource StartMode()
        {
            return TargetSettings.StartMode != null ? (TargetSource)(int)TargetSettings.StartMode.Value : TargetSource.Feet;
        }

        /// <summary>Switches to a live mode (Feet, Aimed, Continued).</summary>
        public static void SetLive(TargetSource mode)
        {
            Mode = mode;
            lastLive = mode;
        }

        /// <summary>Fixes a height in one of the fixed modes.</summary>
        public static void SetFixed(TargetSource mode, float height)
        {
            Mode = mode;
            Fixed = height;
            TargetHeight.Publish(height, mode);
        }

        /// <summary>Releases a fixed height: back to the live mode used before it.</summary>
        public static void Release() => SetLive(lastLive);

        /// <summary>Moves the height by metres; a live mode first fixes the current height (Locked), a floor becomes Locked.</summary>
        public static void AdjustLocked(float delta)
        {
            float start = IsFixed ? Fixed : BrushState.TargetHeight;
            SetFixed(Mode == TargetSource.Exact ? TargetSource.Exact : TargetSource.Locked, start + delta);
        }

        /// <summary>The selector's target value: switches to Exact, starting from the current height.</summary>
        public static void AdjustExact(float delta)
        {
            float start = IsFixed ? Fixed : BrushState.TargetHeight;
            SetFixed(TargetSource.Exact, start + delta);
        }
    }
}
