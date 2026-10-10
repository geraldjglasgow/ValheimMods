using DevBridge.Server;
using DevBridge.Studio;

namespace DevBridge.Routes
{
    /// <summary>
    /// The studio's Workshop tab (also usable from curl): the asset workshop's built bundles, and one opened (loaded from
    /// its file, again after a rebuild) as its models and effects, which /studio/view draws with bundle=.
    /// </summary>
    internal static class StudioWorkshopRoute
    {
        private const string Pad = "\n                       ";

        internal static void Register(Router router)
        {
            router.Add("/studio/workshop", "/studio/workshop" + Pad +
                "the workshop's bundles built for Windows (Workshop folder under [Studio]), newest first: loaded, rebuilt since, or held by a mod", List);
            router.Add("/studio/workshop/open", "/studio/workshop/open?bundle=<name>&reload=1" + Pad +
                "load a workshop bundle (again when rebuilt since, or with reload=1) and list its models and effects in groups", Open);
        }

        private static void List(BridgeRequest request) => request.Json(StudioWorkshop.List());

        private static void Open(BridgeRequest request) =>
            request.Json(StudioWorkshop.Open(request.Require("bundle"), request.Flag("reload")));
    }
}
