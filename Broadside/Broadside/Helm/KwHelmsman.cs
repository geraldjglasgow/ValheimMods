using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// The helmsman at a Broadside helm, on every peer that draws: a launched ship's (on the ship's controls, the game's
    /// ShipControlls, held at a rudder's tiller or deck tiller, <see cref="KwTillerGrip"/>, or at a captain's wheel,
    /// <see cref="KwWheelGrip"/>), or a captain's wheel on the dock (<see cref="KwWheelHelm"/>). Who holds it comes from
    /// replicated state (the ship's helm user or the dock wheel's helmsman, in their ZDO; on his own game, his own
    /// controls), looked up four times a second. After the animator has posed him, his hands go onto what he holds and his
    /// upper body turns and leans to it (<see cref="KwHelmPose"/>), reaching out once it is within his arms' length and
    /// letting go as quickly. On a ship he stays on his own feet (<see cref="Take"/>, <see cref="KwHelmFeet"/>), walked by
    /// the game's own movement, so the game carries his steps to every peer; at a dock wheel the wheel attaches him where
    /// he stands. With nobody at the helm it costs a check four times a second; a dedicated server has none.
    /// </summary>
    public sealed class KwHelmsman : MonoBehaviour
    {
        // How often who holds it is looked up; how fast the hands take hold and let go; within how far of his shoulders
        // (less half their width) what he holds is fully held, and beyond how far not at all.
        private const float LookEvery = 0.25f, Ease = 6f, Reached = 0.75f, Beyond = 1.2f, Shoulders = 1.47f, ShoulderHalf = 0.2f;

        // A dedicated server draws nobody: its helms keep the game's own take, which it never answers for itself.
        private static readonly bool Headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        private ShipControlls? helm;
        private KwWheelHelm? dock;
        private IKwHelmGrip? grip;
        private KwHelmPose? pose;
        private Player? tried;
        private float weight, lookAt;
        private bool holding, broken;

        /// <summary>The Broadside ship's helm the local player holds now, if any.</summary>
        public static KwHelmsman? Mine { get; private set; }

        public KwHelmFeet Feet { get; } = new KwHelmFeet();

        /// <summary>Whether a ship's helm is taken through this rather than the game's own way.</summary>
        public bool Steers => helm != null && grip != null;

        /// <summary>A launched ship's helm and what its helmsman holds (none: the game's own helm, sitting at the stern).</summary>
        public static void OnShip(ShipControlls helm, IKwHelmGrip? grip)
        {
            KwHelmsman? had = helm.GetComponent<KwHelmsman>();
            if (grip == null || Headless)
            {
                if (had != null)
                    Destroy(had);
                return;
            }
            KwHelmsman made = had ?? helm.gameObject.AddComponent<KwHelmsman>();
            (made.helm, made.grip, made.tried, made.pose) = (helm, grip, null, null);
        }

        /// <summary>A captain's wheel on the dock, its turning wheel what the helmsman holds.</summary>
        public static void OnWheel(KwWheelHelm wheel, Transform turning)
        {
            if (Headless)
                return;
            KwHelmsman made = wheel.GetComponent<KwHelmsman>() ?? wheel.gameObject.AddComponent<KwHelmsman>();
            (made.dock, made.grip) = (wheel, new KwWheelGrip(turning));
        }

        /// <summary>Whether this is the ship's helm the local player holds now, standing on his own feet.</summary>
        public bool Walking(Character character) =>
            grip != null && helm != null && ReferenceEquals(character, Player.m_localPlayer) && ReferenceEquals(Player.m_localPlayer.GetDoodadController(), helm);

        /// <summary>His feet toward his stand at the tiller or behind the wheel, this physics step.</summary>
        public void Steer(Player me) => Feet.Steer(helm!.m_ship.transform, grip!.Stand, grip.Crowded(me), me);

        /// <summary>His facing at the helm, this physics step.</summary>
        public float Face(Player me, float dt) => Feet.Face(grip!.Bow, me, dt);

        /// <summary>
        /// On the local player's game, the ship's owner has given him the helm: he steers the ship under the game's doodad
        /// control on his own feet, walked to the tiller; his hand items put away and his body passing through the rudder
        /// while he has it.
        /// </summary>
        public void Take(Player me)
        {
            if (Mine != null && Mine != this)
                Mine.LetGo(me);
            if (me.IsAttached())
                me.AttachStop();
            me.StartDoodadControl(helm!);
            Feet.Start(helm!.m_ship.transform, me);
            if (Mine != this)
            {
                me.HideHandItems(false, false);
                Collide(me, false);
            }
            (Mine, lookAt) = (this, 0f);
        }

        // A throw is reported once and stops this helmsman, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside helmsman", static h => h.Tick(), this);
            broken = false;
        }

        private void Tick()
        {
            if (Mine == this)
                Watch();
            if (Time.time < lookAt)
                return;
            lookAt = Time.time + LookEvery;
            Player? who = Who();
            holding = who != null;
            if (who == null || who == tried)
                return;
            (tried, pose, weight) = (who, KwHelmPose.Of(who), 0f);
            grip?.Reset();
        }

        // Who holds the helm on this peer: the local player while he does here, else the one the ship's or the dock wheel's
        // ZDO names.
        private Player? Who()
        {
            Player? me = Player.m_localPlayer;
            if (dock != null)
                return dock.Steering ? me : Named(dock.Helmsman, me);
            if (helm == null || Mine == this)
                return Mine == this ? me : null;
            long user = helm.GetUser();
            if (user == 0L || !helm.m_ship.IsPlayerInBoat(user))
                return null;
            Player? found = Player.GetPlayer(user);
            return found != me ? found : null;
        }

        // The player a ZDO names, if loaded here; never the local player, whose own game knows whether he holds it.
        private static Player? Named(ZDOID id, Player? me)
        {
            if (id.IsNone() || ZNetScene.instance == null)
                return null;
            GameObject? found = ZNetScene.instance.FindInstance(id);
            Player? player = found != null ? found.GetComponent<Player>() : null;
            return player != me ? player : null;
        }

        private void LateUpdate()
        {
            if (broken || pose == null || grip == null)
                return;
            broken = true;
            Guard.Run("Broadside helmsman pose", static h => h.Pose(), this);
            broken = false;
        }

        // After the animator: his hands toward the peg by how near it is, or letting go once he no longer holds it.
        private void Pose()
        {
            Player player = pose!.Player;
            if (player == null)
            {
                (pose, tried) = (null, null);
                return;
            }
            KwHelmHold hold = grip!.Hold(player, Time.deltaTime);
            weight = Mathf.MoveTowards(weight, holding ? Near(player.transform, hold) : 0f, Ease * Time.deltaTime);
            if (weight > 0.001f)
                pose.Pose(hold, weight);
            else if (!holding)
                (pose, tried) = (null, null);
        }

        // 1 while what he holds is within his arms' length of his shoulders, 0 once it is well beyond.
        private static float Near(Transform body, KwHelmHold hold)
        {
            Vector3 shoulders = body.position + body.up * Shoulders;
            float far = 0f;
            if (hold.Left.HasValue)
                far = Mathf.Max(far, (hold.Left.Value - shoulders).magnitude);
            if (hold.Right.HasValue)
                far = Mathf.Max(far, (hold.Right.Value - shoulders).magnitude);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Reached, Beyond, far - ShoulderHalf));
        }

        // On the local player's game while he holds this helm: once he no longer does (let go, jumped, walked off, died),
        // his hand items and his collisions back.
        private void Watch()
        {
            Player? me = Player.m_localPlayer;
            if (me == null || !ReferenceEquals(me.GetDoodadController(), helm))
                LetGo(me);
        }

        private void LetGo(Player? me)
        {
            (Mine, lookAt) = (null, 0f);
            if (me == null)
                return;
            me.ShowHandItems(false, false);
            Collide(me, true);
        }

        private void OnDestroy()
        {
            if (Mine == this)
                LetGo(Player.m_localPlayer);
        }

        // The helmsman's body and what he holds pass through each other (his capsule would rub on it as he steps).
        private void Collide(Player me, bool collide)
        {
            if (me.m_collider == null || grip == null)
                return;
            foreach (Collider part in grip.Parts.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(me.m_collider, part, !collide);
        }
    }
}
