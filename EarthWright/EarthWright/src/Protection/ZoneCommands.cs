using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// <c>ew zone add &lt;name&gt; &lt;radius&gt; [player]</c>, <c>ew zone remove &lt;name&gt;</c>, <c>ew zone list</c>. Typed on
    /// the admin's own machine; add (centred on the admin's character) and remove are sent to the server, which checks
    /// the admin list and answers (<see cref="ZoneRpc"/>). The list shows the zones every check uses, so any player may
    /// ask for it.
    /// </summary>
    public static class ZoneCommands
    {
        public static void Register()
        {
            Command.Add("zone", "zone add <name> <radius> [player] | zone remove <name> | zone list   admin zones (5-200 m)", Run);
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            string verb = args.Length > 2 ? args[2].ToLowerInvariant() : "list";
            if (verb == "add")
                Add(args);
            else if (verb == "remove")
                Remove(args);
            else if (verb == "list")
                List(args);
            else
                Print(args, ZoneWords.Usage);
        }

        private static void Add(Terminal.ConsoleEventArgs args)
        {
            if (!MayChange(args))
                return;
            if (args.Length < 5 || !AdminZone.ValidName(args[3]) || !TryRadius(args[4], out float radius))
            {
                Print(args, args.Length >= 4 && !AdminZone.ValidName(args[3]) ? ZoneWords.BadName : ZoneWords.Usage);
                return;
            }
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                Print(args, ZoneWords.NoPlayer);
                return;
            }
            Vector3 at = player.transform.position;
            string owner = args.Length > 5 ? string.Join(" ", args.Args.Skip(5)) : "";
            AdminZone zone = new AdminZone { Name = args[3], X = at.x, Z = at.z, Radius = AdminZone.ClampRadius(radius), Player = owner };
            Print(args, ZoneRpc.SendAdd(zone) ? ZoneWords.Sent : ZoneWords.NoNetwork);
        }

        private static void Remove(Terminal.ConsoleEventArgs args)
        {
            if (!MayChange(args))
                return;
            if (args.Length < 4)
            {
                Print(args, ZoneWords.Usage);
                return;
            }
            Print(args, ZoneRpc.SendRemove(args[3]) ? ZoneWords.Sent : ZoneWords.NoNetwork);
        }

        private static void List(Terminal.ConsoleEventArgs args)
        {
            IReadOnlyList<AdminZone> zones = ZoneBook.Current;
            Print(args, ZoneWords.Mode + ": " + ProtectionSettings.Zones.Value);
            if (zones.Count == 0)
                Print(args, ZoneWords.NoZones);
            foreach (AdminZone zone in zones)
                args.Context.AddString("  " + zone.Describe());
        }

        /// <summary>A hint on this machine; the server decides with its own admin list.</summary>
        private static bool MayChange(Terminal.ConsoleEventArgs args)
        {
            if (Side.LocalIsAdmin)
                return true;
            Print(args, ZoneWords.NotAdmin);
            return false;
        }

        private static bool TryRadius(string text, out float radius)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out radius) && radius > 0f;
        }

        private static void Print(Terminal.ConsoleEventArgs args, string text)
        {
            args.Context.AddString("EarthWright: " + Language.Localize(text));
        }
    }
}
