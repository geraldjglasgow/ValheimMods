using System.Collections.Generic;
using System;
using HarmonyLib;
using PackPanel.Core;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>
    /// Replaces native wood panel art with PackPanel's frame and wallpaper, retaining layout and controls. A scene load or
    /// a theme change looks at every image again, <see cref="PerFrame"/> a frame rather than all in one; the sprites
    /// already judged are forgotten on a scene load, so the list never keeps a scene's runtime sprites alive.
    /// </summary>
    public static class GamePanelTheme
    {
        private const int PerFrame = 500;
        private static bool pending = true;
        private static Image[] scan;
        private static int scanned;
        private static readonly HashSet<Image> newlyEnabled = new HashSet<Image>();
        private static readonly Dictionary<Sprite, bool> woodSprites = new Dictionary<Sprite, bool>();

        public static void Initialize()
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                woodSprites.Clear();
                RequestRefresh();
            };
            InventorySettings.Enabled.SettingChanged += (sender, args) => RequestRefresh();
            InventorySettings.BrownStyle.SettingChanged += (sender, args) => RequestRefresh();
            InventorySettings.PanelTheme.SettingChanged += (sender, args) => RequestRefresh();
        }

        public static void RequestRefresh() => pending = true;

        // Run after prefab setup, coalescing all panels enabled by one menu into one scan.
        // Inactive panels are included so switching back to Brown/vanilla restores them too.
        public static void Update()
        {
            if (!pending && scan == null && newlyEnabled.Count == 0)
                return;
            bool on = SkinArt.Timber && SkinArt.Panel != null;
            if (pending)
            {
                pending = false;
                scan = Resources.FindObjectsOfTypeAll<Image>();
                scanned = 0;
            }
            if (scan != null)
                ScanSome(on);
            foreach (Image image in newlyEnabled)
                if (image != null && Themed(image))
                    TimberBackground.Apply(image, on);
            newlyEnabled.Clear();
        }

        /// <summary>The next <see cref="PerFrame"/> images of the scan; the scan ends with the last.</summary>
        private static void ScanSome(bool on)
        {
            int end = Math.Min(scan.Length, scanned + PerFrame);
            for (; scanned < end; scanned++)
            {
                Image image = scan[scanned];
                if (image != null && Themed(image))
                    TimberBackground.Apply(image, on);
            }
            if (scanned >= scan.Length)
                scan = null;
        }

        private static bool Themed(Image image) => image.gameObject.scene.IsValid() && image.canvas != null && IsPanel(image);

        private static bool IsPanel(Image image)
        {
            bool panel = image.transform.Find("PackPanel_timberwood") != null
                || ((image.type == Image.Type.Sliced || image.type == Image.Type.Tiled) && image.fillCenter && WoodSprite(image.sprite));
            // The independently drawn rim is decorative, not another panel to style recursively (its name read last: a new string).
            return panel && image.name != "PackPanel_frame";
        }

        /// <summary>
        /// One of the game's wood panel sprites, decided once per sprite (reading a sprite's name makes a new string).
        /// Selection masks, little projecting tabs and repair buttons are not panel backgrounds.
        /// </summary>
        private static bool WoodSprite(Sprite sprite)
        {
            if (sprite == null)
                return false;
            if (!woodSprites.TryGetValue(sprite, out bool wood))
            {
                string name = sprite.name;
                wood = name.StartsWith("woodpanel_", StringComparison.Ordinal)
                    && !name.EndsWith("_mask", StringComparison.Ordinal)
                    && !name.StartsWith("woodpanel_flik", StringComparison.Ordinal);
                woodSprites[sprite] = wood;
            }
            return wood;
        }

        [HarmonyPatch(typeof(Image), "OnEnable")]
        private static class PanelEnabled
        {
            private static void Postfix(Image __instance)
            {
                // Runs for every image shown anywhere (other mods' UI, map pins): the cheap reads come first, so almost
                // every one stops before a name is read or a child looked up. With Timber off nothing new is themed; a
                // theme change rescans every image anyway. Existing panels retain their theme and geometry while
                // hidden: opening one must not rescan every Image in the game or dirty unrelated menus.
                if (!SkinArt.Timber || !__instance.fillCenter || !WoodSprite(__instance.sprite))
                    return;
                if (__instance.transform.Find("PackPanel_timberwood") == null && IsPanel(__instance))
                    newlyEnabled.Add(__instance);
            }
        }
    }
}
