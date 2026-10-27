using MannedWeapons;
using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// A ship cannon in the world, on every peer: a manned weapon of the MannedWeapons library, the Bone Ballista's way.
    /// The library holds the hold and its hand-over (E asks the cannon's owner, who hands the gunner its ZDO, so his own
    /// game aims, reloads and fires from then on), the aim within the cannon's arc with his view held to it, his walk round
    /// it and his hands on its grips, the reload and the trigger (its numbers are <see cref="KwArmDesigns"/>). The cannon's
    /// own part: its parts, children found by name: "yaw" (the carriage, levered round on its trucks), under it "pitch"
    /// (the gun, tilting) with the "muzzle" at its end, and the gunner's stand point "attach" on the yaw part, so he walks
    /// round with it, stepping aside from its recoil (<see cref="KwShipRecoil"/>); the ball's flight from the muzzle the
    /// barrel is laid by (<see cref="Flight"/>); his hands and lean (<see cref="KwArmHands"/>); its loads, triggers and
    /// lanyard (<see cref="KwArmReload"/>, <see cref="KwArmTrigger"/>, <see cref="KwArmLaunch"/>); its look
    /// (<see cref="KwArmLook"/>); its words (<see cref="KwArmWords"/>). It is hauled about its ship's deck with the
    /// alternate use, Shift+E (<see cref="AltUse"/>; <see cref="KwArmTow"/>): nobody mans one being hauled
    /// (<see cref="Free"/>), nor hauls one being manned. A captain at a ship's helm fires the loaded ones aboard
    /// (<see cref="KwBroadside"/>, the library's ordered shot: whatever the load is for), an unmanned one on his game
    /// (<see cref="ShootCalled"/>), reloaded from his pack (<see cref="KwBroadsideReload"/>), a manned one on its gunner's
    /// where he has it laid (<see cref="ShootOrdered"/>), reloaded by him. Unmanned it goes back to its rest lay (the
    /// library's <see cref="WeaponDesign.Rest"/>, <see cref="KwArmDesigns.RestPitch"/>).
    /// </summary>
    public sealed class KwArm : MannedWeapon
    {
        public const string YawName = "yaw", PitchName = "pitch", MuzzleName = "muzzle", AttachName = "attach";
        private static readonly bool Headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        /// <summary>The cannon's id (its <see cref="KwArmDef"/>), set on the prefab.</summary>
        public string Id = "";

        private WeaponDesign? design;
        private KwArmTow? tow;
        private KwShipRider? rider;
        private Transform? riderOn;
        private bool broken, greeted;

        public KwArmDef Def { get; private set; } = null!;
        public KwArmLink Link { get; private set; } = null!;
        public Transform Muzzle { get; private set; } = null!;

        /// <summary>Its moving parts and grips, on a peer that draws (null on a dedicated server).</summary>
        public KwArmMotion? Motion { get; private set; }

        /// <summary>Its carriage rolling back at a shot and its gunner's spot stepping aside.</summary>
        public KwShipRecoil? Recoil { get; private set; }

        /// <summary>A shot leaving partway into the lanyard's pull.</summary>
        public KwArmLaunch Launch { get; } = new KwArmLaunch();

        /// <summary>On the gunner's game: the trigger he last pulled (what the next load is for) and the one he pulls now.</summary>
        public int Want { get; set; }
        public int Pulled { get; set; }

        public override WeaponDesign Design => design ??= KwArmDesigns.Of(KwArmory.Arm(Id));

        /// <summary>Who hauls it by its rear (none when nobody does).</summary>
        public ZDOID Tower => Link != null && Link.Valid ? Link.Zdo.GetZDOID(Link.Tower) : ZDOID.None;

        /// <summary>What it is hauled by (its rear).</summary>
        public KwArmTow? Tow => tow != null ? tow : tow = GetComponentInChildren<KwArmTow>();

        /// <summary>
        /// Whether it rides a launched Broadside ship now: built on it, it is the ship's child (<see cref="KwShipRider.On"/>;
        /// its rider looked up again only when what carries it changes).
        /// </summary>
        public bool Aboard
        {
            get
            {
                Transform? parent = transform.parent;
                if (parent != riderOn)
                    (riderOn, rider) = (parent, GetComponent<KwShipRider>());
                return rider != null && rider.On != null;
            }
        }

        protected override WeaponFrame? FindFrame()
        {
            KwArmDef? def = KwArmory.Arm(Id);
            Transform? yaw = transform.Find(YawName), pitch = yaw != null ? yaw.Find(PitchName) : null;
            Transform? muzzle = pitch != null ? pitch.Find(MuzzleName) : null, stand = yaw != null ? yaw.Find(AttachName) : null;
            if (def == null || yaw == null || pitch == null || muzzle == null || stand == null)
                return null;
            (Def, Link, Muzzle) = (def, new KwArmLink(Net), muzzle);
            Motion = Headless ? null : KwArmMotion.Of(transform, def);
            Recoil = KwShipRecoil.Of(transform, def, yaw, pitch, stand);
            return new WeaponFrame(yaw, pitch, stand, Motion?.GripLeft, Motion?.GripRight);
        }

        /// <summary>The stand point, turned with its carriage but not rolled back with it, and stepped aside at a shot.</summary>
        public override Vector3 StandPoint(in WeaponState s) => Recoil != null ? Recoil.Stand(s) : Frame!.Stand.position;

        /// <summary>The crosshair's target is looked for past the trunnions.</summary>
        public override Vector3 AimPivot => Frame!.Pitch.position;

        /// <summary>
        /// The ball's flight, for laying the barrel so it lands on the crosshair: from the muzzle, at the speed and drop of
        /// the shot loaded (the attack's when the cannon has no other), carried on by the ship under it.
        /// </summary>
        public override ShotFlight Flight
        {
            get
            {
                KwShot shot = Def.Alternate == null || !Link.Valid ? Def.Primary : KwArmTrigger.Of(Def, Link.Zdo.GetInt(Link.LoadKind));
                return ShotFlight.At(Frame!, Muzzle.position, shot.Speed, shot.Gravity, carried: true);
            }
        }

        protected override bool Shoot(Player shooter) => KwArmTrigger.Shoot(this, shooter);

        /// <summary>
        /// From the lanyard's pull until the ball has left the barrel stays laid on what the gunner's crosshair was on as he
        /// pulled (the library's <see cref="WeaponAim.Mark"/>: his step aside never moves it, a deck rolling or turning
        /// meanwhile is followed), and the ball leaves onto it (<see cref="KwShooter"/>).
        /// </summary>
        protected override bool Steady(in WeaponState s) => s.ShotAge < Def.FireDelay + 0.05f;

        /// <summary>A shot the captain called: the shot the load is for, its reload then queued on his game (<see cref="KwBroadsideReload"/>).</summary>
        protected override bool ShootCalled(Player caller)
        {
            Pulled = Link.Zdo.GetInt(Link.LoadKind);
            if (!Shoot(caller))
                return false;
            KwBroadsideReload.Fired(this, caller, Pulled);
            return true;
        }

        /// <summary>A shot the captain ordered of a cannon its gunner mans, on the gunner's game: the shot the load is for, his own reload following.</summary>
        protected override bool ShootOrdered(Player gunner)
        {
            Pulled = Link.Zdo.GetInt(Link.LoadKind);
            return Shoot(gunner);
        }

        protected override bool Free => Tower.IsNone();

        /// <summary>The library's calls, and on a cannon with a ZDO the captain's ask for it to load it (<see cref="KwBroadsideLoad"/>).</summary>
        protected override void Awake()
        {
            base.Awake();
            if (Net.GetZDO() != null)
                Net.Register<long>(KwArmLink.Load, (sender, captain) => Guard.Run("Broadside cannon load", () => KwBroadsideLoad.Asked(this, sender)));
        }

        protected override string? Refusal(Player player) => Tower.IsNone() ? null : KwArmWords.Hauled(this, player);

        /// <summary>The alternate use (Shift+E) anywhere on it hauls it about its ship's deck, or lets go (<see cref="KwArmTow.Take"/>); its gunner's lets go of it, as E does.</summary>
        protected override bool AltUse(Player player)
        {
            if (Tow == null || HeldBy(player))
                return false;
            Tow.Take(player);
            return true;
        }

        protected override void Buttons(Player player, bool attack, bool attackHold, bool secondaryAttack, bool secondaryAttackHold) =>
            KwArmTrigger.Buttons(this, player, attack, attackHold, secondaryAttack, secondaryAttackHold);

        protected override ItemDrop.ItemData? FindAmmo(Player player) => KwArmReload.Find(this, player);

        protected override void TakeAmmo(Player player, ItemDrop.ItemData ammo, int step) => KwArmReload.Take(this, player);

        protected override OperatorPose Pose(in WeaponState s, OperatorRig rig, Transform body) => KwArmHands.Pose(this, s, rig);

        protected override string HoverText(in WeaponState s, bool mine, bool takeable, string? barred) => KwArmWords.Hover(this, s, mine, takeable, barred);

        // A throw is reported once and stops this cannon's look, rather than once a frame.
        protected override void Draw(in WeaponState s)
        {
            if (broken)
                return;
            broken = true;
            Greet();
            Guard.Run("Broadside cannon", static (a, st) => KwArmLook.Draw(a, st), this, s);
            broken = false;
        }

        // The gunner's hint as he takes hold; the next load is for the attack's shot again.
        private void Greet()
        {
            Player? me = Player.m_localPlayer;
            bool mine = me != null && HeldBy(me);
            if (mine && !greeted)
            {
                Want = 0;
                me!.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(KwArmWords.Hint(Def)));
            }
            greeted = mine;
        }
    }
}
