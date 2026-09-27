using System.Collections.Generic;
using System.Globalization;
using System.Text;
using YamlConfig;

namespace EarthWright.Protection
{
    /// <summary>
    /// EarthWright.Zones.yml as read by the YAML hub: a <c>zones:</c> list of name, x, z, radius and an optional player.
    /// A zone with a bad or duplicate name or without a position is skipped with a warning; a radius outside 5-200 m is
    /// clamped; zones past the hundredth are dropped. Unparsable numbers reject the file (the previous zones stay).
    /// </summary>
    public sealed class ZoneFile : YamlModel
    {
        public readonly List<AdminZone> Zones = new List<AdminZone>();

        protected override void Read(YamlNode root)
        {
            YamlNode list = root.Get("zones");
            if (list.Kind == YamlNodeKind.Missing || list.Kind == YamlNodeKind.Null)
                return;
            foreach (YamlNode item in list.Items)
                ReadZone(item);
        }

        protected override void Verify()
        {
            if (Zones.Count <= AdminZone.MaxZones)
                return;
            Warnings.Add($"{Zones.Count} admin zones found; only the first {AdminZone.MaxZones} are used");
            Zones.RemoveRange(AdminZone.MaxZones, Zones.Count - AdminZone.MaxZones);
        }

        private void ReadZone(YamlNode item)
        {
            if (!item.Get("name").TryString(out string name) || !AdminZone.ValidName(name.Trim()))
            {
                item.Warn("a zone needs a name of letters, digits, '-' and '_' (at most 32); skipped");
                return;
            }
            name = name.Trim();
            if (Zones.Exists(z => z.NameIs(name)))
            {
                item.Warn($"a second zone named '{name}'; only the first is used");
                return;
            }
            if (!item.Get("x").TryFloat(out float x) || !item.Get("z").TryFloat(out float z))
            {
                item.Warn($"zone '{name}' needs x and z; skipped");
                return;
            }
            Zones.Add(new AdminZone { Name = name, X = x, Z = z, Radius = ReadRadius(item, name), Player = ReadPlayer(item.Get("player")) });
        }

        private static float ReadRadius(YamlNode item, string name)
        {
            if (!item.Get("radius").TryFloat(out float radius))
                return 20f;
            float clamped = AdminZone.ClampRadius(radius);
            if (clamped != radius)
                item.Warn($"zone '{name}' radius {radius} is outside {AdminZone.MinRadius}-{AdminZone.MaxRadius} m; {clamped} is used");
            return clamped;
        }

        /// <summary>A key without a value counts as "no player" rather than an error.</summary>
        private static string ReadPlayer(YamlNode node)
        {
            if (node.Kind != YamlNodeKind.Scalar || !node.TryString(out string player))
                return "";
            return player.Trim();
        }
    }

    /// <summary>Writes the zone file the server keeps: a fixed explanatory header and the zones.</summary>
    public static class ZoneYaml
    {
        public const string Header =
            "# EarthWright admin zones.\n" +
            "# The server owns this file. Admins change it in game with 'ew zone add <name> <radius> [player]' (the zone is\n" +
            "# centred on the admin) and 'ew zone remove <name>'; the server then rewrites this file. It may also be edited by\n" +
            "# hand: it is reloaded within five seconds and pushed to every player.\n" +
            "# Zones only matter while 'Admin Zone Mode' in section '11. Protection' is not Off:\n" +
            "#   OnlyInsideZones:  terrain can only be changed inside a zone (a zone naming a player is for that player only).\n" +
            "#   NeverInsideZones: terrain can never be changed inside a zone (except by the player a zone names).\n" +
            "# Each zone: name (letters, digits, '-' and '_'), x and z (world position of the centre), radius (5 to 200 m),\n" +
            "# player (optional character name). At most 100 zones. Example:\n" +
            "#   zones:\n" +
            "#     - name: spawn\n" +
            "#       x: 0\n" +
            "#       z: 0\n" +
            "#       radius: 60\n";

        public static string Write(IEnumerable<AdminZone> zones)
        {
            StringBuilder text = new StringBuilder(Header);
            StringBuilder body = new StringBuilder();
            foreach (AdminZone zone in zones)
                AppendZone(body, zone);
            text.Append(body.Length == 0 ? "zones: []\n" : "zones:\n" + body);
            return text.ToString();
        }

        private static void AppendZone(StringBuilder text, AdminZone zone)
        {
            text.Append("  - name: ").Append(Quote(zone.Name)).Append('\n');
            text.Append("    x: ").Append(Number(zone.X)).Append('\n');
            text.Append("    z: ").Append(Number(zone.Z)).Append('\n');
            text.Append("    radius: ").Append(Number(zone.Radius)).Append('\n');
            if (!string.IsNullOrEmpty(zone.Player))
                text.Append("    player: ").Append(Quote(zone.Player)).Append('\n');
        }

        private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
