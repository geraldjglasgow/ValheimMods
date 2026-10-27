using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The tool switch's hover (<see cref="KwToolSwitch"/>): while a Broadside tool is in hand and the crosshair is on a
    /// part a click would switch to another tool for, the tool hint line (<see cref="KwToolHint"/>) says so once ("Click:
    /// change this mast (switches to the Sail tool)"), again only when the part or the tool changes. Looked at a few times a
    /// second; a moment after the crosshair leaves such a part the line is cleared, unless something else was said on it
    /// since. Local to the player's screen.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwToolSwitchHint
    {
        private const float Every = 0.15f, Gone = 0.3f;
        private static float next, seen, saidAt = -1f;
        private static string said = "";

        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside tool switch hint", Hover, __instance);

        /// <summary>What was said forgotten (a click switched), so the line is never cleared over the click's own words.</summary>
        public static void Forget() => said = "";

        private static void Hover(Player player)
        {
            if (player != Player.m_localPlayer || Time.time < next)
                return;
            next = Time.time + Every;
            string? tool = KwToolSwitch.Held(player);
            KwSwitchTarget? target = tool != null && !KwForm.IsOpen ? KwToolSwitch.Target(player, tool) : null;
            if (target != null)
                Say(player, tool + "|" + target.Tool + "|" + target.Key, target.Hint);
            else if (said.Length > 0 && Time.time - seen > Gone)
                Clear(player, tool != null);
        }

        private static void Say(Player player, string key, string hint)
        {
            seen = Time.time;
            if (key == said)
                return;
            said = key;
            player.Message(MessageHud.MessageType.Center, hint);
            saidAt = KwToolHint.Since;
        }

        // The hint taken off the line while a tool is still in hand and nothing else was said on it since.
        private static void Clear(Player player, bool held)
        {
            said = "";
            if (held && KwToolHint.Since == saidAt)
                player.Message(MessageHud.MessageType.Center, "");
        }
    }
}
