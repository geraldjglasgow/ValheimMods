using EarthWright.Actions;
using EarthWright.Core;

namespace EarthWright.Brush
{
    /// <summary>
    /// The brush's per-frame work (registered with <see cref="Ticker"/>): works out whether the brush is active, loads
    /// the selected entry's remembered values when the entry changes, reads the keys, publishes the values into
    /// <see cref="BrushState"/>, refreshes the target height, drives hold-to-repeat and hands the HUD its lines.
    /// When the terrain tool is put away everything the brush changed in the game (place delay, HUD lines) is undone.
    /// Runs on every client for its own local player; a dedicated server has no local player and does nothing here.
    /// </summary>
    public static class BrushTick
    {
        private static ToolAction loadedAction;
        private static BrushValues values;
        private static Player lastPlayer;

        /// <summary>The selected entry's values are loaded again next frame (the YAML rules changed).</summary>
        public static void ForceReload() => loadedAction = null;

        public static void Update()
        {
            Player player = Player.m_localPlayer;
            TrackPlayer(player);
            HardLevel.Expire();
            ToolAction action = GeneralSettings.Active && player != null ? ActionCatalog.Current : null;
            if (action == null)
            {
                Deactivate();
                return;
            }
            if (action != loadedAction || values == null)
                Load(action);
            else
                SyncExternal();
            BrushState.Active = true;
            BrushState.Action = action;
            if (InputGate.Open)
                BrushInput.Update(player, action, values);
            BrushPublisher.Publish(action, values);
            TargetHeight.Refresh(player);
            PlaceRepeat.Update(player, action);
            BrushHud.Show(action, values);
        }

        /// <summary>
        /// Loads an entry's remembered values. Values written into the brush state since the last publish (a custom
        /// entry's start values, written when it was selected in the build menu) belong to the entry now selected.
        /// </summary>
        private static void Load(ToolAction action)
        {
            loadedAction = action;
            values = BrushMemory.For(action);
            ExternalEdits.Adopt(values);
            BrushPublisher.Publish(action, values);
            ValueSelector.EnsureValid(action, values);
        }

        /// <summary>
        /// Adopts values another module wrote into the brush state (the Preview panel, Menu's custom start values) into
        /// the selected entry and publishes them clamped. Also called by the ghost patch, so values written after this
        /// frame's tick are in place before the next click.
        /// </summary>
        public static void SyncExternal()
        {
            if (loadedAction == null || values == null || loadedAction != ActionCatalog.Current)
                return;
            if (ExternalEdits.Adopt(values))
                BrushPublisher.Publish(loadedAction, values);
        }

        private static void Deactivate()
        {
            if (!BrushState.Active)
                return;
            BrushState.Active = false;
            BrushState.Action = null;
            BrushState.HasAim = false;
            PlaceRepeat.Stop();
            BrushHud.Hide();
        }

        /// <summary>A new local player (login, another character, after logout): locks and one-shots do not carry over.</summary>
        private static void TrackPlayer(Player player)
        {
            if (ReferenceEquals(player, lastPlayer))
                return;
            lastPlayer = player;
            TargetState.Reset();
            PlaceRepeat.Stop();
            EditFactory.ClearOneShot();
        }
    }
}
