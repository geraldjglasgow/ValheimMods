using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// The hard level key: with a level or raise entry selected, the next click becomes an instant, hard-edged level to
    /// the current target height, and one click is triggered through the game's own placement (so the entry's costs and
    /// the normal edit pipeline apply). The one-shot overrides (<see cref="BrushState.NextHeight"/> and friends) are
    /// used up by the edit factory; if the click does not happen soon (no stamina, too soon after the last one) they are
    /// dropped, so they never leak into a later ordinary click. Local player only.
    /// </summary>
    public static class HardLevel
    {
        private const float ArmedSeconds = 0.5f;
        private static float armedAt = -10f;

        public static bool Applies(ToolAction action)
        {
            return action != null && !EntryKinds.IsPath(action) && (action.Height == HeightOp.Level || action.Height == HeightOp.Raise);
        }

        public static void Update(Player player, ToolAction action)
        {
            if (!Keys.Pressed(ControlSettings.HardLevelKey))
                return;
            if (!Applies(action) || !LevelGate.Style(LevelStyle.Instant))
            {
                Messages.Center(Applies(action) ? BrushWords.LevelLocked : BrushWords.HardLevelNeeds);
                return;
            }
            BrushState.NextHeight = HeightOp.Level;
            BrushState.NextStyle = LevelStyle.Instant;
            BrushState.NextHardness = 1f;
            armedAt = Time.time;
            player.m_placePressedTime = Time.time;
        }

        /// <summary>Drops overrides nobody used within the armed time; called every frame, whatever is selected.</summary>
        public static void Expire()
        {
            if (BrushState.NextHeight.HasValue && Time.time - armedAt > ArmedSeconds)
                EditFactory.ClearOneShot();
        }
    }
}
