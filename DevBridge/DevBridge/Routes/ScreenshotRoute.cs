using System.Collections;
using System.Collections.Generic;
using DevBridge.Capture;
using DevBridge.Server;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Routes
{
    /// <summary>/screenshot: the finished frame, UI included, written to a PNG or JPG file.</summary>
    internal static class ScreenshotRoute
    {
        internal static void Register(Router router) => router.Add("/screenshot",
            "/screenshot?out=<file.png|.jpg>&maxWidth=1600&crop=x,y,w,h&quality=85\n" +
            "                       capture the game window with its UI; crop is in screen pixels from the top-left and is taken\n" +
            "                       before shrinking; replies with the file path, the screen size and the image size",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            FrameGrab.RequireGraphics();
            Async.Start(request, Capture(request));
        }

        private static IEnumerator Capture(BridgeRequest request)
        {
            yield return new WaitForEndOfFrame();
            Texture2D shot = FrameGrab.Take();
            int[] screen = { shot.width, shot.height };
            Texture2D image = FrameGrab.Shrink(FrameGrab.Crop(shot, request.Get("crop")), request.Int("maxWidth", 1600));
            try
            {
                string path = ImageFiles.Save(image, request.Get("out"), request.Int("quality", 85), "shot");
                request.Json(new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["screen"] = screen,
                    ["image"] = new[] { image.width, image.height },
                });
            }
            finally
            {
                Object.Destroy(image);
            }
        }
    }
}
