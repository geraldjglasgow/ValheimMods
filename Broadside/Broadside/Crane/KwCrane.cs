using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cargo crane in the world, worked like a ship's helm. E asks the crane's owner (<see cref="KwCargo.OperateRpc"/>);
    /// the owner lets one operator at a time work it, keeps who in the ZDO (<see cref="KwCargo.OperatorKey"/>) and hands
    /// the crane's ownership to them, the way a chest is handed to whoever opens it, so the operator's game moves the
    /// crane and writes its pose (<see cref="KwCranePose"/>) ten times a second; every other screen follows the ZDO. The
    /// operator stands on the turret under the game's doodad control: Left and Right turn it, Forward and Backward slide
    /// the boom, Jump and Crouch raise and lower the head, Attack picks up a crate, Block sets it down, Run is twice as
    /// fast, Use leaves (<see cref="KwCraneInput"/>, <see cref="KwCraneLift"/>). The owner frees the crane from an operator
    /// who has gone, and a crane with no operator of its own lets its crate go (<see cref="Watch"/>).
    /// </summary>
    public sealed class KwCrane : MonoBehaviour, Interactable, Hoverable, IDoodadController
    {
        public const string Label = "Cargo crane";
        private const float UseRange = 5f, Reach = 3f, SendEvery = 0.1f;
        private static readonly Vector3 Detach = new Vector3(0f, 0.2f, -0.9f);

        private ZNetView? view;
        private KwCraneRig? rig;
        private KwCranePose shown = new KwCranePose(2f), target = new KwCranePose(2f), local = new KwCranePose(2f), sent = new KwCranePose(2f);
        private Vector3 move;
        private bool run, operating, broken;
        private float lastSend, lastWatch;
        private int misses;

        public KwCraneRig? Rig => rig;
        public KwCraneLift? Lift { get; private set; }
        public ZDOID Id => view != null && view.IsValid() ? view.GetZDO().m_uid : ZDOID.None;
        public ZDOID Crate => view != null && view.IsValid() ? view.GetZDO().GetZDOID(KwCargo.CrateKey) : ZDOID.None;
        public ZDOID Operator => view != null && view.IsValid() ? view.GetZDO().GetZDOID(KwCargo.OperatorKey) : ZDOID.None;
        public bool Operating => operating;

        private void Start() => Guard.Run("Broadside cargo crane", static c => c.Init(), this);

        // Removed or broken while worked here: the game stands the operator up; the keys box goes too.
        private void OnDestroy()
        {
            if (operating)
                KwCraneKeys.Hide();
        }

        private void Init()
        {
            view = GetComponent<ZNetView>();
            rig = GetComponent<KwCraneRig>();
            if (view == null || view.GetZDO() == null || rig == null || !rig.Ready)
            {
                enabled = false;
                return;
            }
            Lift = new KwCraneLift(this, view, rig);
            view.Register<ZDOID, bool>(KwCargo.OperateRpc, (sender, who, take) => Guard.Run("Broadside crane request", () => Request(sender, who, take)));
            view.Register<bool>(KwCargo.OperateReplyRpc, (sender, granted) => Guard.Run("Broadside crane reply", () => Reply(granted)));
            view.Register<ZDOID, bool>(KwCargo.HookedRpc, (sender, crate, ok) => Guard.Run("Broadside crane hooked", () => Lift.Hooked(crate, ok)));
            shown.Read(view.GetZDO(), rig.RestDrop);
            rig.Pose(shown);
            if (KwCargoModel.Screens)
                gameObject.AddComponent<KwCraneHands>();
        }

        // A throw is reported once and stops this crane, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside cargo crane", static c => c.Tick(), this);
            broken = false;
        }

        private void Tick()
        {
            if (!view!.IsValid())
                return;
            if (operating)
                Operate(Time.deltaTime);
            else
                Show(Time.deltaTime);
            rig!.Pose(shown);
            if (view.IsOwner())
                Watch();
        }

        // Another game's crane: follow the pose its ZDO holds.
        private void Show(float dt)
        {
            target.Read(view!.GetZDO(), rig!.RestDrop);
            shown.Follow(target, dt);
        }

        // The operator's game: move by the keys, act on a press, send the pose while it changes.
        private void Operate(float dt)
        {
            Player? me = Player.m_localPlayer;
            if (me == null || !me.IsAttached() || me.GetAttachPoint() != rig!.Operator || Operator != me.GetZDOID() && view!.IsOwner())
            {
                Leave(me);
                return;
            }
            float hoist = KwCraneInput.Hoist + Lift!.TakeRaise(dt);
            local.Step(move, hoist, run || KwCraneInput.Run, Lift.Lowest(local), dt);
            if (KwCraneInput.TakePick())
                Lift.PickUp(me);
            if (KwCraneInput.TakeDrop())
                Lift.SetDown(me);
            Lift.Tick();
            shown.CopyFrom(local);
            KwCraneKeys.Keep();
            Send();
        }

        /// <summary>The head jumps up by this much at once (a crate's sling drawn tight when it is hooked).</summary>
        public void Jump(float up) => local.Drop = Mathf.Max(KwCargo.MinDrop, local.Drop - up);

        private void Send()
        {
            if (!view!.IsOwner() || local.Same(sent) || Time.time - lastSend < SendEvery)
                return;
            lastSend = Time.time;
            local.Write(view.GetZDO());
            sent.CopyFrom(local);
        }

        // The game stood the operator up some other way (or another game took the crane): stop working it here.
        private void Leave(Player? me)
        {
            if (me != null && ReferenceEquals(me.GetDoodadController(), this))
                me.StopDoodadControl();
            else
                Stopped(me);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || view == null || !view.IsValid() || !(user is Player player) || player != Player.m_localPlayer)
                return false;
            if (operating)
            {
                player.StopDoodadControl();
                return true;
            }
            if (rig!.Operator == null || Vector3.Distance(player.transform.position, rig.Operator.position) > UseRange)
                return false;
            view.InvokeRPC(KwCargo.OperateRpc, player.GetZDOID(), true);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        // On the owner: one operator at a time (a gone one counts as none); the new operator gets the crane's ownership.
        private void Request(long sender, ZDOID who, bool take)
        {
            if (!view!.IsOwner())
                return;
            ZDOID current = Operator;
            if (!take)
            {
                if (current == who)
                    view.GetZDO().Set(KwCargo.OperatorKey, ZDOID.None);
                return;
            }
            bool free = current.IsNone() || current == who || Gone(current);
            if (free)
                Grant(sender, who);
            view.InvokeRPC(sender, KwCargo.OperateReplyRpc, free);
        }

        private void Grant(long sender, ZDOID who)
        {
            Lift!.Release();
            ZDO zdo = view!.GetZDO();
            zdo.Set(KwCargo.OperatorKey, who);
            if (sender == ZNet.GetUID())
                return;
            ZDOMan.instance.ForceSendZDO(sender, zdo.m_uid);
            zdo.SetOwner(sender);
        }

        // On the asking player's game: stand on the turret under the game's doodad control, or hear that it is taken.
        private void Reply(bool granted)
        {
            Player? me = Player.m_localPlayer;
            if (me == null || operating)
                return;
            if (!granted)
            {
                me.Message(MessageHud.MessageType.Center, "$msg_inuse");
                return;
            }
            local.Read(view!.GetZDO(), rig!.RestDrop);
            sent.CopyFrom(local);
            (operating, move, run) = (true, Vector3.zero, false);
            KwCraneInput.Clear();
            me.StartDoodadControl(this);
            me.AttachStart(rig.Operator, gameObject, true, false, false, "", Detach);
            KwCraneKeys.Show();
        }

        public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block) =>
            (move, this.run) = (operating ? moveDir : Vector3.zero, run);

        public void OnUseStop(Player player) => Stopped(player);

        // Working ends: the crate on the hook is let go, the last pose written, the crane freed, the operator stood down.
        private void Stopped(Player? player)
        {
            if (!operating)
                return;
            operating = false;
            KwCraneInput.Clear();
            if (view != null && view.IsValid() && player != null)
            {
                Lift!.Release();
                if (view.IsOwner())
                    local.Write(view.GetZDO());
                view.InvokeRPC(KwCargo.OperateRpc, player.GetZDOID(), false);
            }
            player?.AttachStop();
            KwCraneKeys.Hide();
        }

        public Component GetControlledComponent() => rig != null && rig.Operator != null ? rig.Operator : this;

        public Vector3 GetPosition() => rig != null && rig.Operator != null ? rig.Operator.position : transform.position;

        public bool IsValid() => this != null && view != null && view.IsValid();

        // On the owner, once a second: an operator gone three checks running is freed (a crashed game never says so),
        // and a crate hangs only from a crane its own operator works on this game (the operator left, or the crane
        // changed owner: it is let go, and settles safely under where it hung).
        private void Watch()
        {
            if (Time.time - lastWatch < 1f)
                return;
            lastWatch = Time.time;
            ZDOID who = Operator;
            misses = !who.IsNone() && Gone(who) ? misses + 1 : 0;
            if (misses >= 3)
                (misses, who) = (0, Free());
            Player? me = Player.m_localPlayer;
            bool mine = operating && me != null && who == me.GetZDOID();
            if (!Crate.IsNone() && !mine && !Lift!.Waiting)
                Lift.Release();
        }

        private ZDOID Free()
        {
            view!.GetZDO().Set(KwCargo.OperatorKey, ZDOID.None);
            return ZDOID.None;
        }

        // An operator with no character (logged out, dead) or whose character is away from the turret.
        private bool Gone(ZDOID who)
        {
            ZDO? character = ZDOMan.instance.GetZDO(who);
            return character == null || Vector3.Distance(character.GetPosition(), rig!.Operator!.position) > Reach;
        }

        public string GetHoverText() => Localization.instance.Localize(KwCraneText.Hover(this));

        public string GetHoverName() => Label;

        public float GetHoverOffset() => 0f;
    }
}
