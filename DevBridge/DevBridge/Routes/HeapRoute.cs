using DevBridge.Heap;
using DevBridge.Server;

namespace DevBridge.Routes
{
    /// <summary>/heap: memory now, hitches beside garbage collections, what Unity objects and mods' statics hold.</summary>
    internal static class HeapRoute
    {
        internal static void Register(Router router) => router.Add("/heap",
            "/heap                  memory now: managed heap used/size/free, collections, GC mode and slice, Unity native\n" +
            "/heap?seconds=30&hitch=40&max=50\n" +
            "                       watch seconds= (max 120) of frames, nothing patched: each garbage collection (frame time,\n" +
            "                       heap before/after), each frame over hitch= ms and whether a collection came within a frame,\n" +
            "                       with the log lines and the networked objects spawned or gone around it,\n" +
            "                       the heap's growth between collections (a lower bound on allocation) and a verdict\n" +
            "/heap?objects=1&top=25&sort=count&type=Texture2D&mark=a&diff=a\n" +
            "                       loaded Unity objects by type: count and native MB; type= that type's largest by name;\n" +
            "                       mark= keeps this census, diff= shows what changed since that mark (a hitch of its own)\n" +
            "/heap?statics=1&mod=Name&top=25&mark=a&diff=a\n" +
            "                       every plugin's static collections by size (items, nested items one level down), per mod and\n" +
            "                       per field, and classes whose static constructor threw; mark=/diff= as for objects",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            if (request.Has("seconds")) HeapWatch.Start(request);
            else if (request.Flag("objects")) request.Json(UnityObjectCensus.Reply(request));
            else if (request.Flag("statics")) request.Json(StaticCensus.Reply(request));
            else request.Json(HeapNow.Snapshot());
        }
    }
}
