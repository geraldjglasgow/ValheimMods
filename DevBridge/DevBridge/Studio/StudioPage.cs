using System.IO;
using System.Net;
using DevBridge.Server;

namespace DevBridge.Studio
{
    /// <summary>
    /// GET /studio: the studio page (Studio/studio.html, embedded, with its parts such as the Commands tab added before
    /// its body ends), with this start's token written in, served on the HTTP thread. It may not be framed, loads nothing
    /// from elsewhere and calls only this host (<see cref="StudioAccess"/>).
    /// </summary>
    internal static class StudioPage
    {
        private const string Resource = "DevBridge.Studio.studio.html";
        private static readonly string[] Parts = { "DevBridge.Studio.commands.html", "DevBridge.Studio.shots.html" };
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
            string built = Embedded(Resource);
            foreach (string part in Parts) built = built.Replace("</body>", Embedded(part) + "\n</body>");
            return page = built;
        }

        private static string Embedded(string resource)
        {
            using (Stream stream = typeof(StudioPage).Assembly.GetManifestResourceStream(resource))
            {
                if (stream == null) throw new BridgeException($"the studio page's {resource} is missing from this build");
                using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
            }
        }
    }
}
