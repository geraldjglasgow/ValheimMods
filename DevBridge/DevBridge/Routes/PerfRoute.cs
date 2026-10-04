using DevBridge.Perf;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/perf: what each mod costs per frame, measured in the running game.</summary>
    internal static class PerfRoute
    {
        internal static void Register(Router router) => router.Add("/perf",
            "/perf?seconds=5&mod=Name&top=10&baseline=1\n" +
            "                       sample seconds= (max 60) of frames: fps, frame ms avg/p50/p95/p99/max, collections, managed heap;\n" +
            "                       per mod the main-thread ms and calls per frame of its Harmony prefixes, postfixes and finalizers\n" +
            "                       and its MonoBehaviours' Update, LateUpdate, FixedUpdate and OnGUI, with its top= methods;\n" +
            "                       transpilers listed, not timed; mod= times one mod only (less overhead), baseline=1 times\n" +
            "                       nothing; probing first takes a few seconds with many mods: raise timeout= if a reply is late",
            Handle);

        private static void Handle(BridgeRequest request) => PerfSession.Start(request, new PerfOptions
        {
            Seconds = Mathf.Clamp(request.Float("seconds", 5f), 0.5f, 60f),
            Mod = request.Get("mod"),
            Top = Mathf.Clamp(request.Int("top", 10), 1, 200),
            Baseline = request.Flag("baseline"),
        });
    }
}
