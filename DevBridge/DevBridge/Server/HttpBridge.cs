using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using BepInEx.Logging;

namespace DevBridge.Server
{
    /// <summary>The localhost HTTP listener. Each request is served on a pool thread that waits for the main thread, or for
    /// a direct path (ServeDirect) answers there itself.</summary>
    internal sealed class HttpBridge
    {
        private const int PortsToTry = 10;

        private static readonly Dictionary<string, Func<BridgeRequest, HttpListenerResponse, Reply>> Direct =
            new Dictionary<string, Func<BridgeRequest, HttpListenerResponse, Reply>>(StringComparer.OrdinalIgnoreCase);

        private readonly Router router;
        private readonly ManualLogSource log;
        private HttpListener listener;

        internal int Port { get; private set; }

        internal HttpBridge(Router router, ManualLogSource log)
        {
            this.router = router;
            this.log = log;
        }

        internal void Start(int firstPort)
        {
            for (int port = firstPort; port < firstPort + PortsToTry; port++)
            {
                if (!TryListen(port)) continue;
                Port = port;
                new Thread(Loop) { IsBackground = true, Name = "DevBridge" }.Start();
                log.LogInfo($"DevBridge listening on http://127.0.0.1:{port}/");
                return;
            }
            log.LogError($"DevBridge found no free port in {firstPort}-{firstPort + PortsToTry - 1}");
        }

        private bool TryListen(int port)
        {
            var candidate = new HttpListener();
            candidate.Prefixes.Add($"http://127.0.0.1:{port}/");
            try
            {
                candidate.Start();
                listener = candidate;
                return true;
            }
            catch (Exception)
            {
                candidate.Close();
                return false;
            }
        }

        private void Loop()
        {
            while (listener != null && listener.IsListening)
            {
                HttpListenerContext context;
                try { context = listener.GetContext(); }
                catch (Exception) { return; }
                ThreadPool.QueueUserWorkItem(_ => Serve(context));
            }
        }

        /// <summary>
        /// Serves path on the HTTP thread itself, never the main thread, for calls that wait or stream (/events): the handler
        /// returns a reply, or null once it has written and closed the response itself. Register before Start.
        /// </summary>
        internal static void ServeDirect(string path, Func<BridgeRequest, HttpListenerResponse, Reply> handler) =>
            Direct[path] = handler;

        private void Serve(HttpListenerContext context)
        {
            string refusal = BrowserGuard.Refusal(context.Request);
            if (refusal != null)
            {
                log.LogWarning($"DevBridge refused {context.Request.Url?.AbsolutePath}: {refusal}");
                Write(context.Response, Reply.Error(403, refusal));
                return;
            }
            Reply reply;
            try { reply = Answer(BridgeRequest.From(context.Request), context.Response); }
            catch (Exception error) { reply = Reply.FromException(error); }
            if (reply != null) Write(context.Response, reply);
        }

        private Reply Answer(BridgeRequest request, HttpListenerResponse response) =>
            Direct.TryGetValue(request.Path, out var handler) ? handler(request, response) : router.Dispatch(request);

        private static void Write(HttpListenerResponse response, Reply reply)
        {
            byte[] body = Encoding.UTF8.GetBytes(reply.Body);
            try
            {
                response.StatusCode = reply.Status;
                response.ContentType = reply.ContentType + "; charset=utf-8";
                response.ContentLength64 = body.Length;
                response.OutputStream.Write(body, 0, body.Length);
            }
            catch (Exception)
            {
                // the caller hung up; nothing to tell it
            }
            finally
            {
                response.Close();
            }
        }

        internal void Stop()
        {
            HttpListener current = listener;
            listener = null;
            if (current == null) return;
            current.Stop();
            current.Close();
        }
    }
}
