using System.Collections.Generic;
using DevBridge.Hitbox;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/hitbox: turns the hit-shape view on or off and replies with the swings and hits it saw.</summary>
    internal static class HitboxRoute
    {
        internal static void Register(Router router)
        {
            router.Add("/hitbox",
                "/hitbox?on=1|off=1&seconds=1.5&players=0&last=20&clear=1\n" +
                "                       show hit shapes: each melee swing flashes its reach for seconds= (red at its height, orange on\n" +
                "                       the ground; players=1 adds players' swings), each hit on you draws a yellow line from the attacker;\n" +
                "                       replies with recent swings and hits: attack, range, distance centre to centre, gap body to body;\n" +
                "                       the Crypt Executioner's axe head (a mod's own hit) is drawn as a trail of rings",
                Hitbox);
        }

        private static void Hitbox(BridgeRequest request)
        {
            if (request.Has("on")) HitboxView.On = request.Flag("on");
            if (request.Flag("off")) HitboxView.On = false;
            if (request.Has("players")) HitboxView.Players = request.Flag("players");
            HitboxView.Seconds = Mathf.Clamp(request.Float("seconds", HitboxView.Seconds), 0.2f, 30f);
            if (request.Flag("clear")) HitboxView.Clear();
            if (HitboxView.On) ModHits.Hook();
            request.Json(new Dictionary<string, object>
            {
                ["on"] = HitboxView.On,
                ["seconds"] = HitboxView.Seconds,
                ["players"] = HitboxView.Players,
                ["mod_hits"] = ModHits.Hooked,
                ["events"] = HitboxView.Recent(request.Int("last", 20)),
            });
        }
    }
}
