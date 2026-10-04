using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace DevBridge.Swap
{
    /// <summary>
    /// The star looks of starred copies across a revert. LevelEffects tints a starred creature by putting a tinted copy
    /// of its body material in slot 0 (one per creature and level, kept for every later spawn); a copy spawned during the
    /// swap goes back to the prefab's unstarred body when the swap had put the bundle's materials on (replace) or changed
    /// their number. The level each body wore is read before the revert, from that cache (a mod may show stars on
    /// creatures that stay level 1), and LevelEffects puts the look on again afterwards, as at the spawn.
    /// </summary>
    internal static class StarLooks
    {
        /// <summary>Before the revert: each starred copy's level effects whose body the swap reached, with the level shown.</summary>
        internal static Dictionary<LevelEffects, int> Read(SwapEntry entry, IEnumerable<Target> targets)
        {
            var swapped = new HashSet<string>(entry.Pairs.Where(p => p.Bundle).Select(p => p.Path));
            var starred = new Dictionary<LevelEffects, int>();
            foreach (Target target in targets.Where(t => !t.IsPrefab && t.Root))
            {
                foreach (LevelEffects level in target.Root.GetComponentsInChildren<LevelEffects>(true))
                {
                    if (!level.m_character || !level.m_mainRender || !swapped.Contains(target.PrefabPath(level.m_mainRender.transform))) continue;
                    int shown = Shown(entry, level);
                    if (shown > 1) starred[level] = shown;
                }
            }
            return starred;
        }

        /// <summary>The level whose cached tint the body wears (ours stands for the material it was copied from), else the creature's.</summary>
        private static int Shown(SwapEntry entry, LevelEffects level)
        {
            Material body = level.m_mainRender.sharedMaterials.FirstOrDefault();
            if (body && entry.Made.TryGetValue(body, out Material source)) body = source;
            string prefab = Utils.GetPrefabName(level.m_character.gameObject);
            foreach (KeyValuePair<string, Material> pair in LevelEffects.m_materials)
            {
                if (body && pair.Value == body && pair.Key.StartsWith(prefab, StringComparison.Ordinal) && int.TryParse(pair.Key.Substring(prefab.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int shown)) return shown;
            }
            return level.m_character.GetLevel();
        }

        /// <summary>After the revert: the star looks on again, at the size each copy has now (a mod may size starred creatures its own way).</summary>
        internal static void Put(Dictionary<LevelEffects, int> starred)
        {
            foreach (KeyValuePair<LevelEffects, int> pair in starred.Where(p => p.Key))
            {
                Vector3 scale = pair.Key.transform.localScale;
                pair.Key.SetupLevelVisualization(pair.Value);
                pair.Key.transform.localScale = scale;
            }
        }
    }
}
