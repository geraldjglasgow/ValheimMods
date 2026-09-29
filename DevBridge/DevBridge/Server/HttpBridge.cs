using System;
using System.Net;
using System.Text;
using System.Threading;
using BepInEx.Logging;

namespace DevBridge.Server
{
    /// <summary>The localhost HTTP listener. Each request is served on a pool thread that waits for the main thread.</summary>
    internal sealed class HttpBridge
    {
        private const int PortsToTry = 10;

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

        private void Serve(HttpListenerContext context)
        {
            Reply reply;
            try { reply = router.Dispatch(BridgeRequest.From(context.Request)); }
            catch (Exception error) { reply = Reply.FromException(error); }
            Write(context.Response, reply);
        }

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
