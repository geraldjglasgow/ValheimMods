using EarthWright.Core;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// The local player's own game terrain ops, which EarthWright's pipeline does not carry: digging with the pickaxe
    /// (any attack or area effect of the player that spawns a terrain op where it hits the ground) and hoe or cultivator
    /// entries placed while EarthWright is off for this player. They are refused where they start, before the terrain op
    /// exists, so nothing is spawned or charged. Every machine refuses only its own player's ops; another player's ops
    /// are refused on that player's machine.
    /// <para>Decision: these two spawn points replace a TerrainOp.Awake prefix. They know who caused the op and with
    /// which item, where Awake would have to guess from timing and distance.</para>
    /// </summary>
    public static class GameOpLock
    {
        private static float lastMessage = -10f;

        /// <summary>The reason a dig by this character with this weapon (null for an area effect) is refused, or null.</summary>
        public static string DigReason(Character character, GameObject prefab, ItemDrop.ItemData weapon)
        {
            if (character == null || character != Player.m_localPlayer || !IsTerrainPrefab(prefab))
                return null;
            string item = PrefabNames.OfItem(weapon);
            return TerrainLock.BlocksLocal(item) ? ProtectionWords.Locked : null;
        }

        /// <summary>The reason the local player's placement of a terrain piece the game handles itself is refused, or null.</summary>
        public static string PlaceReason(Player player, Piece piece)
        {
            if (player == null || player != Player.m_localPlayer || piece == null || EarthWrightHandles())
                return null;
            // Only the piece's own component, as the game's hoe entries have it: building pieces may carry a terrain
            // modifier somewhere below them to shape the ground they stand on, and they stay buildable.
            if (piece.GetComponent<TerrainOp>() == null && piece.GetComponent<TerrainModifier>() == null)
                return null;
            string reason = AccessGuards.ToolsReason(Side.LocalIsAdmin);
            if (reason == null && TerrainLock.BlocksLocal(LocalTool.RightItemName))
                reason = ProtectionWords.Locked;
            return reason ?? CombatLock.Reason();
        }

        /// <summary>At most one message a second, so a held attack does not flood the screen.</summary>
        public static void Show(string reason)
        {
            if (Time.time - lastMessage < 1f && Time.time >= lastMessage)
                return;
            lastMessage = Time.time;
            Messages.Center(reason);
        }

        /// <summary>EarthWright's placement hook takes this click and runs every sender guard itself.</summary>
        private static bool EarthWrightHandles() => GeneralSettings.Active && LocalTool.IsToolName(LocalTool.RightItemName);

        private static bool IsTerrainPrefab(GameObject prefab)
        {
            return prefab != null && (prefab.GetComponentInChildren<TerrainOp>() != null || prefab.GetComponentInChildren<TerrainModifier>() != null);
        }
    }

    /// <summary>Pickaxe digging and the player's other terrain-spawning hits (the attacker's machine runs this).</summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.SpawnOnHitTerrain))]
    public static class DigLockPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(GameObject prefab, Character character, ItemDrop.ItemData weapon, ref GameObject __result)
        {
            string reason = Safe.Call("EarthWright dig lock", () => GameOpLock.DigReason(character, prefab, weapon), null);
            if (reason == null)
                return true;
            GameOpLock.Show(reason);
            __result = null;
            return false;
        }
    }

    /// <summary>
    /// Terrain pieces the game places itself (EarthWright off for this player, or a tool EarthWright does not handle).
    /// Low priority: runs after EarthWright's placement hook, which already refused its own clicks through the guards.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class VanillaPlaceLockPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        public static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            string reason = Safe.Call("EarthWright placement lock", () => GameOpLock.PlaceReason(__instance, piece), null);
            if (reason == null)
                return true;
            Messages.Center(reason);
            __result = false;
            return false;
        }
    }
}
