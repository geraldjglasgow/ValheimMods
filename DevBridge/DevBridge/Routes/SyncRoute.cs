using DevBridge.Server;
using DevBridge.Sync;

namespace DevBridge.Routes
{
    /// <summary>/sync: the same question put to every DevBridge on this machine (a dedicated server, a host, clients), answers compared.</summary>
    internal static class SyncRoute
    {
        internal static void Register(Router router) => router.Add("/sync",
            "/sync?peers=1 | id=<user:id>|nearest=<prefab text>|hover=1&keys= | expr=<expression> | config=<plugin>&section=\n" +
            "                       this instance against every other DevBridge on this machine (ports 7780-7789, or ports=):\n" +
            "                       peers=1 who runs what, with plugin version differences; a ZDO's values, owner, revisions and\n" +
            "                       position (the target found here); an /eval result (members=, filter= passed on; an assignment\n" +
            "                       runs everywhere); a plugin's config values; \"same\" true only when every peer agrees",
            Handle);

        /// <summary>The target is found and the arguments checked here on the main thread; the asking happens off it.</summary>
        private static void Handle(BridgeRequest request) => SyncJob.Start(request, SyncScope.From(request), Query(request));

        private static SyncQuery Query(BridgeRequest request)
        {
            if (request.Get("expr") is string expr) return SyncQuery.Eval(expr, request);
            if (request.Get("config") is string mod) return SyncQuery.Config(mod, request.Get("section"));
            if (request.Has("id") || request.Flag("hover") || request.Has("nearest"))
                return SyncQuery.Zdo(WorldRoute.Pick(request), request.Get("keys"));
            return null;
        }
    }
}
