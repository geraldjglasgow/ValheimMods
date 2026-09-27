using System.Collections.Generic;
using System.Linq;
using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Menu;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// Turns edit sources into words for messages. Brush entries, ramps, roads and custom entries send their entry's
    /// piece id ("mud_road_v2", "ew_ramp", "ew_custom_x"): the entry's own name ("Level ground"). A special key
    /// ("ramp", "road") names the entry that has it. The reset keys and command send "reset"; console commands
    /// "command:&lt;name&gt;", shown as "ew &lt;name&gt;". Anything else is shown as it is.
    /// </summary>
    internal static class SourceNames
    {
        private const int Shown = 3;
        private const string CommandPrefix = "command:";
        private const string ResetSource = "reset";

        public static string Describe(IList<string> sources)
        {
            if (sources == null || sources.Count == 0)
                return "?";
            string names = string.Join(", ", sources.Take(Shown).Select(Describe));
            return sources.Count > Shown ? names + " +" + (sources.Count - Shown) : names;
        }

        public static string Describe(string source)
        {
            if (string.IsNullOrEmpty(source))
                return "?";
            if (source.StartsWith(CommandPrefix))
                return "ew " + source.Substring(CommandPrefix.Length);
            if (source.StartsWith(Snapshots.SourcePrefix))
                return Language.Format(HistoryWords.SnapshotSource, source.Substring(Snapshots.SourcePrefix.Length));
            if (source == ResetSource)
                return Language.Localize(HistoryWords.ResetSource);
            string pieceName = PieceName(source) ?? PieceName(SpecialEntry(source));
            return string.IsNullOrEmpty(pieceName) ? source : Language.Localize(pieceName);
        }

        /// <summary>The id of the entry whose special key this is ("ramp" gives the ramp entry), or null.</summary>
        private static string SpecialEntry(string key)
        {
            ToolAction action = ActionCatalog.All.FirstOrDefault(a => a.IsSpecial && a.Special == key);
            return action?.Id;
        }

        /// <summary>The $token name of the entry piece with this prefab name, or null.</summary>
        private static string PieceName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return null;
            GameObject prefab = Safe.Call("EarthWright entry lookup", () => EntryRegistry.Prefab(prefabName), null);
            if (prefab == null && ZNetScene.instance != null)
                prefab = ZNetScene.instance.GetPrefab(prefabName);
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            return piece != null && !string.IsNullOrEmpty(piece.m_name) ? piece.m_name : null;
        }
    }
}
