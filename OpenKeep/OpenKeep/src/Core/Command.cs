using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace OpenKeep.Core
{
    /// <summary>
    /// The <c>openkeep</c> console command: <c>reload</c> (cfg and every YAML file; admin or host on a server),
    /// <c>containers</c> (the reachable containers around the player), <c>write docs</c> (the Stacks module's
    /// documentation files) and <c>signs</c> (the Signs module's list, reset and rewrite), the last two through
    /// reflection so Core does not depend on those modules.
    /// </summary>
    public static class Command
    {
        private const float ListRange = 20f;
        private static readonly string[] SubCommands = { "reload", "containers", "write", "signs" };
        private static bool registered;

        internal static void Register()
        {
            if (registered)
                return;
            registered = true;
            new Terminal.ConsoleCommand("openkeep", "OpenKeep: openkeep reload | containers | write docs | signs [reset | rewrite]",
                (Terminal.ConsoleEvent)(args => Guard.Run("openkeep command", () => Run(args))),
                optionsFetcher: () => new List<string>(SubCommands));
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
            switch (sub)
            {
                case "reload": Reload(args); break;
                case "containers": Containers(args); break;
                case "write": WriteDocs(args); break;
                case "signs": Signs(args); break;
                default: Help(args); break;
            }
        }

        private static void Help(Terminal.ConsoleEventArgs args)
        {
            args.Context.AddString("openkeep reload        reloads the cfg and every OpenKeep YAML file (admin or host on a server)");
            args.Context.AddString("openkeep containers    lists the reachable containers within 20 m");
            args.Context.AddString("openkeep write docs    writes OpenKeep.Items.txt and OpenKeep.Containers.txt next to the cfg");
            args.Context.AddString("openkeep signs         lists the loaded containers with their sign state");
            args.Context.AddString("openkeep signs reset   allows signs again on containers whose sign was removed with the hammer (admin or host)");
            args.Context.AddString("openkeep signs rewrite rewrites the sign of every loaded container this game owns");
        }

        private static void Reload(Terminal.ConsoleEventArgs args)
        {
            if (ZNet.instance != null && !ZNet.instance.LocalPlayerIsAdminOrHost())
            {
                args.Context.AddString("OpenKeep: only an admin or the host can reload on a server.");
                return;
            }
            Plugin.Synced.Config.Reload();
            Plugin.Synced.Yaml.LoadAll();
            Plugin.Synced.Yaml.ApplyAll();
            args.Context.AddString("OpenKeep: configuration and YAML files reloaded.");
        }

        private static void Containers(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                args.Context.AddString("OpenKeep: no local player.");
                return;
            }
            Vector3 position = player.transform.position;
            List<Container> found = ContainerScan.Nearby(position, ListRange, ContainerUse.Reach);
            args.Context.AddString($"OpenKeep: {found.Count} reachable containers within {ListRange:0} m ({ContainerScan.All().Count} loaded in total):");
            foreach (Container container in found)
            {
                float distance = Vector3.Distance(position, container.transform.position);
                Inventory inventory = container.GetInventory();
                args.Context.AddString($"  {ContainerScan.PrefabName(container)}  {distance:0.0} m  {inventory.NrOfItems()} stacks, {inventory.NrOfItemsIncludingStacks()} items");
            }
        }

        private static void WriteDocs(Terminal.ConsoleEventArgs args)
        {
            if (args.Length < 3 || !string.Equals(args[2], "docs", StringComparison.OrdinalIgnoreCase))
            {
                args.Context.AddString("Usage: openkeep write docs");
                return;
            }
            Type documentation = typeof(Command).Assembly.GetType("OpenKeep.Stacks.Documentation");
            MethodInfo write = documentation?.GetMethod("Write", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (write == null)
            {
                args.Context.AddString("OpenKeep: the Stacks module is not built; there is nothing to write.");
                return;
            }
            write.Invoke(null, null);
            args.Context.AddString("OpenKeep: documentation files written next to the cfg.");
        }

        private static void Signs(Terminal.ConsoleEventArgs args)
        {
            Type command = typeof(Command).Assembly.GetType("OpenKeep.Signs.SignsCommand");
            MethodInfo run = command?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Terminal.ConsoleEventArgs) }, null);
            if (run == null)
            {
                args.Context.AddString("OpenKeep: the Signs module is not built.");
                return;
            }
            run.Invoke(null, new object[] { args });
        }
    }

    /// <summary>Registers the command once the game has set up its own; InitTerminal itself runs only once.</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class CommandRegisterPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => Command.Register();
    }
}
