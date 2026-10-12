using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Ride level of whoever rides a mount, as the mount's owner knows it, for the Riding perks (section 41). The
    /// rider's client sends its Ride skill factor (0..1) every physics frame in the saddle's "Controls" RPC
    /// (Sadle.ApplyControlls, from Player.SetControls); the RPC goes to the mount's ZDO owner, which keeps it in
    /// Sadle.m_rideSkill (Sadle.RPC_Controls) and clears it when the rider lets go. The game reads it there through
    /// BaseAI.GetRiderSkill for its own gallop speed and mount stamina. Taking the saddle makes the rider the owner
    /// (Sadle.RPC_RequestControl), but any owner works: the level arrives with the RPC, not from the owner's own skills.
    /// Character.HaveRider is true while the saddle's user is within its use range (Sadle.HaveValidUser), on every peer.
    /// </summary>
    public static class MountRider
    {
        /// <summary>The rider's Ride factor (0..1) on the mount's owner; 0 without a rider, or on another peer.</summary>
        public static float Factor(Character mount)
        {
            if (mount == null || !mount.HaveRider())
                return 0f;
            return Mathf.Clamp01(mount.m_baseAI.GetRiderSkill());
        }
    }
}
