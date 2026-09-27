using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Reflex, the "block chance", on the local player's own client. Character.RPC_Damage blocks a blockable hit when
    /// IsBlocking() says the player is blocking, which for the owner reads the block input (m_blocking) and requires the
    /// player not to be attacking, dodging, staggered and so on. When a blockable hit from a character arrives while the
    /// player is not blocking, holds a shield and faces the hit, Reflex rolls its chance; on success m_blocking is set
    /// for this one hit (<see cref="IncomingHit"/> puts it back at the end), so the game blocks it with every rule of a
    /// normal block. The block timer is not running, so a reflex block never parries. "Reflex!" shows when the block
    /// held (<see cref="BlockHooks"/>).
    /// </summary>
    public static class Reflex
    {
        private static bool raised;

        public static void Before(Player player, HitData hit, Character attacker)
        {
            raised = false;
            if (!hit.m_blockable || attacker == null || player.m_blocking || !HoldsShield(player))
                return;
            if (Vector3.Dot(hit.m_dir, player.transform.forward) > 0f)
                return;
            if (Random.value >= DefenseSkill.LocalShare(DefenseGuardSettings.ReflexChance.Value))
                return;
            player.m_blocking = true;
            raised = true;
        }

        public static void OnBlocked(Player player)
        {
            if (raised)
                DefenseCallout.OverPlayer("Reflex!");
        }

        public static void After(Player player)
        {
            if (raised)
                player.m_blocking = false;
            raised = false;
        }

        private static bool HoldsShield(Player player)
        {
            ItemDrop.ItemData left = player.m_leftItem;
            return left != null && left.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;
        }
    }
}
