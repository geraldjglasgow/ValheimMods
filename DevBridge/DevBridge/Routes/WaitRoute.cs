using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Logs;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/wait: block until time passes or the game reaches a state, so callers need no sleep loops.</summary>
    internal static class WaitRoute
    {
        internal static void Register(Router router) => router.Add("/wait",
            "/wait?seconds=N        wait N real seconds (max 600)\n" +
            "/wait?for=menu|player|log&grep=text&since=N&timeout=120\n" +
            "                       until the main menu shows, the local player is in the world with no loading screen,\n" +
            "                       or a log line containing grep appears (after since=, default: from now)",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            string what = request.Get("for");
            Func<string> condition = what == null ? After(request.Float("seconds", 1f)) : Condition(what, request);
            float timeout = what == null ? request.Float("seconds", 1f) + 1f : request.Float("timeout", 30f);
            Async.Start(request, Until(request, condition, timeout));
        }

        private static Func<string> After(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            return () => Time.realtimeSinceStartup >= end ? $"waited {seconds} s" : null;
        }

        private static Func<string> Condition(string what, BridgeRequest request)
        {
            switch (what.ToLowerInvariant())
            {
                case "menu": return () => StatusRoute.State() == "menu" ? "main menu" : null;
                case "player": return () => PlayerReady() ? "player in the world" : null;
                case "log": return LogMatch(request.Require("grep"), request.Has("since") ? request.Int("since", 0) : LogCapture.Lines.Next);
                default: throw new BridgeException("for= takes menu, player or log");
            }
        }

        private static bool PlayerReady()
        {
            Player player = Player.m_localPlayer;
            if (!player || player.IsTeleporting()) return false;
            return !Hud.instance || !Hud.instance.m_loadingScreen.gameObject.activeInHierarchy || Hud.instance.m_loadingScreen.alpha < 0.05f;
        }

        // Each look reads only the lines logged since the one before (a line's text is put together when read).
        private static Func<string> LogMatch(string grep, long since)
        {
            long cursor = since;
            Func<LineBuffer.Line, bool> match = l => l.Text.IndexOf(grep, StringComparison.OrdinalIgnoreCase) >= 0;
            return () =>
            {
                long end = LogCapture.Lines.Next;
                List<LineBuffer.Line> hits = LogCapture.Lines.Since(cursor, 1, match);
                cursor = end;
                return hits.Count == 0 ? null : $"{hits[0].Seq} {hits[0].Text}";
            };
        }

        private static IEnumerator Until(BridgeRequest request, Func<string> condition, float timeout)
        {
            float start = Time.realtimeSinceStartup;
            string met;
            while ((met = condition()) == null && Time.realtimeSinceStartup - start < timeout) yield return null;
            request.Json(new Dictionary<string, object>
            {
                ["met"] = met != null,
                ["detail"] = met ?? $"timed out after {timeout} s",
                ["seconds"] = Fmt.R(Time.realtimeSinceStartup - start),
                ["state"] = StatusRoute.State(),
            });
        }
    }
}
