using System.IO;
using System.Net;
using DevBridge.Server;

namespace DevBridge.Studio
{
    /// <summary>
    /// GET /studio: the studio page (Studio/studio.html, embedded), with this start's token written in, served on the HTTP
    /// thread. It may not be framed, loads nothing from elsewhere and calls only this host (<see cref="StudioAccess"/>).
    /// </summary>
    internal static class StudioPage
    {
        private const string Resource = "DevBridge.Studio.studio.html";
        private const string Rules = "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; " +
            "img-src 'self' data:; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

        private static string page;

        internal static Reply Answer(BridgeRequest request, HttpListenerResponse response)
        {
            var reply = new Reply(200, "text/html", Page().Replace("__STUDIO_TOKEN__", StudioAccess.Token));
            reply.Headers["Content-Security-Policy"] = Rules;
            reply.Headers["X-Frame-Options"] = "DENY";
            reply.Headers["Cache-Control"] = "no-store";
            reply.Headers["Referrer-Policy"] = "same-origin";
            return reply;
        }

        private static string Page()
        {
            if (page != null) return page;
            using (Stream stream = typeof(StudioPage).Assembly.GetManifestResourceStream(Resource))
            {
                if (stream == null) throw new BridgeException($"the studio page {Resource} is missing from this build");
                using (var reader = new StreamReader(stream)) page = Director.ScenesPage.AddTo(reader.ReadToEnd());
            }
            return page;
        }
    }
}
