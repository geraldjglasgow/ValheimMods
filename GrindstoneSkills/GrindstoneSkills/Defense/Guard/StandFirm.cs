using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Less knockback from blocked hits. Character.RPC_Damage pushes the player back with the hit's m_pushForce right
    /// after Humanoid.BlockAttack, which already shrinks it for a held block; the block hook shrinks it further by
    /// Knockback Reduction, on the blocker's own client.
    /// </summary>
    public static class StandFirm
    {
        public static void OnBlock(HitData hit) =>
            hit.m_pushForce *= 1f - Mathf.Clamp01(DefenseSkill.LocalShare(DefenseGuardSettings.Knockback.Value));
    }
}
