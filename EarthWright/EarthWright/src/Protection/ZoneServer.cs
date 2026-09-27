using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlConfig;

namespace EarthWright.Protection
{
    /// <summary>
    /// Server side of zone changes. The request has already been checked against the server's admin list
    /// (<see cref="ZoneRpc"/>). A change rewrites EarthWright.Zones.yml through the YAML hub, which validates it, writes
    /// it back to disk and applies it again; applying publishes the new zones to every player. The reply is a line of
    /// $tokens and values that the requesting admin's machine localizes.
    /// </summary>
    public static class ZoneServer
    {
        public static string Add(AdminZone zone)
        {
            if (zone == null || !AdminZone.ValidName(zone.Name))
                return ZoneWords.BadName;
            List<AdminZone> zones = new List<AdminZone>(ZoneBook.FileZones);
            int index = zones.FindIndex(z => z.NameIs(zone.Name));
            if (index < 0 && zones.Count >= AdminZone.MaxZones)
                return ZoneWords.Full;
            zone.Radius = AdminZone.ClampRadius(zone.Radius);
            zone.Player = (zone.Player ?? "").Trim();
            if (index >= 0)
                zones[index] = zone;
            else
                zones.Add(zone);
            Save(zones);
            return (index >= 0 ? ZoneWords.Replaced : ZoneWords.Added) + " " + zone.Describe();
        }

        public static string Remove(string name)
        {
            List<AdminZone> zones = new List<AdminZone>(ZoneBook.FileZones);
            int removed = zones.RemoveAll(z => z.NameIs(name));
            if (removed == 0)
                return ZoneWords.Unknown + " '" + name + "'";
            Save(zones);
            return ZoneWords.Removed + " " + name;
        }

        /// <summary>
        /// Writes every zone into the main file and empties any other copy (a second EarthWright.Zones.yml next to the
        /// plugin), so a later reload cannot bring a removed zone back.
        /// </summary>
        private static void Save(List<AdminZone> zones)
        {
            YamlFileSet set = ZoneBook.Set;
            if (set == null || Plugin.Synced == null)
                return;
            string main = MainPath(set);
            Dictionary<string, string> files = new Dictionary<string, string> { [main] = ZoneYaml.Write(zones) };
            foreach (string path in set.Files.Keys.Where(p => !SamePath(p, main)))
                files[path] = ZoneYaml.Write(Enumerable.Empty<AdminZone>());
            Plugin.Synced.Yaml.Replace(set, files, saveToDisk: true);
            if (set.Current is ZoneFile file)
                ZoneBook.Apply(file);
        }

        private static string MainPath(YamlFileSet set)
        {
            string first = set.Files.Keys.FirstOrDefault();
            if (!string.IsNullOrEmpty(first))
                return first;
            string folder = Plugin.Synced.SearchPaths.Count > 0 ? Plugin.Synced.SearchPaths[0] : BepInEx.Paths.ConfigPath;
            return Path.Combine(folder, set.MainFileName);
        }

        private static bool SamePath(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
    }
}
