using System;
using System.Collections.Generic;
using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Reach
{
    /// <summary>
    /// The "From storage: n" line of station hover texts. Smelters and ovens hover through their switches
    /// (<c>Switch.GetHoverText</c>, which calls the station's own hover callback or a fixed text); fires,
    /// fermenters and plain cooking stations through their own <c>GetHoverText</c>. Hover texts run every frame, so
    /// the line of the hovered station is worked out once and kept until something it counts could have changed.
    /// </summary>
    public static class StationHover
    {
        /// <summary>A hover text runs every frame; its line is kept this long at most, and dropped sooner on any change.</summary>
        private const float KeepSeconds = 1f;

        private static UnityEngine.Object memoFor;
        private static List<Container> memoList;
        private static int memoChange;
        private static int memoWorldLevel;
        private static float memoAt;
        private static string memoLine;

        public static bool Active => ReachRules.Active(ReachMode.Stations);

        /// <summary>
        /// The line last worked out for this station, while the reach list, every inventory and the world level are
        /// as they were and it is under a second old. "" means the station shows no line.
        /// </summary>
        public static bool Remembered(UnityEngine.Object station, out string line)
        {
            line = memoLine;
            if (!ReferenceEquals(station, memoFor) || Time.unscaledTime - memoAt > KeepSeconds)
                return false;
            return ReferenceEquals(ReachChests.List(), memoList) && StorageIndex.Change == memoChange
                && Game.m_worldLevel == memoWorldLevel;
        }

        /// <summary>Keeps the line just worked out for this station ("" for none); see <see cref="Remembered"/>.</summary>
        public static string Remember(UnityEngine.Object station, string line)
        {
            memoFor = station;
            memoList = ReachChests.List();
            memoChange = StorageIndex.Change;
            memoWorldLevel = Game.m_worldLevel;
            memoAt = Time.unscaledTime;
            memoLine = line;
            return line;
        }

        /// <summary>Feeding is on and the station's prefab is not disabled in the stations: map.</summary>
        public static bool Shows(UnityEngine.Component station) => Active && ReachRules.StationRuleFor(station).Enabled;

        public static string Line(Func<ItemDrop.ItemData, bool> accepts)
        {
            return "\n" + Language.Localize("$ok_fromstorage") + " " + ReachCount.CountMatching(accepts);
        }

        /// <summary>What the switch's station accepts through that switch, or null for a switch that adds nothing
        /// or a station disabled in the stations: map.</summary>
        public static Func<ItemDrop.ItemData, bool> SwitchAccepts(Switch sw)
        {
            Smelter smelter = sw.GetComponentInParent<Smelter>();
            if (smelter != null)
            {
                if (!ReachRules.StationRuleFor(smelter).Enabled)
                    return null;
                if (smelter.m_addOreSwitch == sw)
                    return StationAccepts.SmelterOre(smelter);
                return smelter.m_addWoodSwitch == sw && smelter.m_fuelItem != null ? StationAccepts.Fuel(smelter, smelter.m_fuelItem) : null;
            }
            CookingStation station = sw.GetComponentInParent<CookingStation>();
            if (station == null || !ReachRules.StationRuleFor(station).Enabled)
                return null;
            if (station.m_addFoodSwitch == sw)
                return StationAccepts.CookingFood(station);
            return station.m_addFuelSwitch == sw && station.m_fuelItem != null ? StationAccepts.Fuel(station, station.m_fuelItem) : null;
        }
    }

    [HarmonyPatch(typeof(Switch), nameof(Switch.GetHoverText))]
    public static class SwitchHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Switch __instance, ref string __result)
        {
            if (!StationHover.Active || string.IsNullOrEmpty(__result))
                return;
            if (!StationHover.Remembered(__instance, out string line))
                line = StationHover.Remember(__instance, Work(__instance));
            __result += line;
        }

        private static string Work(Switch sw)
        {
            Func<ItemDrop.ItemData, bool> accepts = StationHover.SwitchAccepts(sw);
            return accepts != null ? StationHover.Line(accepts) : "";
        }
    }

    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
    public static class FireplaceHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Fireplace __instance, ref string __result)
        {
            if (!StationHover.Active || string.IsNullOrEmpty(__result) || !__instance.m_canRefill || __instance.m_infiniteFuel || __instance.m_fuelItem == null)
                return;
            if (!StationHover.Remembered(__instance, out string line))
                line = StationHover.Remember(__instance, Work(__instance));
            __result += line;
        }

        private static string Work(Fireplace fire) =>
            StationHover.Shows(fire) ? StationHover.Line(StationAccepts.Fuel(fire, fire.m_fuelItem)) : "";
    }

    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetHoverText))]
    public static class FermenterHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Fermenter __instance, ref string __result)
        {
            if (!StationHover.Active || string.IsNullOrEmpty(__result) || __instance.GetContent() != 0)
                return;
            if (!StationHover.Remembered(__instance, out string line))
                line = StationHover.Remember(__instance, Work(__instance));
            __result += line;
        }

        private static string Work(Fermenter fermenter)
        {
            if (!StationHover.Shows(fermenter) || !PrivateArea.CheckAccess(fermenter.transform.position, 0f, false))
                return "";
            return StationHover.Line(StationAccepts.FermenterBase(fermenter));
        }
    }

    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetHoverText))]
    public static class CookingStationHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CookingStation __instance, ref string __result)
        {
            if (!StationHover.Active || string.IsNullOrEmpty(__result))
                return;
            if (!StationHover.Remembered(__instance, out string line))
                line = StationHover.Remember(__instance, Work(__instance));
            __result += line;
        }

        private static string Work(CookingStation station) =>
            StationHover.Shows(station) ? StationHover.Line(StationAccepts.CookingFood(station)) : "";
    }
}
