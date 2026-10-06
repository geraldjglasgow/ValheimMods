using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Ship health from the builder's Sailing level.
    /// <list type="bullet">
    /// <item>Player.PlacePiece instantiates the ship on the builder's client, which owns it, and then calls
    /// Piece.SetCreator. A postfix there stores the builder's level in the ship's ZDO and raises the new ship's max
    /// health; its Awake ran before the level was stored.</item>
    /// <item>Every client that loads the ship later raises it in Ship.Awake (patched there rather than in every building
    /// piece's WearNTear.Awake), so every machine agrees on the max. When the ship's WearNTear woke first, it already
    /// added the world level bonus (a factor too, so the order does not matter) and computed the health share, which is
    /// then worked out again.</item>
    /// </list>
    /// The current health in the ZDO is an absolute number and stays untouched: a new ship has none stored and reads as
    /// full. Ships built without GrindstoneSkills have no stored level and keep the game's health. A changed setting
    /// applies to ships as they load.
    /// </summary>
    public static class ShipwrightHealth
    {
        private static readonly int LevelHash = Keys.ShipwrightLevel.GetStableHashCode();

        [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
        private static class Placed
        {
            [HarmonyPostfix]
            private static void Postfix(Piece __instance, long uid)
            {
                Player player = Player.m_localPlayer;
                ZNetView nview = __instance.m_nview;
                WearNTear wearNTear = __instance.GetComponent<WearNTear>();
                if (player == null || player.GetPlayerID() != uid || wearNTear == null || __instance.GetComponent<Ship>() == null)
                    return;
                if (nview == null || !nview.IsValid() || !nview.IsOwner() || nview.GetZDO().GetFloat(LevelHash, -1f) >= 0f)
                    return;
                float level = SailingSkill.Local();
                nview.GetZDO().Set(LevelHash, level);
                wearNTear.m_health *= Factor(level);
            }
        }

        [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
        private static class Loaded
        {
            [HarmonyPrefix]
            private static void Prefix(Ship __instance)
            {
                WearNTear wearNTear = __instance.GetComponent<WearNTear>();
                ZNetView nview = __instance.GetComponent<ZNetView>();
                ZDO zdo = nview != null ? nview.GetZDO() : null;
                if (wearNTear == null || zdo == null)
                    return;
                float factor = Factor(zdo.GetFloat(LevelHash));
                if (Mathf.Approximately(factor, 1f))
                    return;
                wearNTear.m_health *= factor;
                // m_nview is set in WearNTear.Awake: when it already ran, its health share used the old max.
                if (wearNTear.m_nview != null)
                    wearNTear.m_healthPercentage = Mathf.Clamp01(zdo.GetFloat(ZDOVars.s_health, wearNTear.m_health) / wearNTear.m_health);
            }
        }

        /// <summary>The max health multiplier of a ship built at this Sailing level; 1 while Sailing is off.</summary>
        public static float Factor(float level) =>
            SailingSkill.Active ? 1f + SailingSkill.Share(SailingSettings.ShipHealth.Value, level) : 1f;
    }
}
