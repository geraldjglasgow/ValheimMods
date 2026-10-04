using System;
using System.Net;

namespace DevBridge.Server
{
    /// <summary>
    /// Only local tools may call the bridge, never a web page: a page open in the player's browser can reach 127.0.0.1 with
    /// an image or a form and would otherwise run /eval or /reload inside the game. Browsers mark what they send with an
    /// Origin, Referer or Sec-Fetch-Site header, which curl, scripts and agent HTTP clients do not send. A DNS-rebinding page
    /// gets past those as same-origin, but its Host names its own domain, not 127.0.0.1 or localhost.
    /// </summary>
    internal static class BrowserGuard
    {
        private static readonly string[] BrowserHeaders = { "Origin", "Referer", "Sec-Fetch-Site" };

        /// <summary>Why a request is refused, or null when a local tool sent it.</summary>
        internal static string Refusal(HttpListenerRequest request)
        {
            foreach (string header in BrowserHeaders)
            {
                if (!string.IsNullOrEmpty(request.Headers[header]))
                    return $"a browser request ({header} header); DevBridge answers local tools only";
            }
            string host = HostName(request.UserHostName);
            if (host != "127.0.0.1" && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return $"host '{request.UserHostName}' is not 127.0.0.1 or localhost";
            return null;
        }

        /// <summary>The Host header without its port.</summary>
        private static string HostName(string hostHeader)
        {
            if (string.IsNullOrEmpty(hostHeader)) return "";
            int colon = hostHeader.LastIndexOf(':');
            return colon > 0 ? hostHeader.Substring(0, colon) : hostHeader;
        }
    }
}
