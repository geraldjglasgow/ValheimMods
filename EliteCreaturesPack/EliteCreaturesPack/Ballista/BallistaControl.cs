using MannedWeapons;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The Bone Ballista's component, on every peer: a manned weapon (the MannedWeapons library holds the hold and its
    /// hand-over to the holder's machine, the aim within the ballista's turn, the holder's walk round it and his hands, the
    /// reload's steps and the trigger; its numbers are <see cref="BallistaDesign"/>). The ballista's own part: its model's
    /// parts (<see cref="BallistaParts"/>), the holder's spot stepping in for the pull and the load
    /// (<see cref="BallistaTimeline.Close"/>), the nose's kick at a shot, its look (<see cref="BallistaLook"/>), his hands on
    /// the string and with the missile (<see cref="BallistaHands"/>) and the shot (<see cref="BallistaShot"/>), whose flight
    /// from the groove (<see cref="Flight"/>) the library lays on the crosshair.
    /// </summary>
    public sealed class BallistaControl : MannedWeapon
    {
        private const float KickDegrees = 3f, KickTime = 0.08f;

        private readonly BallistaLook look = new BallistaLook();

        public BallistaParts? Parts { get; private set; }

        public override WeaponDesign Design => BallistaDesign.Design;

        protected override WeaponFrame? FindFrame()
        {
            Parts = BallistaParts.Of(transform);
            return Parts != null ? new WeaponFrame(Parts.Yaw, Parts.Pitch, Parts.StandAim, Parts.GripLeft, Parts.GripRight) : null;
        }

        /// <summary>Behind the ballista to aim, stepped in toward `stand_load` for the pull and the load.</summary>
        public override Vector3 StandPoint(in WeaponState s) =>
            Vector3.Lerp(Parts!.StandAim.position, Parts.StandLoad.position, BallistaTimeline.Close(s));

        /// <summary>The bolt's flight, for laying the ballista so it lands on the crosshair: from its nose in the groove, straight, at the missile's speed.</summary>
        public override ShotFlight Flight => ShotFlight.At(Frame!, BallistaShot.Nose(Parts!), BallistaMissile.Speed);

        protected override bool Shoot(Player shooter) => Parts != null && BallistaShot.Fire(this, Parts, shooter);

        /// <summary>A shot kicks the nose up and it settles back.</summary>
        protected override float Kick(in WeaponState s) => s.ShotAge < 1f ? KickDegrees * Mathf.Exp(-s.ShotAge / KickTime) : 0f;

        protected override void Draw(in WeaponState s) => look.Show(Parts!, s);

        protected override void DrawAtRest() => look.Rest(Parts!);

        protected override OperatorPose Pose(in WeaponState s, OperatorRig rig, Transform body) => BallistaHands.Pose(Parts!, s, look.Centre, rig, body);

        protected override void Gripped(in WeaponState s, OperatorRig rig, Transform body) => BallistaHands.Missile(Parts!, s, rig, body);

        protected override void Released(in WeaponState s) => Parts?.Held?.SetActive(false);
    }
}
