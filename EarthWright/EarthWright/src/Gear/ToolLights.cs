using System.Collections.Generic;
using EarthWright.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace EarthWright.Gear
{
    /// <summary>
    /// A light on the terrain tool in every player's hand, on every client. Each machine builds a player's hand item
    /// itself from the player's ZDO (<c>VisEquipment.SetRightHandEquipped</c>); a point light is added to that item when
    /// it is a terrain tool, so every player sees every other player's tool glow, and it goes away with the item. Range,
    /// brightness and on/off are the server's settings; the colour is each viewer's own. Lights follow setting changes
    /// twice a second. Nothing is added on a dedicated server, which draws nothing.
    /// </summary>
    public static class ToolLights
    {
        private static readonly List<Light> lights = new List<Light>();
        private static float nextRefresh;

        internal static void OnRightHandEquipped(VisEquipment vis, int itemHash)
        {
            GameObject instance = vis.m_rightItemInstance;
            if (instance == null || !vis.m_isPlayer || vis.m_isArmorStand || !IsTerrainTool(itemHash))
                return;
            // No light on a machine that draws nothing, nor on the main menu's character preview.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || FejdStartup.instance != null)
                return;
            GameObject holder = new GameObject("EW_ToolLight");
            holder.transform.SetParent(instance.transform, false);
            holder.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            Light light = holder.AddComponent<Light>();
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            Configure(light);
            lights.Add(light);
        }

        internal static void Update()
        {
            if (lights.Count == 0 || Time.time < nextRefresh)
                return;
            nextRefresh = Time.time + 0.5f;
            lights.RemoveAll(light => light == null);
            foreach (Light light in lights)
                Configure(light);
        }

        private static void Configure(Light light)
        {
            light.enabled = GearSettings.ToolLight.Value && GeneralSettings.Enabled.Value;
            light.range = GearSettings.LightRange.Value;
            light.intensity = GearSettings.LightIntensity.Value;
            light.color = GearSettings.LightColour.Value;
        }

        private static bool IsTerrainTool(int itemHash)
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(itemHash) : null;
            return prefab != null && LocalTool.IsToolName(prefab.name);
        }
    }

    /// <summary>Adds the light when a player's right-hand item is (re)built on this machine.</summary>
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightHandEquipped))]
    public static class ToolLightEquipPatch
    {
        [HarmonyPostfix]
        public static void Postfix(VisEquipment __instance, bool __result, int hash)
        {
            if (__result)
                Safe.Run("EarthWright tool light", () => ToolLights.OnRightHandEquipped(__instance, hash));
        }
    }
}
