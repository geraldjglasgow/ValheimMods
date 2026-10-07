using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// Tells the local player the day's fortune: once when they arrive in the world and again whenever a new day starts
    /// (a top-left message, so it does not cover the game's own "Day N" banner). Checked every few seconds from the local
    /// player's update; nothing is said while Daily Fortune is off. The "fortune" console command prints it at any time.
    /// </summary>
    public static class FortuneNotice
    {
        private const float Interval = 3f;

        private static float timer;
        private static int toldDay = -1;
        private static Player toldPlayer;

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Tick
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                    return;
                timer += Time.deltaTime;
                if (timer < Interval)
                    return;
                timer = 0f;
                HookGuard.Run("fortune notice", static player => Check(player), __instance);
            }
        }

        private static void Check(Player player)
        {
            if (!Fortune.On || EnvMan.instance == null || WorldGenerator.instance == null)
                return;
            int day = EnvMan.instance.GetDay();
            if (day == toldDay && player == toldPlayer)
                return;
            toldDay = day;
            toldPlayer = player;
            player.Message(MessageHud.MessageType.TopLeft, Fortune.Line(Fortune.Today()));
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
        private static class Command
        {
            [HarmonyPostfix]
            private static void Postfix() =>
                _ = new Terminal.ConsoleCommand("fortune", "Hearthhold: today's fortune and what it does to star rolls.", Print);
        }

        private static void Print(Terminal.ConsoleEventArgs args)
        {
            if (!Fortune.On)
                args.Context.AddString("Daily Fortune is off on this server.");
            else
                args.Context.AddString(Fortune.Line(Fortune.Today()));
        }
    }
}
