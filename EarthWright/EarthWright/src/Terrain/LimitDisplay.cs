using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Every client, whenever a heightmap with a compiler rebuilds: the game's <c>TerrainComp.ApplyToHeightmap</c>,
    /// reproduced with its ±8 m display clamp widened to ±<see cref="HeightLimits.Absolute"/>, so the display never cuts
    /// an edit the owner allowed (the real limits are enforced when the owner writes). It also captures the heights
    /// before the compiler's edits for <see cref="EngineBaseHeights"/>. Everything else is exactly the game's code.
    /// </summary>
    public static class LimitDisplay
    {
        private static bool warned;

        /// <summary>True when the heightmap was built here; false lets the game's own method run (unexpected sizes).</summary>
        internal static bool TryApply(TerrainComp comp, Texture2D clearedMask, List<float> heights, Heightmap map)
        {
            if (!comp.m_initialized)
                return true;
            int pitch = comp.m_width + 1;
            int count = pitch * pitch;
            if (heights == null || heights.Count != count || comp.m_levelDelta.Length != count || comp.m_smoothDelta.Length != count)
                return false;
            if (map != null)
                EngineBaseHeights.Capture(map, heights);
            ApplyHeights(comp, heights, HeightLimits.Absolute);
            // The paint texture exists wherever the heightmap was generated (a dedicated server too); if it is ever
            // missing the heights are still drawn with EarthWright's clamp rather than the game's ±8 m.
            if (clearedMask != null && comp.m_modifiedPaint.Length == count && comp.m_paintMask.Length == count)
                ApplyPaint(comp, clearedMask, pitch);
            return true;
        }

        private static void ApplyHeights(TerrainComp comp, List<float> heights, float absolute)
        {
            int count = heights.Count;
            for (int i = 0; i < count; i++)
            {
                float level = comp.m_levelDelta[i];
                float smooth = comp.m_smoothDelta[i];
                if (level == 0f && smooth == 0f)
                    continue;
                float ground = heights[i];
                heights[i] = Mathf.Clamp(ground + level + smooth, ground - absolute, ground + absolute);
            }
        }

        private static void ApplyPaint(TerrainComp comp, Texture2D clearedMask, int pitch)
        {
            for (int y = 0; y < pitch; y++)
            {
                for (int x = 0; x < pitch; x++)
                {
                    int i = y * pitch + x;
                    if (comp.m_modifiedPaint[i])
                        clearedMask.SetPixel(x, y, comp.m_paintMask[i]);
                }
            }
        }

        /// <summary>
        /// The game's "the pickaxe has dug as deep as it may" check (no more stone drops), following the dig limit where
        /// the pickaxe hits instead of the fixed 7.95 m (the limit less 5 cm, as the game does).
        /// </summary>
        internal static bool AtMaxDepth(Heightmap map, Vector3 worldPos)
        {
            if (!map.GetWorldHeight(worldPos, out float height))
                return false;
            float ground = EngineBaseHeights.At(worldPos, float.NaN);
            if (float.IsNaN(ground) && !map.GetWorldBaseHeight(worldPos, out ground))
                return false;
            return Mathf.Max(ground - height, 0f) >= HeightLimits.Dig(worldPos) - 0.05f;
        }

        internal static void WarnOnce(string what, Exception e)
        {
            if (warned)
                return;
            warned = true;
            Plugin.Log?.LogError($"EarthWright {what} failed, the game's own code runs instead: {e}");
        }
    }

    /// <summary>The display clamp, replaced (see <see cref="LimitDisplay"/>).</summary>
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.ApplyToHeightmap))]
    public static class ApplyToHeightmapPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TerrainComp __instance, Texture2D clearedMask, List<float> heights, Heightmap hm)
        {
            try
            {
                return !LimitDisplay.TryApply(__instance, clearedMask, heights, hm);
            }
            catch (Exception e)
            {
                LimitDisplay.WarnOnce("height display", e);
                return true;
            }
        }
    }

    /// <summary>The pickaxe's maximum depth follows the dig limit (see <see cref="LimitDisplay.AtMaxDepth"/>).</summary>
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.AtMaxWorldLevelDepth))]
    public static class AtMaxWorldLevelDepthPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Heightmap __instance, Vector3 worldPos, ref bool __result)
        {
            try
            {
                __result = LimitDisplay.AtMaxDepth(__instance, worldPos);
                return false;
            }
            catch (Exception e)
            {
                LimitDisplay.WarnOnce("dig depth check", e);
                return true;
            }
        }
    }
}
