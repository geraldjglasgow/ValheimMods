using System;
using System.Net;
using System.Security.Cryptography;

namespace DevBridge.Server
{
    /// <summary>
    /// The one browser page DevBridge answers: /studio, served by DevBridge itself with a token made fresh at each start.
    /// The page opens only from the address bar or a reload (Sec-Fetch-Site none or same-origin), never from a link or a
    /// frame on another site, and may not be framed. Its calls carry the token in a header; another page can neither read
    /// the token (the same-origin rule) nor send that header to 127.0.0.1 without a preflight it fails. With the token a call
    /// reaches the /studio/ endpoints only, so the page can move and play things but never run /eval or /reload.
    /// </summary>
    internal static class StudioAccess
    {
        internal const string Header = "X-DevBridge-Studio";

        internal static readonly string Token = NewToken();

        internal static bool Allows(HttpListenerRequest request)
        {
            string path = request.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            if (path == "/studio") return Opened(request);
            return path.StartsWith("/studio/") && SameOrigin(request) && request.Headers[Header] == Token;
        }

        private static bool Opened(HttpListenerRequest request)
        {
            string site = request.Headers["Sec-Fetch-Site"];
            return request.HttpMethod == "GET" && (site == "none" || site == "same-origin");
        }

        // The page's own calls: same-origin, and an Origin (sent with a POST) naming this very host and port.
        private static bool SameOrigin(HttpListenerRequest request)
        {
            string site = request.Headers["Sec-Fetch-Site"];
            if (!string.IsNullOrEmpty(site) && site != "same-origin") return false;
            string origin = request.Headers["Origin"];
            return string.IsNullOrEmpty(origin) || origin.Equals("http://" + request.UserHostName, StringComparison.OrdinalIgnoreCase);
        }

        private static string NewToken()
        {
            var bytes = new byte[24];
            using (var random = new RNGCryptoServiceProvider()) random.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
