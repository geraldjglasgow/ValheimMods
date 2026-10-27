using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ready-made ships that come with the mod (the user, 2026-10-26: "the ready made ships need to be packed into the
    /// mod"): blueprints embedded as <c>Broadside.ships.&lt;Name&gt;.json</c>, written on start into the player's ships'
    /// folder under <see cref="Folder"/> (so the hammer lists them as "Ship: Ready-made/Black_Gull"), with the sail
    /// pictures they wear (<c>Broadside.sails.&lt;file&gt;</c>) into the sail pictures' folder. A file there already is never
    /// written over, and a ship the player has deleted is not brought back: the names written are kept in
    /// <see cref="Record"/>, and each name is written once. A later version's new ships are written when it first starts.
    /// Only where there is a screen (a dedicated server has no hammer).
    /// </summary>
    public static class KwReadyShips
    {
        public const string Folder = "Ready-made", Record = ".installed";
        private const string Ships = "Broadside.ships.", Sails = "Broadside.sails.";

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

        // Each embedded file under a prefix into a folder, once; with a record, never again after it was written.
        private static void Write(string folder, string prefix, bool recorded)
        {
            Assembly own = Assembly.GetExecutingAssembly();
            Directory.CreateDirectory(folder);
            string record = Path.Combine(folder, Record);
            HashSet<string> done = recorded && File.Exists(record) ? new HashSet<string>(File.ReadAllLines(record)) : new HashSet<string>();
            int written = 0;
            foreach (string resource in own.GetManifestResourceNames())
            {
                string file = resource.StartsWith(prefix, StringComparison.Ordinal) ? resource.Substring(prefix.Length) : "";
                if (file.Length == 0 || !done.Add(file))
                    continue;
                string path = Path.Combine(folder, file);
                if (File.Exists(path))
                    continue;
                Copy(own, resource, path);
                written++;
            }
            if (recorded)
                File.WriteAllLines(record, done);
            if (written > 0)
                Log.Info($"Broadside: {written} ready-made file(s) written to {folder}");
        }

        private static void Copy(Assembly own, string resource, string path)
        {
            using Stream from = own.GetManifestResourceStream(resource);
            using FileStream to = File.Create(path);
            from.CopyTo(to);
        }
    }
}
