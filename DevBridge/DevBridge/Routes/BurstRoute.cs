using System.Collections;
using System.Collections.Generic;
using DevBridge.Capture;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/burst: a run of frames laid out on one contact sheet, so a single image shows motion.</summary>
    internal static class BurstRoute
    {
        internal static void Register(Router router) => router.Add("/burst",
            "/burst?seconds=2&frames=12|every=N&cell=480&cols=&crop=x,y,w,h&delay=0&out=<sheet.png|.jpg>&keep=1&video=<.mp4|.gif>\n" +
            "                       frames (at most 64) spread over seconds= of real time, or every= N rendered frames, on one contact\n" +
            "                       sheet, each cell= pixels wide (crop in screen pixels, taken first), labelled with its number and\n" +
            "                       seconds since the first (game time, marked GAME, when slow motion or a pause made them differ);\n" +
            "                       keep=1 also writes each frame beside the sheet; video= (or video=mp4|gif beside the sheet) runs\n" +
            "                       ffmpeg from the PATH or ffmpeg=<exe> at fps= (default real speed); replies with the sheet path,\n" +
            "                       its sizes and each frame's real and game seconds",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            FrameGrab.RequireGraphics();
            BurstPlan plan = BurstPlan.From(request);
            float deadline = Time.realtimeSinceStartup + (float)request.Patience.TotalSeconds - 3f;
            Async.Start(request, Run(request, plan, deadline));
        }

        private static IEnumerator Run(BridgeRequest request, BurstPlan plan, float deadline)
        {
            var shots = new List<Shot>();
            for (IEnumerator capture = BurstCapture.Run(plan, shots); capture.MoveNext();) yield return capture.Current;
            var output = new BurstOutput(plan, shots);
            output.WriteSheet();
            if (plan.Keep || plan.Video != null)
                for (IEnumerator frames = output.WriteFrames(); frames.MoveNext();) yield return frames.Current;
            if (plan.Video != null)
            {
                output.StartVideo();
                while (!output.Video.Done && Time.realtimeSinceStartup < deadline) yield return null;
            }
            request.Json(BurstReply.Build(output));
        }
    }
}
