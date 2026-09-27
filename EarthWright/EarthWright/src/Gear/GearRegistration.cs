using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// When the shovel and the tool levels are put into the game: whenever an object database is ready (main menu and
    /// world, on every machine including a dedicated server), whenever the net scene wakes (its order against the object
    /// database is not guaranteed, and dropped shovels need it), and whenever a Tools setting changes (hot reload or the
    /// server's values arriving).
    /// </summary>
    public static class GearRegistration
    {
        public static void OnObjectDb(ObjectDB db)
        {
            List<GameObject> scene = ZNetScene.instance != null ? ZNetScene.instance.m_prefabs : null;
            if (!ShovelPrefab.EnsureBuilt(db.m_items, scene))
                return;
            ShovelPrefab.RegisterIn(db);
            if (ZNetScene.instance != null)
                ShovelPrefab.RegisterIn(ZNetScene.instance);
            ShovelTable.Refresh();
            ShovelRecipe.RegisterIn(db);
        }

        public static void OnScene(ZNetScene scene)
        {
            List<GameObject> items = ObjectDB.instance != null ? ObjectDB.instance.m_items : null;
            if (ShovelPrefab.EnsureBuilt(scene.m_prefabs, items))
                ShovelPrefab.RegisterIn(scene);
        }

        /// <summary>Re-applies the levels, recipes and the shovel's menu when their settings change.</summary>
        public static void WatchSettings()
        {
            Watch(GeneralSettings.Enabled, ToolLevels.ApplyAll);
            Watch(GearSettings.ShovelEnabled, ToolLevels.ApplyAll);
            Watch(GearSettings.ShovelRecipe, ToolLevels.ApplyAll);
            Watch(GearSettings.ShovelStation, ToolLevels.ApplyAll);
            Watch(GearSettings.ShovelMaxDurability, ToolLevels.ApplyAll);
            Watch(GearSettings.ShovelWearPerUse, ToolLevels.ApplyAll);
            Watch(GearSettings.ShovelEntries, ShovelTable.Refresh);
            foreach (ToolLevelSettings tool in ToolLevels.Tools)
            {
                Watch(tool.MaxLevel, ToolLevels.ApplyAll);
                Watch(tool.UpgradeCost, ToolLevels.ApplyAll);
                Watch(tool.DurabilityPerLevel, ToolLevels.ApplyAll);
            }
        }

        private static void Watch<T>(ConfigEntry<T> entry, Action apply)
        {
            string name = "EarthWright tools: " + entry.Definition.Key;
            entry.SettingChanged += (_, __) => Safe.Run(name, apply);
        }
    }
}
