using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using OpenKeep.Capacity;
using OpenKeep.Core;
using PatchGuard;
using UnityEngine;

namespace OpenKeep.Stacks
{
    /// <summary>
    /// OpenKeep.Items.txt and OpenKeep.Containers.txt next to the .cfg: every item with its prefab name, display
    /// name, type, vanilla and current stack and weight, and every container prefab with its vanilla size. Written
    /// on load when "Write Documentation" is on and by the console command "openkeep write docs" (by reflection).
    /// </summary>
    public static class Documentation
    {
        public const string ItemsFile = "OpenKeep.Items.txt";
        public const string ContainersFile = "OpenKeep.Containers.txt";

        public static void Write()
        {
            string folder = Path.GetDirectoryName(StacksModule.Synced.Config.ConfigFilePath);
            WriteItems(Path.Combine(folder, ItemsFile));
            WriteContainers(Path.Combine(folder, ContainersFile));
        }

        /// <summary>Writes the files when the setting is on and the item database and the scene are there.</summary>
        public static void WriteIfReady()
        {
            if (!StacksSettings.WriteDocumentation.Value || ZNetScene.instance == null)
                return;
            ObjectDB db = ObjectDB.instance;
            if (db == null || db.m_items == null || db.m_items.Count == 0)
                return;
            Guard.Run("write documentation", Write);
        }

        private static void WriteItems(string path)
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null)
                return;
            StringBuilder text = new StringBuilder();
            text.AppendLine("prefab\tname\ttype\tvanilla stack\tvanilla weight\tstack\tweight");
            List<ItemDrop> prefabs = StackValues.Prefabs(db).OrderBy(p => p.name, System.StringComparer.OrdinalIgnoreCase).ToList();
            foreach (ItemDrop prefab in prefabs)
                text.AppendLine(ItemLine(prefab));
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
            Plugin.Log.LogInfo($"OpenKeep: wrote {path} ({prefabs.Count} items).");
        }

        private static string ItemLine(ItemDrop prefab)
        {
            ItemDrop.ItemData.SharedData shared = prefab.m_itemData.m_shared;
            ItemValue vanilla = VanillaValues.Remember(prefab.name, shared);
            string display = Language.Localize(shared.m_name ?? "").Replace('\t', ' ').Replace('\n', ' ');
            return string.Join("\t", prefab.name, display, shared.m_itemType.ToString(),
                vanilla.Stack.ToString(), Number(vanilla.Weight), shared.m_maxStackSize.ToString(), Number(shared.m_weight));
        }

        private static string Number(float value) => value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        private static void WriteContainers(string path)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return;
            StringBuilder text = new StringBuilder();
            text.AppendLine("prefab\tvanilla width\tvanilla height");
            int count = 0;
            foreach (KeyValuePair<string, Container> prefab in ContainerPrefabs.All().OrderBy(p => p.Key, System.StringComparer.OrdinalIgnoreCase))
            {
                ContainerSize vanilla = VanillaSizes.Remember(prefab.Key, prefab.Value);
                text.AppendLine(string.Join("\t", prefab.Key, vanilla.Width.ToString(), vanilla.Height.ToString()));
                count++;
            }
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
            Plugin.Log.LogInfo($"OpenKeep: wrote {path} ({count} containers).");
        }

        /// <summary>The scene comes after the database in the game scene: write once both exist.</summary>
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class ScenePatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Low)]
            private static void Postfix() => WriteIfReady();
        }
    }
}
