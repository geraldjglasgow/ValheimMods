using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ready-made ships that come with the mod (the user, 2026-10-26: "the ready made ships need to be packed into the
    /// mod"): blueprints embedded as <c>Broadside.ships.&lt;Name&gt;.json</c>, written on start into the player's ships'
    /// folder under <see cref="Folder"/> (so the hammer lists them as "Ship: Ready-made/Black_Gull"), with the sail
    /// pictures they wear (<c>Broadside.sails.&lt;file&gt;</c>) into the sail pictures' folder. A ship the player has
    /// deleted is not brought back, and a file the player made or changed is never written over: the names written are
    /// kept in <see cref="Record"/>, each with the hash of what was written ("name|hash"; a name alone from earlier
    /// versions). A copy still as the mod wrote it, or as an earlier version shipped it (<see cref="Superseded"/>), is
    /// written over when the mod's own has changed; a later version's new ships are written when it first starts.
    /// Only where there is a screen (a dedicated server has no hammer).
    /// </summary>
    public static class KwReadyShips
    {
        public const string Folder = "Ready-made", Record = ".installed";
        private const string Ships = "Broadside.ships.", Sails = "Broadside.sails.";

        /// <summary>
        /// Ready-made ships as earlier versions wrote them (<see cref="Hash"/>), written over by the mod's own: Wayfinder
        /// and Storm Petrel of 0.1.0 to 0.3.4, whose holds were 1.6 m high under a 1.85 m player.
        /// </summary>
        private static readonly HashSet<string> Superseded = new HashSet<string> { "8d354775cd7bd0cd", "5004000eabb0d106" };

        public static void Install()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            try
            {
                Write(Path.Combine(KwBpLibrary.Folder, Folder), Ships, true);
                Write(KwSailEmblems.Folder, Sails, false);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the ready-made ships could not be written: {e.Message}");
            }
        }

        // Each embedded file under a prefix into a folder (Place); with a record, what was written kept in it.
        private static void Write(string folder, string prefix, bool recorded)
        {
            Assembly own = Assembly.GetExecutingAssembly();
            Directory.CreateDirectory(folder);
            string record = Path.Combine(folder, Record);
            Dictionary<string, string> done = recorded ? Read(record) : new Dictionary<string, string>();
            int written = 0;
            foreach (string resource in own.GetManifestResourceNames())
            {
                string file = resource.StartsWith(prefix, StringComparison.Ordinal) ? resource.Substring(prefix.Length) : "";
                if (file.Length > 0 && Place(Bytes(own, resource), Path.Combine(folder, file), file, done))
                    written++;
            }
            if (recorded)
                File.WriteAllLines(record, done.Select(p => p.Value.Length == 0 ? p.Key : p.Key + "|" + p.Value));
            if (written > 0)
                Log.Info($"Broadside: {written} ready-made file(s) written to {folder}");
        }

        // One file into place: written when absent and never written before; written over when the copy there is the
        // mod's own (as recorded, or an earlier version's) and the mod's has changed. Any other file there is the player's.
        private static bool Place(byte[] mine, string path, string file, Dictionary<string, string> done)
        {
            string hash = Hash(mine);
            bool known = done.TryGetValue(file, out string? wrote);
            if (!File.Exists(path) && known)
                return false;
            if (File.Exists(path))
            {
                string there = Hash(File.ReadAllBytes(path));
                if (there == hash || (there != wrote && !Superseded.Contains(there)))
                {
                    done[file] = there == hash ? hash : known ? wrote! : "";
                    return false;
                }
            }
            File.WriteAllBytes(path, mine);
            done[file] = hash;
            return true;
        }

        // The record's lines, "name" or "name|hash"; a name with a hash wins over the same name alone.
        private static Dictionary<string, string> Read(string record)
        {
            Dictionary<string, string> done = new Dictionary<string, string>();
            if (!File.Exists(record))
                return done;
            foreach (string line in File.ReadAllLines(record))
            {
                int bar = line.IndexOf('|');
                string name = bar < 0 ? line : line.Substring(0, bar), hash = bar < 0 ? "" : line.Substring(bar + 1);
                if (name.Length > 0 && (!done.TryGetValue(name, out string? had) || had.Length == 0))
                    done[name] = hash;
            }
            return done;
        }

        /// <summary>FNV-1a, 64 bits, over a file's bytes less its carriage returns, as 16 hex digits.</summary>
        public static string Hash(byte[] bytes)
        {
            ulong hash = 14695981039346656037UL;
            foreach (byte b in bytes)
            {
                if (b != (byte)'\r')
                    hash = (hash ^ b) * 1099511628211UL;
            }
            return hash.ToString("x16");
        }

        private static byte[] Bytes(Assembly own, string resource)
        {
            using Stream from = own.GetManifestResourceStream(resource);
            using MemoryStream to = new MemoryStream();
            from.CopyTo(to);
            return to.ToArray();
        }
    }
}
