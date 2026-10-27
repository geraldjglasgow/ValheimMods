using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A shot that leaves partway into its motion (<see cref="KwArmDef.FireDelay"/>: a cannon fires halfway through its
    /// lanyard's pull), on every peer that draws the cannon. Each shot the ZDO counts (<see cref="WeaponState.Shots"/>, the
    /// gunner's own the moment he fires, everyone else's as the ZDO arrives) is seen once; when its age
    /// (<see cref="WeaponState.ShotAge"/>) passes the delay its muzzle effects play (<see cref="KwArmLook.Blast"/>), and on
    /// the gunner's game the projectile he fired leaves the muzzle (<see cref="KwShooter"/>). A peer that first draws the
    /// cannon after a shot, or hears of it seconds late, plays nothing of it. Meanwhile the barrel stays laid on what the
    /// gunner's crosshair was on as he pulled (the library's <see cref="WeaponAim.Mark"/>), and the ball leaves onto it
    /// (<see cref="KwShooter"/>).
    /// </summary>
    public sealed class KwArmLaunch
    {
        /// <summary>A shot heard of this long after the delay is not played.</summary>
        private const float Late = 2f;

        private int seen = -1;
        private float heldAt;
        private KwShot? flying, shown;
        private Player? gunner;
        private ItemDrop.ItemData? ammo;

        /// <summary>The gunner's shot, launched from the muzzle when the delay is up.</summary>
        public void Hold(KwShot shot, Player by, ItemDrop.ItemData? item) => (flying, gunner, ammo, heldAt) = (shot, by, item, Time.time);

        /// <summary>Each drawn frame: a new shot noted, and what is due launched and shown.</summary>
        public void Tick(KwArm arm, in WeaponState s)
        {
            Watch(arm, s);
            float delay = arm.Def.FireDelay;
            if (flying != null && Time.time >= heldAt + delay)
            {
                if (gunner != null)
                    KwShooter.Fire(flying, arm, gunner, ammo);
                (flying, gunner, ammo) = (null, null, null);
            }
            if (shown != null && s.ShotAge >= delay)
            {
                KwArmLook.Blast(arm, shown);
                shown = null;
            }
        }

        // A shot the ZDO has counted since this peer last looked, for the trigger that fired it.
        private void Watch(KwArm arm, in WeaponState s)
        {
            if (seen < 0 || s.Shots < seen)
                seen = s.Shots;
            if (s.Shots <= seen)
                return;
            seen = s.Shots;
            if (s.ShotAge < arm.Def.FireDelay + Late)
                shown = KwArmTrigger.Of(arm.Def, arm.Link.Zdo.GetInt(arm.Link.Kind));
        }
    }
}
