using System;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The game's own terrain operations (pickaxe digging, and any terrain piece that is not an EarthWright click) run on
    /// the compiler's owner through <c>RPC_ApplyOperation</c>. These are the game's <c>LevelTerrain</c> and
    /// <c>RaiseTerrain</c>, reproduced line for line, with the hidden ±8 m clamp of the level delta replaced by the
    /// configured raise and dig limits at the operation's centre (dig exceptions included). As everywhere, a point already
    /// past a limit is not pushed back. <c>SmoothTerrain</c> keeps the game's ±1 m smooth clamp and is left alone.
    /// </summary>
    public static class LimitGameOps
    {
        /// <summary>The game's LevelTerrain with the configured limits.</summary>
        internal static void Level(TerrainComp comp, Vector3 worldPos, float radius, bool square)
        {
            Heightmap map = comp.m_hmap;
            map.WorldToVertex(worldPos, out int x, out int y);
            float target = worldPos.y - comp.transform.position.y;
            float reach = radius / map.m_scale;
            int span = Mathf.CeilToInt(reach);
            int pitch = comp.m_width + 1;
            float up = HeightLimits.Raise(worldPos);
            float down = HeightLimits.Dig(worldPos);
            for (int i = y - span; i <= y + span; i++)
            {
                for (int j = x - span; j <= x + span; j++)
                {
                    bool inside = square || !(Distance(x, y, j, i) > reach);
                    if (inside && j >= 0 && i >= 0 && j < pitch && i < pitch)
                        LevelPoint(comp, j, i, pitch, target, up, down);
                }
            }
        }

        private static void LevelPoint(TerrainComp comp, int j, int i, int pitch, float target, float up, float down)
        {
            float height = comp.m_hmap.GetHeight(j, i);
            int index = i * pitch + j;
            float change = target - height + comp.m_smoothDelta[index];
            comp.m_smoothDelta[index] = 0f;
            SetLevel(comp, index, change, up, down);
        }

        /// <summary>The game's RaiseTerrain with the configured limits.</summary>
        internal static void Raise(TerrainComp comp, Vector3 worldPos, float radius, float delta, bool square, float power)
        {
            Heightmap map = comp.m_hmap;
            map.WorldToVertex(worldPos, out int x, out int y);
            float centreHeight = worldPos.y - comp.transform.position.y;
            float reach = radius / map.m_scale;
            int span = Mathf.CeilToInt(reach);
            int pitch = comp.m_width + 1;
            float up = HeightLimits.Raise(worldPos);
            float down = HeightLimits.Dig(worldPos);
            for (int i = y - span; i <= y + span; i++)
            {
                for (int j = x - span; j <= x + span; j++)
                {
                    if (j < 0 || i < 0 || j >= pitch || i >= pitch)
                        continue;
                    float weight = RaiseWeight(square, Distance(x, y, j, i), reach, power);
                    if (weight >= 0f)
                        RaisePoint(comp, j, i, pitch, centreHeight, delta, delta * weight, up, down);
                }
            }
        }

        /// <summary>The game's falloff for raising: -1 outside the circle, 1 for squares and power 0.</summary>
        private static float RaiseWeight(bool square, float distance, float reach, float power)
        {
            if (square)
                return 1f;
            if (distance > reach)
                return -1f;
            if (power <= 0f)
                return 1f;
            float weight = 1f - distance / reach;
            return power != 1f ? Mathf.Pow(weight, power) : weight;
        }

        private static void RaisePoint(TerrainComp comp, int j, int i, int pitch, float centreHeight, float delta, float add, float up, float down)
        {
            float height = comp.m_hmap.GetHeight(j, i);
            float target = centreHeight + add;
            if (delta < 0f && target > height)
                return;
            if (delta > 0f)
            {
                if (target < height)
                    return;
                if (target > height + add)
                    target = height + add;
            }
            int index = i * pitch + j;
            float change = target - height + comp.m_smoothDelta[index];
            comp.m_smoothDelta[index] = 0f;
            SetLevel(comp, index, change, up, down);
        }

        /// <summary>Adds the change to the level delta, held within the limits (widened to include where the point was).</summary>
        private static void SetLevel(TerrainComp comp, int index, float change, float up, float down)
        {
            float before = comp.m_levelDelta[index];
            float level = before + change;
            comp.m_levelDelta[index] = Mathf.Clamp(level, Mathf.Min(-down, before), Mathf.Max(up, before));
            comp.m_modifiedHeight[index] = true;
        }

        private static float Distance(int x, int y, int j, int i) => Vector2.Distance(new Vector2(x, y), new Vector2(j, i));
    }

    /// <summary>The game's LevelTerrain with EarthWright's limits (see <see cref="LimitGameOps"/>).</summary>
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.LevelTerrain))]
    public static class LevelTerrainPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TerrainComp __instance, Vector3 worldPos, float radius, bool square)
        {
            if (__instance.m_hmap == null)
                return true;
            try
            {
                LimitGameOps.Level(__instance, worldPos, radius, square);
                return false;
            }
            catch (Exception e)
            {
                LimitDisplay.WarnOnce("level limits", e);
                return false;
            }
        }
    }

    /// <summary>The game's RaiseTerrain with EarthWright's limits (see <see cref="LimitGameOps"/>).</summary>
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.RaiseTerrain))]
    public static class RaiseTerrainPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TerrainComp __instance, Vector3 worldPos, float radius, float delta, bool square, float power)
        {
            if (__instance.m_hmap == null)
                return true;
            try
            {
                LimitGameOps.Raise(__instance, worldPos, radius, delta, square, power);
                return false;
            }
            catch (Exception e)
            {
                LimitDisplay.WarnOnce("raise limits", e);
                return false;
            }
        }
    }
}
