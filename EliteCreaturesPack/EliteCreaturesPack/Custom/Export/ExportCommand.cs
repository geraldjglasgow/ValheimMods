using System;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Commands;
using EliteCreaturesPack.Custom.Humans;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// <c>ecp export &lt;prefab&gt;</c>: writes any creature (game, mod or custom) as a full definition to start from;
    /// <c>ecp export human</c>: a human definition to start a human from (features/custom-creatures.md section 7). Reached
    /// through <see cref="EcpCommands"/>, which has already confirmed the player is an admin (<see cref="CommandAccess"/>);
    /// <c>args[0]</c> is "ecp", <c>args[1]</c> "export", <c>args[2]</c> the prefab (any case) or "human". It runs on the
    /// machine of the admin who typed it, since every peer has every prefab (custom ones built from the server's
    /// definitions), and writes into that machine's config folder (<see cref="ExportFile"/>); the reply gives the path. A
    /// failure is logged and answered, never thrown.
    /// </summary>
    internal static class ExportCommand
    {
        public const string Usage = "ecp export <prefab> | ecp export human";

        public static void Run(Terminal.ConsoleEventArgs args)
        {
            string target = args.Length > 2 ? args[2].Trim() : "";
            if (target.Length == 0)
            {
                EcpCommands.Reply(args, "ecp export: name a creature prefab, or human: " + Usage + ".");
                return;
            }
            try
            {
                Export(args, target);
            }
            catch (Exception e)
            {
                Guard.Report(e, "ecp export");
                EcpCommands.Reply(args, $"ecp export: failed ({e.Message}); the log has the details.");
            }
        }

        private static void Export(Terminal.ConsoleEventArgs args, string target)
        {
            if (string.Equals(target, HumanBody.Base, StringComparison.OrdinalIgnoreCase))
            {
                Done(args, ExportFile.Write(HumanBody.Base, HumanExport.Text()), "a human to start from");
                return;
            }
            GameObject? prefab = FindCreature(target, out string refusal);
            if (prefab == null)
            {
                EcpCommands.Reply(args, refusal);
                return;
            }
            CustomPrefabs.TryGet(prefab.name, out CustomCreature? custom);
            string text = CreatureExport.Text(new ExportSource(prefab, custom), ExportFile.SuggestName(prefab.name));
            Done(args, ExportFile.Write(prefab.name, text), prefab.name + (custom != null ? ", a custom creature," : ""));
        }

        /// <summary>A creature prefab by name, exact first, then in any case; null with the reason otherwise.</summary>
        private static GameObject? FindCreature(string name, out string refusal)
        {
            ZNetScene scene = ZNetScene.instance;
            GameObject? prefab = scene.GetPrefab(name);
            if (prefab == null)
            {
                prefab = scene.m_prefabs.Find(candidate =>
                    candidate != null && string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase));
            }
            Character? character = prefab != null ? prefab.GetComponent<Character>() : null;
            refusal = prefab == null ? $"ecp export: no prefab is called '{name}'."
                : character == null ? $"ecp export: {prefab.name} is not a creature."
                : character is Player ? $"ecp export: {prefab.name} is the player; `ecp export human` starts a person."
                : "";
            return refusal.Length == 0 ? prefab : null;
        }

        // On a client the file lands in that player's own config folder: the server's files are not touched.
        private static void Done(Terminal.ConsoleEventArgs args, string path, string what)
        {
            bool client = ZNet.instance != null && !ZNet.instance.IsServer();
            Log.Info($"ecp export: {what} written to {path}");
            EcpCommands.Reply(args, $"ecp export: {what} written to {path}"
                + (client ? " (on this machine; the server's files are not changed)." : "."));
        }
    }
}
