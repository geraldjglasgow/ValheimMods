using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevBridge.Capture;
using DevBridge.Server;
using DevBridge.Studio;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace DevBridge.Routes
{
    /// <summary>
    /// The studio's Screenshot button and Screenshots tab (also usable from curl): take one, list them, show one, rename
    /// and delete them (<see cref="StudioShots"/>).
    /// </summary>
    internal static class StudioShotsRoute
    {
        private const string Pad = "\n                       ";

        internal static void Register(Router router)
        {
            router.Add("/studio/shot", "/studio/shot           a screenshot of the game window, UI included, saved as <time>.png in the Screenshots folder", Take);
            router.Add("/studio/shots", "/studio/shots          the Screenshots folder's PNG and JPG files, newest first", request => request.Json(StudioShots.List()));
            router.Add("/studio/shots/thumb", "/studio/shots/thumb?name=   a screenshot's small copy as a data URL (made when missing)",
                request => request.Json(new Dictionary<string, object> { ["image"] = StudioShots.Thumb(request.Require("name")) }));
            router.Add("/studio/shots/image", "/studio/shots/image?name=   a screenshot as a data URL",
                request => request.Json(new Dictionary<string, object> { ["image"] = StudioShots.Image(request.Require("name")) }));
            router.Add("/studio/shots/rename", "/studio/shots/rename?name=&to=   rename one (its extension kept when to= leaves it off)",
                request => request.Json(StudioShots.Rename(request.Require("name"), request.Require("to"))));
            router.Add("/studio/shots/delete", "/studio/shots/delete?names=a.png,b.png (or POST them, one a line)" + Pad +
                "move screenshots to the Recycle Bin", Delete);
            router.Add("/studio/shots/open", "/studio/shots/open?name=   Explorer at the folder, or with that screenshot picked",
                request => request.Json(StudioShots.Open(request.Get("name"))));
        }

        private static void Take(BridgeRequest request)
        {
            FrameGrab.RequireGraphics();
            Async.Start(request, Capture(request));
        }

        // The frame is read on the main thread; the PNG is made on a worker so the game does not hitch.
        private static IEnumerator Capture(BridgeRequest request)
        {
            yield return new WaitForEndOfFrame();
            Texture2D shot = FrameGrab.Take();
            byte[] raw = shot.GetRawTextureData();
            (GraphicsFormat format, uint width, uint height) = (shot.graphicsFormat, (uint)shot.width, (uint)shot.height);
            string path = StudioShots.NewPath();
            StudioShots.WriteThumb(shot, path);
            Task write = Task.Run(() => File.WriteAllBytes(path, ImageCodec.Png(raw, format, width, height)));
            while (!write.IsCompleted) yield return null;
            StudioShots.Done(path);
            if (write.IsFaulted) throw new BridgeException("the screenshot could not be written: " + write.Exception?.InnerException?.Message);
            request.Json(StudioShots.Describe(new FileInfo(path)));
        }

        // A POST is one name a line, so a name may hold a comma; names= is comma separated.
        private static void Delete(BridgeRequest request)
        {
            string[] names = request.Has("names") ? request.Get("names", "").Split(',') : (request.Get("body") ?? "").Split('\n', '\r');
            request.Json(StudioShots.Delete(names.Select(name => name.Trim()).Where(name => name.Length > 0)));
        }    }
}
