using System;
using System.IO;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// What the Blueprints tab remembers on this machine only (never synced): the folder it showed, whether it was the
    /// last tab shown with the hammer and the Construction ghosts switch, in BepInEx/config/OpenKeep.BlueprintsTab.txt
    /// ("folder=houses/nordic", "tab=true", "ghosts=hidden"). Read once when first needed (a folder that no longer
    /// exists falls back to the top), written whenever one of them changes. The build menu itself keeps its tab only while the build tool stays the same, and a new
    /// menu (another world, a restart) starts on its first tab, so the menu is put back on the Blueprints tab when it
    /// opens with the hammer and that was the last tab shown (<see cref="BlueprintTab.Opened"/>).
    /// </summary>
    public static class TabMemory
    {
        private const string FileName = "OpenKeep.BlueprintsTab.txt";

        private static bool loaded;
        private static string savedFolder;
        private static bool savedTab;
        private static bool savedGhosts = true;

        /// <summary>The Blueprints tab was the last tab shown with the hammer.</summary>
        public static bool OnTab { get; set; }

        /// <summary>This player wants construction site ghosts drawn (<see cref="GhostSwitch"/>).</summary>
        public static bool GhostsShown { get; set; } = true;

        private static string PathOf => System.IO.Path.Combine(BepInEx.Paths.ConfigPath, FileName);

        /// <summary>Reads the file once and opens the remembered folder when it still exists.</summary>
        public static void Load()
        {
            if (loaded)
                return;
            loaded = true;
            savedFolder = "";
            BlueprintSafe.Run("OpenKeep blueprints tab memory", Read);
            OnTab = savedTab;
            GhostsShown = savedGhosts;
            if (savedFolder.Length > 0 && !BlueprintLibrary.Open(savedFolder))
                savedFolder = "";
        }

        private static void Read()
        {
            if (!File.Exists(PathOf))
                return;
            foreach (string line in File.ReadAllLines(PathOf))
            {
                int equals = line.IndexOf('=');
                string key = equals > 0 ? line.Substring(0, equals).Trim() : "";
                string value = equals > 0 ? line.Substring(equals + 1).Trim() : "";
                if (key == "folder")
                    savedFolder = value.Contains("..") ? "" : value.Trim('/');
                else if (key == "tab")
                    savedTab = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                else if (key == "ghosts")
                    savedGhosts = !string.Equals(value, "hidden", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Per frame: writes the file when the folder shown, the tab flag or the ghosts switch changed since it was last written.</summary>
        public static void Tick()
        {
            if (!loaded || (BlueprintLibrary.CurrentFolder == savedFolder && OnTab == savedTab && GhostsShown == savedGhosts))
                return;
            savedFolder = BlueprintLibrary.CurrentFolder;
            savedTab = OnTab;
            savedGhosts = GhostsShown;
            BlueprintSafe.Run("OpenKeep blueprints tab memory", Write);
        }

        private static void Write()
        {
            File.WriteAllLines(PathOf, new[]
            {
                "# OpenKeep: the folder the hammer's Blueprints tab showed last, whether it was the last tab shown, and",
                "# whether construction site ghosts are shown. Written by the game on this machine only; delete it to start over.",
                "folder=" + savedFolder,
                "tab=" + (savedTab ? "true" : "false"),
                "ghosts=" + (savedGhosts ? "shown" : "hidden"),
            });
        }
    }
}
