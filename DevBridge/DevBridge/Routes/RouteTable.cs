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
            BurstRoute.Register(router);
            UiRoute.Register(router);
            PointerRoute.Register(router);
            KeyRoute.Register(router);
            MouseRoute.Register(router);
            ConsoleRoute.Register(router);
            LogRoute.Register(router);
            EventsRoute.Register(router);
            EvalRoute.Register(router);
            WorldRoute.Register(router);
            ConfigRoute.Register(router);
            SyncRoute.Register(router);
            BundleRoute.Register(router);
            PrefabsRoute.Register(router);
            PlaceRoute.Register(router);
            AnimateRoute.Register(router);
            SwapRoute.Register(router);
            EffectRoute.Register(router);
            FrameRoute.Register(router);
            HitboxRoute.Register(router);
            TimeRoute.Register(router);
            OverlayRoute.Register(router);
            TuneRoute.Register(router);
            TraceRoute.Register(router);
            ScenarioRoute.Register(router);
            PerfRoute.Register(router);
            ReloadRoute.Register(router);
            return router;
        }
    }
}
