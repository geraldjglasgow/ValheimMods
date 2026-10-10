using System;
using System.Collections.Generic;

namespace DevBridge.Server
{
    internal delegate void Handler(BridgeRequest request);

    /// <summary>Maps paths to handlers and runs each handler on the game's main thread.</summary>
    internal sealed class Router
    {
        private readonly Dictionary<string, Handler> routes = new Dictionary<string, Handler>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> usage = new List<string>();

        internal void Add(string path, string help, Handler handler)
        {
            routes[path] = handler;
            usage.Add(help);
        }

        internal string Help() =>
            $"DevBridge {DevBridgePlugin.PluginVersion} on port {DevBridgePlugin.Instance.Port}. " +
            "GET or POST; arguments as query or form fields (curl -G --data-urlencode \"expr=...\").\n\n" +
            string.Join("\n", usage);

        /// <summary>Called on an HTTP thread: queues the handler for the main thread and waits for its reply.</summary>
        internal Reply Dispatch(BridgeRequest request)
        {
            string path = request.Path.Length == 0 ? "/help" : request.Path;
            if (!routes.TryGetValue(path, out Handler handler))
                return Reply.Error(404, $"no endpoint {path}\n\n{Help()}");
            MainThread.Post(() => Run(handler, request));
            if (request.Wait(request.Patience)) return request.Result;
            return Reply.Error(504, "the game did not answer in time: still loading, frozen, or the call needs a larger timeout=");
        }

        /// <summary>
        /// Called on the main thread (a shot's cue): runs the handler here and now and returns the request, whose reply
        /// may come later from a coroutine.
        /// </summary>
        internal BridgeRequest Invoke(string path, IDictionary<string, string> args)
        {
            BridgeRequest request = BridgeRequest.Make(path, args);
            if (!routes.TryGetValue(request.Path, out Handler handler)) throw new BridgeException($"no endpoint {request.Path}");
            Run(handler, request);
            return request;
        }

        private static void Run(Handler handler, BridgeRequest request)
        {
            try
            {
                handler(request);
            }
            catch (Exception error)
            {
                request.Finish(Reply.FromException(error));
            }
        }
    }
}
