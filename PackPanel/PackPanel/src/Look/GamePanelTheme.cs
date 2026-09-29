using System.Collections.Generic;
using System;
using HarmonyLib;
using PackPanel.Core;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>Replaces native wood panel art with PackPanel's frame and wallpaper, retaining layout and controls.</summary>
    public static class GamePanelTheme
    {
        private static bool pending = true;
        private static readonly HashSet<Image> newlyEnabled = new HashSet<Image>();

        public static void Initialize()
        {
            SceneManager.sceneLoaded += (scene, mode) => RequestRefresh();
            InventorySettings.Enabled.SettingChanged += (sender, args) => RequestRefresh();
            InventorySettings.BrownStyle.SettingChanged += (sender, args) => RequestRefresh();
            InventorySettings.PanelTheme.SettingChanged += (sender, args) => RequestRefresh();
        }

        public static void RequestRefresh() => pending = true;

        // Run after prefab setup, coalescing all panels enabled by one menu into one scan.
        // Inactive panels are included so switching back to Brown/vanilla restores them too.
        public static void Update()
        {
            if (!pending && newlyEnabled.Count == 0)
                return;
            bool on = SkinArt.Timber && SkinArt.Panel != null;
            if (pending)
            {
                pending = false;
                foreach (Image image in Resources.FindObjectsOfTypeAll<Image>())
                    if (image.gameObject.scene.IsValid() && image.canvas != null && IsPanel(image))
                        TimberBackground.Apply(image, on);
            }
            else
                foreach (Image image in newlyEnabled)
                    if (image != null && image.gameObject.scene.IsValid() && image.canvas != null && IsPanel(image))
                        TimberBackground.Apply(image, on);
            newlyEnabled.Clear();
        }

        private static bool IsPanel(Image image)
        {
            // The independently drawn rim is decorative, not another panel to style recursively.
            if (image.name == "PackPanel_frame")
                return false;
            if (image.transform.Find("PackPanel_timberwood") != null)
                return true;
            Sprite sprite = image.sprite;
            if (sprite == null || (image.type != Image.Type.Sliced && image.type != Image.Type.Tiled))
                return false;
            string name = sprite.name;
            // Selection masks, little projecting tabs and repair buttons are not panel backgrounds.
            return name.StartsWith("woodpanel_", StringComparison.Ordinal)
                && !name.EndsWith("_mask", StringComparison.Ordinal)
                && !name.StartsWith("woodpanel_flik", StringComparison.Ordinal)
                && (image.fillCenter || image.transform.Find("PackPanel_timberwood") != null);
        }

        [HarmonyPatch(typeof(Image), "OnEnable")]
        private static class PanelEnabled
        {
            private static void Postfix(Image __instance)
            {
                // Existing panels retain their theme and geometry while hidden. Opening one must
                // not rescan every Image in the game or dirty unrelated menus.
                if (__instance.transform.Find("PackPanel_timberwood") == null && IsPanel(__instance))
                    newlyEnabled.Add(__instance);
            }
        }
    }
}
