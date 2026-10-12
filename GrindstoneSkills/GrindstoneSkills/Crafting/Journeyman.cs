using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Journeyman (Crafting 75): the workbench and the forge count one level higher for the local player. Only this
    /// client asks CraftingStation.GetLevel about its own player's crafting, so a postfix adds one there:
    /// Player.RequiredCraftingStation (which recipes and upgrades the current station allows, checked again by
    /// InventoryGui.DoCrafting through HaveRequirements), InventoryGui.UpdateRecipe (the level shown in the crafting panel
    /// and the required-level warning) and InventoryGui.CanRepair (which items the station repairs, the game's cap of 4
    /// applied after). The game's one other caller, Player.AddKnownStation, keeps the real level: it saves the station
    /// levels the character has seen into the character file and announces a new one, and a level no station has would
    /// stay saved after the perk is gone. So recipes still appear at the station levels the player has seen; Journeyman
    /// lets them be made, upgraded and repaired one level early. Untouched: the extensions, the build range (worked out
    /// from the extension count, not GetLevel), the station's hover text, and other players, whose clients add nothing for
    /// this player; a dedicated server has no local player and never adds anything. Stations are known by their prefab
    /// (piece_workbench, forge) from the ZDO, not by their name, which modded stations may copy. Hot (once per recipe on
    /// every crafting panel refresh): the local level is read once a frame and the prefab test kept for the last station.
    /// </summary>
    public static class Journeyman
    {
        /// <summary>The Crafting level that unlocks Journeyman.</summary>
        private const float UnlockLevel = 75f;

        /// <summary>How many levels higher the stations count.</summary>
        private const int ExtraLevels = 1;

        private static readonly int WorkbenchHash = "piece_workbench".GetStableHashCode();
        private static readonly int ForgeHash = "forge".GetStableHashCode();

        private static bool discovering;
        private static int checkedFrame = -1;
        private static bool active;
        private static CraftingStation lastStation;
        private static bool lastCounts;

        /// <summary>The Crafting level that unlocks Journeyman, for the skill page.</summary>
        public static float Level => UnlockLevel;

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetLevel))]
        private static class StationLevel
        {
            [HarmonyPostfix]
            private static void Postfix(CraftingStation __instance, ref int __result)
            {
                if (!discovering)
                    __result += HookGuard.Run("Journeyman", static station => Bonus(station), __instance, 0);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownStation))]
        private static class Discovery
        {
            [HarmonyPrefix]
            private static void Prefix() => discovering = true;

            [HarmonyFinalizer]
            private static void Finalizer() => discovering = false;
        }

        private static int Bonus(CraftingStation station) => Active() && Counts(station) ? ExtraLevels : 0;

        /// <summary>Whether the module is on and the local player has reached the level, worked out once a frame.</summary>
        private static bool Active()
        {
            int frame = Time.frameCount;
            if (frame != checkedFrame)
            {
                checkedFrame = frame;
                active = CraftingPerks.Enabled && LocalSkill.Reached(Skills.SkillType.Crafting, UnlockLevel);
            }
            return active;
        }

        /// <summary>Whether the station is a workbench or a forge, kept for the last station asked.</summary>
        private static bool Counts(CraftingStation station)
        {
            if (ReferenceEquals(station, lastStation))
                return lastCounts;
            if (station == null)
                return false;
            ZNetView nview = station.m_nview != null ? station.m_nview : station.GetComponentInParent<ZNetView>();
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            if (zdo == null)
                return false;
            int prefab = zdo.GetPrefab();
            lastStation = station;
            lastCounts = prefab == WorkbenchHash || prefab == ForgeHash;
            return lastCounts;
        }
    }
}
