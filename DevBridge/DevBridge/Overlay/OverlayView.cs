using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// What /overlay has on and how it draws: the categories, the radius, the time between redraws, the cap on lines per
    /// category and the collider layer filter. Everything drawn lives under one root in the world scene, so logging out
    /// removes it, and the ticker turns the overlay off when the world unloads. Local to this machine.
    /// </summary>
    internal static class OverlayView
    {
        internal static readonly Category[] All =
        {
            new Category("colliders", ColliderDrawer.Draw),
            new Category("ai", AiDrawer.Draw),
            new Category("paths", PathDrawer.Draw),
            new Category("wards", WardDrawer.Draw),
            new Category("portals", PortalDrawer.Draw),
            new Category("terrain", TerrainDrawer.Draw),
            new Category("spawners", SpawnerDrawer.Draw),
            new Category("zones", ZoneDrawer.Draw),
        };

        internal static float Radius = 30f;
        internal static float Every = 0.5f;
        internal static int Max = 400;
        internal static int Layers = ~0;
        internal static string LayerNames = "all";

        private static GameObject root;
        private static OverlayArea last;

        internal static bool AnyOn => All.Any(category => category.On);

        internal static void Show(string names)
        {
            foreach (Category category in Pick(names)) category.On = true;
        }

        internal static void Hide(string names)
        {
            foreach (Category category in Pick(names)) category.Off();
            if (!AnyOn) Stop();
        }

        /// <summary>Everything off and the root removed: when asked, or when the world unloads.</summary>
        internal static void Stop()
        {
            foreach (Category category in All) category.Off();
            if (root) UnityEngine.Object.Destroy(root);
            (root, last) = (null, null);
        }

        internal static void Redraw()
        {
            OverlayArea area = OverlayArea.Around(Radius, Layers);
            if (area == null) return;
            last = area;
            foreach (Category category in All.Where(category => category.On)) category.Redraw(area, Root(), Max);
        }

        internal static Dictionary<string, object> Report() => new Dictionary<string, object>
        {
            ["on"] = All.Where(category => category.On).Select(category => category.Name).ToList(),
            ["radius"] = Fmt.R(Radius),
            ["every"] = Fmt.R(Every),
            ["max"] = Max,
            ["layer"] = LayerNames,
            ["around"] = last == null ? null : last.From,
            ["at"] = last == null ? null : Fmt.V3(last.Centre),
            ["drawn"] = All.Where(category => category.On).ToDictionary(category => category.Name, category => (object)category.Report()),
            ["categories"] = string.Join(",", All.Select(category => category.Name)),
        };

        private static Transform Root()
        {
            if (!root) root = new GameObject("DevBridge_Overlay");
            return root.transform;
        }

        private static IEnumerable<Category> Pick(string names)
        {
            string[] wanted = names.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0).ToArray();
            if (wanted.Any(name => name.Equals("all", StringComparison.OrdinalIgnoreCase))) return All;
            return wanted.Select(name => All.FirstOrDefault(category => category.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? throw new BridgeException($"no overlay {name}; there are {string.Join(", ", All.Select(category => category.Name))} and all")).ToList();
        }
    }
}
