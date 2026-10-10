using System;
using System.Collections.Generic;
using System.IO;
using DevBridge.Capture;
using DevBridge.Server;
using DevBridge.Stage;
using DevBridge.Studio;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>
    /// The studio's Items tab (also usable from curl): /studio/items finds the game's items in groups with their tier,
    /// biome and station; /studio/view draws one prefab by itself, turned and zoomed, as a JPG the page shows or a file.
    /// </summary>
    internal static class StudioItemsRoute
    {
        private const string Pad = "\n                       ";

        internal static void Register(Router router)
        {
            router.Add("/studio/items", "/studio/items?filter=iron shield&most=400" + Pad +
                "the game's items matching every word, in groups (Shields, Swords...), each with its tier, biome and station", Items);
            router.Add("/studio/view", "/studio/view?prefab=ShieldBanded&bundle=<workshop bundle>&look=creature|piece|workshop&yaw=30&pitch=20" + Pad +
                "&zoom=1&size=512&file=<.jpg>   one prefab drawn by itself, turned right yaw degrees and tilted top-towards-you" + Pad +
                "pitch: a data URL, or a file; with bundle= a model of a workshop bundle (/studio/workshop/open), dressed in" + Pad +
                "the game's creature or building material as a mod would (by its kind unless look= says), or as built", View);
            StudioWorkshopRoute.Register(router);
        }

        private static void Items(BridgeRequest request) =>
            request.Json(StudioItems.Search(request.Get("filter"), Mathf.Clamp(request.Int("most", 400), 1, 3000)));

        private static void View(BridgeRequest request)
        {
            bool workshop = request.Has("bundle");
            GameObject prefab = workshop ? StudioWorkshop.Prefab(request.Require("bundle"), request.Require("prefab"))
                : GamePrefabs.Require(request.Require("prefab"));
            var view = new BoothView
            {
                Yaw = request.Float("yaw", 30f), Pitch = request.Float("pitch", 20f), Zoom = request.Float("zoom", 1f), Size = request.Int("size", 512),
                Look = workshop ? BoothLook.Check(request.Get("look") ?? StudioWorkshop.LookFor(request.Get("bundle"), prefab)) : null,
            };
            byte[] jpg = StudioBooth.Render(prefab, view);
            var reply = new Dictionary<string, object> { ["prefab"] = prefab.name, ["size"] = Mathf.Clamp(view.Size, StudioBooth.Smallest, StudioBooth.Largest) };
            if (request.Has("file")) reply["file"] = Save(jpg, request.Get("file"));
            else reply["image"] = "data:image/jpeg;base64," + Convert.ToBase64String(jpg);
            request.Json(reply);
        }

        private static string Save(byte[] jpg, string requested)
        {
            string path = Path.ChangeExtension(ImageFiles.PathFor(requested, "studio"), ".jpg");
            File.WriteAllBytes(path, jpg);
            return path;
        }
    }
}
