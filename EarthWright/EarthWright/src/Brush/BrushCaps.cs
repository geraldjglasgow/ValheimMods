using System;

namespace EarthWright.Brush
{
    /// <summary>
    /// Limits other modules put on the brush. The Gear module sets <see cref="LevelMaxRadius"/> from the held tool's
    /// upgrade level; the Brush module clamps the radius to the smallest of its own maximum, the skill cap and this.
    /// </summary>
    public static class BrushCaps
    {
        /// <summary>The largest radius the held tool's level allows; float.MaxValue when levels do not limit it.</summary>
        public static Func<float> LevelMaxRadius = () => float.MaxValue;

        /// <summary>Whether the held tool's level unlocks a feature (see the Gear module's level settings); true when not gated.</summary>
        public static Func<string, bool> LevelUnlocks = feature => true;

        /// <summary>Null when the held tool's level allows this menu entry (by id), else the refusal (set by the Gear module).</summary>
        public static Func<string, string> EntryRefusal = entryId => null;
    }
}
