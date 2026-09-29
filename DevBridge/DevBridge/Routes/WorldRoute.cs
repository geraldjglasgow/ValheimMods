using System.Linq;
using DevBridge.Server;
using DevBridge.World;

namespace DevBridge.Routes
{
    /// <summary>/nearby and /zdo: the networked objects around the player and what their ZDOs hold.</summary>
    internal static class WorldRoute
    {
        internal static void Register(Router router)
        {
            router.Add("/nearby",
                "/nearby?radius=20&filter=<prefab text>&limit=30&at=x,y,z\n" +
                "                       loaded networked objects near the player (or a point), nearest first, with creature, item,\n" +
                "                       container and piece details and the ZDO id",
                Nearby);
            router.Add("/zdo",
                "/zdo?id=<user:id>|hover=1|nearest=<prefab text>&keys=name1,name2\n" +
                "                       every value a ZDO stores, keys named where known (add runtime-built key names with keys=)",
                Zdo);
        }

        private static void Nearby(BridgeRequest request)
        {
            var found = ZdoLookup.Near(ZdoLookup.Centre(request.Get("at")), request.Float("radius", 20f), request.Get("filter"));
            request.Json(found.Take(request.Int("limit", 30)).Select(pair => WorldDump.Summary(pair.Value, pair.Key)).ToList());
        }

        private static void Zdo(BridgeRequest request)
        {
            ZdoNames.Add(request.Get("keys"));
            request.Json(WorldDump.Zdo(Pick(request)));
        }

        private static ZDO Pick(BridgeRequest request)
        {
            if (request.Has("id")) return ZdoLookup.ById(request.Get("id"));
            if (request.Flag("hover")) return ZdoLookup.OfHover();
            string filter = request.Get("nearest") ?? throw new BridgeException("give id=, hover=1 or nearest=<prefab text>");
            var near = ZdoLookup.Near(ZdoLookup.Centre(request.Get("at")), request.Float("radius", 100f), filter);
            return near.Count > 0 ? near[0].Value.GetZDO() : throw new BridgeException($"no {filter} within {request.Float("radius", 100f)} m");
        }
    }
}
