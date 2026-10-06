using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenKeep.Capacity
{
    /// <summary>
    /// OpenKeep.Stations.txt next to the .cfg: every station prefab with its vanilla caps (0 = the station takes no
    /// items or no fuel), tab separated. Written with the Stacks module's documentation files, only when its text
    /// changed unless forced (<see cref="Stacks.DocFile"/>).
    /// </summary>
    public static class StationDocumentation
    {
        public const string FileName = "OpenKeep.Stations.txt";

        public static void Write(string folder, bool force)
        {
            if (ZNetScene.instance == null)
                return;
            StringBuilder text = new StringBuilder();
            text.AppendLine("prefab\tvanilla items\tvanilla fuel");
            int count = 0;
            foreach (KeyValuePair<string, Smelter> prefab in StationPrefabs.All().OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                StationCaps vanilla = VanillaCaps.Remember(prefab.Key, prefab.Value);
                text.AppendLine(string.Join("\t", prefab.Key, vanilla.Items.ToString(), vanilla.Fuel.ToString()));
                count++;
            }
            string path = Path.Combine(folder, FileName);
            if (Stacks.DocFile.Write(path, text.ToString(), force))
                Plugin.Log.LogInfo($"OpenKeep: wrote {path} ({count} stations).");
        }
    }
}
