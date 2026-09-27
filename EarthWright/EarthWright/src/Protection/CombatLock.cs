using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// "Combat Lock" (the spec's "blocks the hoe while you are fully seen"). Decision: fully seen means a hostile creature
    /// within the radius is alerted and hunting you. Sender only (the player's machine): the owner of the ground cannot
    /// know who a creature hunts. A creature's target is only known exactly on the machine that runs its AI; for
    /// creatures another machine runs, "alerted and has a target" stands in (the game replicates only that bit). The
    /// answer is cached for a quarter of a second, since the preview asks several times a second.
    /// </summary>
    public static class CombatLock
    {
        private const float CacheSeconds = 0.25f;

        private static float checkedAt = -10f;
        private static bool hunted;

        public static string Sender(GuardContext ctx) => Reason();

        /// <summary>The refusal while the local player is hunted, or null.</summary>
        public static string Reason()
        {
            if (!ProtectionSettings.CombatLock.Value)
                return null;
            Player player = Player.m_localPlayer;
            if (player == null)
                return null;
            float now = Time.time;
            if (now - checkedAt > CacheSeconds || now < checkedAt)
            {
                hunted = IsHunted(player);
                checkedAt = now;
            }
            return hunted ? ProtectionWords.Combat : null;
        }

        private static bool IsHunted(Player player)
        {
            float range = ProtectionSettings.CombatRadius.Value;
            Vector3 position = player.transform.position;
            foreach (BaseAI ai in BaseAI.BaseAIInstances)
            {
                if (ai == null || !ai.IsAlerted())
                    continue;
                if ((ai.transform.position - position).sqrMagnitude <= range * range && Hunts(ai, player))
                    return true;
            }
            return false;
        }

        private static bool Hunts(BaseAI ai, Player player)
        {
            Character creature = ai.m_character;
            if (creature == null || creature.IsDead() || !BaseAI.IsEnemy(creature, player))
                return false;
            if (ai.m_nview != null && ai.m_nview.IsValid() && ai.m_nview.IsOwner())
                return ai.GetTargetCreature() == player;
            return ai.HaveTarget();
        }
    }
}
