using DevBridge.Server;
using DevBridge.Timing;

namespace DevBridge.Routes
{
    /// <summary>/time: slow motion, pause and frame stepping of this machine's game clock, and handing it back.</summary>
    internal static class TimeRoute
    {
        internal static void Register(Router router) => router.Add("/time",
            "/time?scale=0.25&pause=1|resume=1&step=N|step_seconds=0.1&reset=1&force=1&timeout=30\n" +
            "                       this machine's game clock: scale= slow motion or speed-up (0.01-4), pause=1 freezes it\n" +
            "                       (keeping the scale), resume=1 runs on, step=N runs N frames (step_seconds= that much game\n" +
            "                       time) at the scale and freezes again, reset=1 hands the clock back to the game; alone,\n" +
            "                       reports scale, frame, time and fixedDeltaTime; warns when others are connected, and on a\n" +
            "                       dedicated server needs force=1",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            if (request.Flag("reset"))
            {
                ClockHold.Release("reset");
                request.Json(ClockReport.Build());
                return;
            }
            Validate(request);
            if (request.Has("scale")) ClockHold.SetScale(request.Float("scale", 1f));
            if (Pausing(request)) ClockHold.Pause();
            if (Resuming(request)) ClockHold.Resume();
            if (ClockStep.Asked(request)) Async.Start(request, ClockStep.Run(request));
            else request.Json(ClockReport.Build());
        }

        /// <summary>Throws for any argument that cannot be done, so a request changes all it asks for or nothing.</summary>
        private static void Validate(BridgeRequest request)
        {
            bool step = ClockStep.Asked(request);
            if (!step && !request.Has("scale") && !Pausing(request) && !Resuming(request)) return;
            ClockNetwork.Check(request.Flag("force"));
            if (request.Has("scale") && !(request.Float("scale", 0f) > 0f))
                throw new BridgeException("scale= takes a number from 0.01 to 4; pause=1 stops the clock");
            if (Pausing(request) && Resuming(request)) throw new BridgeException("pause=1 and resume=1 together: choose one");
            if (step && Resuming(request)) throw new BridgeException("a step ends paused: leave out resume=");
            if (step) ClockStep.Validate(request, request.Has("scale") ? ClockHold.Clamp(request.Float("scale", 1f)) : ClockHold.RunScale);
        }

        private static bool Pausing(BridgeRequest request) => request.Flag("pause");

        /// <summary>resume=1, or pause=0.</summary>
        private static bool Resuming(BridgeRequest request) => request.Flag("resume") || request.Has("pause") && !request.Flag("pause");
    }
}
