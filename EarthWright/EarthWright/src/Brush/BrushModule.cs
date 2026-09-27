using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Brush
{
    /// <summary>
    /// Entry point of the Brush module: brush size, shape, edge, target height and the keys, wheel and gamepad that
    /// change them; the ghost placement ("no silent blocks"), the camera zoom block, hold-to-repeat and the hard level
    /// key. Binds sections "1. Brush", "2. Target Height" and the brush keys of "10. Controls", registers
    /// EarthWright.Brushes.yml, the "tool level" sender guard and the per-frame tick. The Harmony patches (ghost, scroll
    /// wheel, gamepad zoom, remove guard, the game-hotkey guards for F9 and debug mode) are attributed classes the
    /// plugin applies.
    /// </summary>
    public static class BrushModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            BrushSettings.Bind(synced);
            TargetSettings.Bind(synced);
            ControlSettings.Bind(synced);
            BrushRules.Register(synced);
            BrushWords.Register();
            TargetState.Reset();
            ForgetOnChange();
            Ticker.OnUpdate("EarthWright brush", BrushTick.Update);
        }

        /// <summary>Settings that shape an entry's starting values: a change starts every entry from its new defaults.</summary>
        private static void ForgetOnChange()
        {
            BrushSettings.SizeMultiplier.SettingChanged += (_, _) => BrushMemory.Forget();
            BrushSettings.MultiplyLevel.SettingChanged += (_, _) => BrushMemory.Forget();
            BrushSettings.MultiplyRaise.SettingChanged += (_, _) => BrushMemory.Forget();
            BrushSettings.MultiplySmooth.SettingChanged += (_, _) => BrushMemory.Forget();
            BrushSettings.MultiplyPaint.SettingChanged += (_, _) => BrushMemory.Forget();
            BrushSettings.ResizeModded.SettingChanged += (_, _) => BrushMemory.Forget();
            BrushSettings.DefaultStyle.SettingChanged += (_, _) => BrushMemory.Forget();
        }
    }
}
