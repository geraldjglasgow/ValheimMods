using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A captain's wheel on the dock, steered the way the game's own helm is. E asks the piece's owner for the helm (the
    /// routed RPC <see cref="Rpc"/>); the owner keeps who holds it in the ZDO (<see cref="HelmsmanKey"/>: the helmsman's
    /// character) and answers (<see cref="ReplyRpc"/>). Granted, the player stands at the wheel under the game's doodad
    /// control, in the game's idle stance with his hands on its handles (<see cref="KwHelmsman"/>, every peer drawing him
    /// from the ZDO): A and D turn the helm at the game's rudder speed (slower near amidships), and it stays where it is
    /// left. The helmsman sends the helm to the owner ten times a second while it changes (<see cref="TurnRpc"/>), who
    /// writes it to the ZDO (<see cref="HelmKey"/>), so every peer turns the wheel and the rudder its steering gear is linked
    /// to (<see cref="KwRudderBlade"/>, following the wheel as drawn, <see cref="Shown"/>, so the gear between them meshes)
    /// from the ZDO alone. E again, jumping or attacking lets go; the owner frees the helm from a helmsman who has gone,
    /// after three checks a second apart. The hover says whether its steering gear reaches the rudder, or why not
    /// (<see cref="KwRudderGearLook.Words"/>).
    /// </summary>
    public sealed class KwWheelHelm : MonoBehaviour, Interactable, Hoverable, IDoodadController
    {
        public const string Rpc = "KW_Helm", TurnRpc = "KW_HelmTurn", ReplyRpc = "KW_HelmReply";

        private const float Speed = 0.5f, UseRange = 3.5f, Reach = 2.5f, SendEvery = 0.1f, Follow = 1.5f;
        private const string Hint = "A and D turn the wheel; it stays where you leave it. [<color=yellow><b>$KEY_Use</b></color>] or jump lets go.";
        private static readonly KeyValuePair<int, int> HelmsmanKey = ZDO.GetHashZDOID("kw_helmsman");
        private static readonly int HelmKey = "kw_helm".GetStableHashCode();
        private static readonly Vector3 Detach = new Vector3(0f, 0.3f, -0.4f);
        private static readonly List<KwWheelHelm> All = new List<KwWheelHelm>();

        private ZNetView? view;
        private Transform? attach, wheel;
        private float local, shown, sent, lastSend, lastWatch;
        private int misses;
        private bool steering, broken;

        /// <summary>The helm: -1 full to port .. 1 full to starboard; on the helmsman's own game, his value as he turns it.</summary>
        public float Value => steering ? local : Stored;

        /// <summary>The helm as the wheel is drawn now (following <see cref="Value"/>, on other peers a moment behind).</summary>
        public float Shown => shown;

        private float Stored => view != null && view.IsValid() ? view.GetZDO().GetFloat(HelmKey) : 0f;

        /// <summary>Who holds the helm, as the ZDO names him (his character); whether the local player steers it now.</summary>
        public ZDOID Helmsman => view != null && view.IsValid() ? view.GetZDO().GetZDOID(HelmsmanKey) : ZDOID.None;
        public bool Steering => steering;

        /// <summary>The nearest wheel loaded on this peer within range of a point, or null.</summary>
        public static KwWheelHelm? Nearest(Vector3 point, float range)
        {
            KwWheelHelm? best = null;
            float bestSq = range * range;
            foreach (KwWheelHelm helm in All)
            {
                float sq = (helm.transform.position - point).sqrMagnitude;
                if (sq < bestSq)
                    (best, bestSq) = (helm, sq);
            }
            return best;
        }

        /// <summary>A rudder angle in words: amidships, or so many degrees to port or starboard.</summary>
        public static string Describe(float degrees) =>
            Mathf.Abs(degrees) < 1f ? "amidships" : $"{Mathf.Abs(degrees):0}° to {(degrees > 0f ? "starboard" : "port")}";

        private void Start() => Guard.Run("Broadside captain's wheel", static w => w.Init(), this);

        private void Init()
        {
            view = GetComponent<ZNetView>();
            attach = transform.Find(KwWheel.AttachName);
            wheel = transform.Find(KwWheel.WheelName);
            if (view == null || view.GetZDO() == null || attach == null || wheel == null)
            {
                enabled = false;
                return;
            }
            view.Register<ZDOID, bool>(Rpc, (sender, who, take) => Guard.Run("Broadside helm request", () => Request(sender, who, take)));
            view.Register<ZDOID, float>(TurnRpc, (sender, who, helm) => Guard.Run("Broadside helm turn", () => Turn(who, helm)));
            view.Register<bool>(ReplyRpc, (sender, granted) => Guard.Run("Broadside helm reply", () => Reply(granted)));
            shown = Stored;
            Draw();
            All.Add(this);
            KwHelmsman.OnWheel(this, wheel);
        }

        private void OnDestroy() => All.Remove(this);

        // A throw is reported once and stops this wheel, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside captain's wheel", static w => w.Tick(), this);
            broken = false;
        }

        private void Tick()
        {
            if (!view!.IsValid())
                return;
            if (steering)
                Hold();
            if (view.IsOwner())
                Watch();
            float target = Value;
            if (Mathf.Approximately(shown, target))
                return;
            shown = steering ? target : Mathf.MoveTowards(shown, target, Follow * Time.deltaTime);
            Draw();
        }

        // The helmsman's game: let go if the game stood him up some other way; else send the helm while it changes.
        private void Hold()
        {
            Player? me = Player.m_localPlayer;
            if (me == null || !me.IsAttached() || me.GetAttachPoint() != attach)
            {
                if (me != null && ReferenceEquals(me.GetDoodadController(), this))
                    me.StopDoodadControl();
                steering = false;
                return;
            }
            if (!Mathf.Approximately(local, sent) && Time.time - lastSend >= SendEvery)
                Send(me);
        }

        private void Send(Player me)
        {
            sent = local;
            lastSend = Time.time;
            view!.InvokeRPC(TurnRpc, me.GetZDOID(), local);
        }

        private void Draw() => wheel!.localRotation = Quaternion.Euler(0f, 0f, -shown * KwWheel.Turn);

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || view == null || !view.IsValid() || !(user is Player player) || player != Player.m_localPlayer)
                return false;
            if (steering)
            {
                player.StopDoodadControl();
                return true;
            }
            if (Vector3.Distance(player.transform.position, attach!.position) > UseRange)
                return false;
            view.InvokeRPC(Rpc, player.GetZDOID(), true);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        // On the owner: a helmsman takes a free helm (or one whose helmsman has gone); only the helmsman lets it go.
        private void Request(long sender, ZDOID who, bool take)
        {
            if (!view!.IsOwner())
                return;
            ZDOID helmsman = Helmsman;
            if (!take)
            {
                if (helmsman == who)
                    view.GetZDO().Set(HelmsmanKey, ZDOID.None);
                return;
            }
            bool free = helmsman.IsNone() || helmsman == who || Gone(helmsman);
            if (free)
                view.GetZDO().Set(HelmsmanKey, who);
            view.InvokeRPC(sender, ReplyRpc, free);
        }

        private void Turn(ZDOID who, float helm)
        {
            if (view!.IsOwner() && who == Helmsman)
                view.GetZDO().Set(HelmKey, Mathf.Clamp(helm, -1f, 1f));
        }

        // On the asking player's game: stand at the wheel under the game's doodad control, or hear that it is taken.
        private void Reply(bool granted)
        {
            Player? me = Player.m_localPlayer;
            if (me == null)
                return;
            if (!granted)
            {
                me.Message(MessageHud.MessageType.Center, "$msg_inuse");
                return;
            }
            local = sent = Stored;
            steering = true;
            me.StartDoodadControl(this);
            me.AttachStart(attach, gameObject, true, false, false, "", Detach);
            me.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(Hint));
        }

        public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
        {
            if (!steering)
                return;
            float rate = Mathf.Lerp(0.5f, 1f, Mathf.Abs(local)) * Speed * Time.deltaTime;
            local = Mathf.Clamp(local + moveDir.x * rate, -1f, 1f);
        }

        public void OnUseStop(Player player)
        {
            steering = false;
            if (view == null || !view.IsValid())
                return;
            view.InvokeRPC(TurnRpc, player.GetZDOID(), local);
            view.InvokeRPC(Rpc, player.GetZDOID(), false);
            player.AttachStop();
        }

        public Component GetControlledComponent() => this;

        public Vector3 GetPosition() => attach != null ? attach.position : transform.position;

        public bool IsValid() => this != null && view != null && view.IsValid();

        // On the owner, once a second: a helmsman gone three checks running loses the helm (a crashed client never says so).
        private void Watch()
        {
            ZDOID helmsman = Helmsman;
            if (helmsman.IsNone() || Time.time - lastWatch < 1f)
                return;
            lastWatch = Time.time;
            misses = Gone(helmsman) ? misses + 1 : 0;
            if (misses < 3)
                return;
            misses = 0;
            view!.GetZDO().Set(HelmsmanKey, ZDOID.None);
        }

        // A helmsman with no character (logged out, dead) or whose character is away from the wheel.
        private bool Gone(ZDOID who)
        {
            ZDO? character = ZDOMan.instance.GetZDO(who);
            return character == null || Vector3.Distance(character.GetPosition(), attach!.position) > Reach;
        }

        public string GetHoverText()
        {
            string gear = KwRudderGearLook.Words(transform.position);
            string helm = $"\nHelm: {Describe(Value * KwRudder.MaxAngle)}" + (gear.Length > 0 ? gear : "\n<color=#888888>Not linked to a rudder yet</color>");
            if (steering)
                return Localization.instance.Localize("Captain's wheel\n[<color=yellow><b>$KEY_Use</b></color>] Let go of the wheel" + helm);
            ZDOID helmsman = Helmsman;
            bool taken = !helmsman.IsNone() && Player.m_localPlayer != null && helmsman != Player.m_localPlayer.GetZDOID();
            string use = taken ? "<color=#888888>Someone has the helm</color>" : "[<color=yellow><b>$KEY_Use</b></color>] Take the helm";
            return Localization.instance.Localize("Captain's wheel\n" + use + helm);
        }

        public string GetHoverName() => "Captain's wheel";

        public float GetHoverOffset() => 0f;
    }
}
