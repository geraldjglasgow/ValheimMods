using DevBridge.Server;

namespace DevBridge.Routes
{
    /// <summary>Every endpoint, in the order /help lists them.</summary>
    internal static class RouteTable
    {
        internal static Router Build()
        {
            var router = new Router();
            router.Add("/help", "/help                  this list", request => request.Text(router.Help()));
            StatusRoute.Register(router);
            WaitRoute.Register(router);
            ScreenshotRoute.Register(router);
            UiRoute.Register(router);
            PointerRoute.Register(router);
            KeyRoute.Register(router);
            MouseRoute.Register(router);
            ConsoleRoute.Register(router);
            LogRoute.Register(router);
            EvalRoute.Register(router);
            WorldRoute.Register(router);
            BundleRoute.Register(router);
            PrefabsRoute.Register(router);
            PlaceRoute.Register(router);
            AnimateRoute.Register(router);
            EffectRoute.Register(router);
            FrameRoute.Register(router);
            return router;
        }
    }
}
