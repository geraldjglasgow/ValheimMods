using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// Keeps the menus in step with the settings. Any setting change (the cfg edited, a value synced from the server,
    /// another module's key rebound), a new custom entry file, or a change of this player's admin rights or of the
    /// clearing switch marks the menus dirty; once per frame they are brought up to date. Descriptions, piece flags and
    /// table layouts are cheap and always redone (another module may have rebuilt a table meanwhile); the player's
    /// menu is only rebuilt when the visible entries or a table actually changed, since that also rebuilds the
    /// player's placement ghost.
    /// </summary>
    public static class MenuRefresh
    {
        private const float PollSeconds = 1f;

        private static bool dirty;
        private static bool customsDirty;
        private static string lastState;
        private static bool lastAdmin;
        private static bool lastClearing;
        private static float nextPoll;

        public static void Initialize(SyncedConfiguration synced)
        {
            synced.Config.SettingChanged += (sender, args) => dirty = true;
            synced.Config.ConfigReloaded += (sender, args) => dirty = true;
            Ticker.OnUpdate("EarthWright menu refresh", Tick);
        }

        public static void MarkDirty() => dirty = true;

        /// <summary>The custom entry list changed: its prefabs are remade on the next refresh.</summary>
        public static void CustomsChanged()
        {
            customsDirty = true;
            dirty = true;
        }

        /// <summary>Brings everything up to date now. <paramref name="databaseReady"/>: a new object database, always relaid.</summary>
        public static void RefreshNow(bool databaseReady)
        {
            dirty = false;
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
                return;
            string selected = PlayerRefresh.SelectedName();
            bool rebuilt = databaseReady || customsDirty;
            if (rebuilt)
                CustomPrefabs.Rebuild();
            customsDirty = false;
            GameEntryBehaviour.Apply();
            EntryDescriptions.ApplyAll();
            BuildMenuMode.Apply();
            EntryVisibility.ApplyFlags();
            bool relaid = ToolTables.ApplyAll();
            string state = EntryVisibility.State();
            if (!rebuilt && !relaid && state == lastState)
                return;
            lastState = state;
            PlayerRefresh.Refresh(selected);
        }

        private static void Tick()
        {
            Poll();
            if (dirty)
                RefreshNow(false);
        }

        /// <summary>Admin rights and the clearing switch are not settings of this module; they are checked once a second.</summary>
        private static void Poll()
        {
            if (Time.unscaledTime < nextPoll)
                return;
            nextPoll = Time.unscaledTime + PollSeconds;
            bool admin = Side.LocalIsAdmin;
            bool clearing = Safe.Call("EarthWright clearing switch", () => MenuHooks.ClearingEnabled(), false);
            if (admin == lastAdmin && clearing == lastClearing)
                return;
            lastAdmin = admin;
            lastClearing = clearing;
            dirty = true;
        }
    }
}
