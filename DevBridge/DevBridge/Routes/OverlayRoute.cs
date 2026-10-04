using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Overlay;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.Rendering;

namespace DevBridge.Routes
{
    /// <summary>/overlay: persistent debug lines in the world for things that have no look of their own.</summary>
    internal static class OverlayRoute
    {
        internal static void Register(Router router)
        {
            router.Add("/overlay",
                "/overlay?show=colliders,ai,paths,wards,portals,terrain,spawners,zones|all&off=1|<names>&radius=30&every=0.5&max=400&layer=<names>|all\n" +
                "                       persistent lines round the player (or the free camera), redrawn every= seconds: collider shapes,\n" +
                "                       AI sight, hearing and targets, AI paths, ward radii, portal links and triggers, edited terrain,\n" +
                "                       spawner ranges, the zone grid; show= adds, off= removes; max= lines per category; layer= keeps\n" +
                "                       only those collider layers; alone, reports what is on and what each category drew",
                Overlay);
        }

        private static void Overlay(BridgeRequest request)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || (ZNet.instance && ZNet.instance.IsDedicated()))
                throw new BridgeException("no graphics here (dedicated server?): the overlay draws only on a game client");
            if (!ZNetScene.instance) throw new BridgeException("no world loaded");
            Settings(request);
            if (request.Has("off")) OverlayView.Hide(request.Flag("off") ? "all" : request.Get("off"));
            if (request.Has("show"))
            {
                OverlayView.Show(request.Require("show"));
                OverlayTicker.Ensure();
            }
            if (OverlayView.AnyOn) OverlayView.Redraw();
            OverlayTicker.Drawn();
            request.Json(OverlayView.Report());
        }

        private static void Settings(BridgeRequest request)
        {
            OverlayView.Radius = Mathf.Clamp(request.Float("radius", OverlayView.Radius), 1f, 300f);
            OverlayView.Every = Mathf.Clamp(request.Float("every", OverlayView.Every), 0.1f, 10f);
            OverlayView.Max = Mathf.Clamp(request.Int("max", OverlayView.Max), 10, 5000);
            if (request.Has("layer")) Layer(request.Get("layer", "all"));
        }

        private static void Layer(string names)
        {
            int mask = 0;
            foreach (string name in names.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0 && name != "all"))
            {
                int layer = LayerMask.NameToLayer(name);
                if (layer < 0) throw new BridgeException($"no layer {name}; the game has {string.Join(", ", LayerList())}");
                mask |= 1 << layer;
            }
            (OverlayView.Layers, OverlayView.LayerNames) = mask == 0 ? (~0, "all") : (mask, names);
        }

        private static IEnumerable<string> LayerList() => Enumerable.Range(0, 32).Select(LayerMask.LayerToName).Where(name => name.Length > 0);
    }
}
