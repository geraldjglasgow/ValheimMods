using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// Health application. The prefab holds the base max health; a loaded ship holds it with the world level bonus
    /// that WearNTear.Awake adds. The current health lives in the ship's ZDO as an absolute number and is never
    /// touched here: only the max changes, then the cached percentage and the damage visuals are refreshed.
    /// </summary>
    public static class ShipHealthUpdater
    {
        /// <summary>1 plus the bonus WearNTear.Awake adds to every piece's max health at world level above 0.</summary>
        public static float WorldLevelFactor()
        {
            if (Game.m_worldLevel <= 0 || Game.instance == null)
                return 1f;
            return 1f + Game.m_worldLevel * Game.instance.m_worldLevelPieceHPMultiplier;
        }

        /// <summary>The prefab gets the base value; Awake adds the world level bonus when an instance is created.</summary>
        public static void ApplyToPrefab(WearNTear wearNTear, float health)
        {
            wearNTear.m_health = health;
        }

        /// <summary>
        /// Changes the max health of a loaded ship, leaves the stored current health alone, and refreshes the
        /// cached percentage and the new/worn/broken visuals from it, the way RPC_HealthChanged does.
        /// </summary>
        public static void ApplyToInstance(WearNTear wearNTear, float health)
        {
            wearNTear.m_health = health * WorldLevelFactor();
            ZNetView view = wearNTear.m_nview;
            if (view == null || !view.IsValid())
                return;

            float current = view.GetZDO().GetFloat(ZDOVars.s_health, wearNTear.m_health);
            wearNTear.m_healthPercentage = wearNTear.m_health > 0f ? Mathf.Clamp01(current / wearNTear.m_health) : 0f;
            wearNTear.UpdateVisual(triggerEffects: false);
        }
    }
}
