using System.Text;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The Bone Ballista's hold, on every peer (the dedicated server answers the requests). The use key asks the
    /// ballista's owner for it, as the game's ship helm does: the owner grants it when nobody valid holds it, records the
    /// holder in the ZDO and hands the ZDO over to them, so from then on the holder's own machine aims, reloads and
    /// shoots (<see cref="BallistaOperator"/>) and writes what everyone draws. The holder controls it through the game's
    /// doodad control (a ship's helm, a saddle): movement keys do nothing, the mouse aims, attack shoots
    /// (<see cref="BallistaPatches"/>), the use key, jump or dodge let go. Held, the player's hand items are put away
    /// and come back on letting go.
    /// </summary>
    public sealed class BallistaControl : MonoBehaviour, Interactable, Hoverable, IDoodadController
    {
        private const string Request = "ecp_bal_request", Release = "ecp_bal_release", Answer = "ecp_bal_answer";
        private const float UseRange = 3f, HolderRange = 6f, Behind = -0.2f;

        public ZNetView Net { get; private set; } = null!;
        public BallistaParts? Parts { get; private set; }

        /// <summary>The holder's own aim (degrees from where the ballista was placed), ahead of what the ZDO has sent.</summary>
        public float Yaw, Pitch;

        /// <summary>The holder's feet (<see cref="BallistaOperator.Steer"/>): there once, at rest and where, stuck how long.</summary>
        public bool Arrived, Settled;
        public float Stuck;
        public Vector3 RestAt;

        private bool holding;

        private void Awake()
        {
            Net = GetComponent<ZNetView>();
            Parts = BallistaParts.Of(transform);
            if (Net.GetZDO() == null)
            {
                return;   // a placement ghost
            }
            Net.Register<long>(Request, RPC_Request);
            Net.Register<long>(Release, RPC_Release);
            Net.Register<bool>(Answer, RPC_Answer);
        }

        private void FixedUpdate()
        {
            if (holding && Net.IsValid() && Player.m_localPlayer is Player player && player.GetDoodadController() == (IDoodadController)this)
            {
                SafeCall.Run("bone ballista hold", static (control, holder) => BallistaOperator.Tick(control, holder), this, player);
            }
        }

        private void OnDestroy()
        {
            if (holding && Player.m_localPlayer != null)
            {
                Player.m_localPlayer.ShowHandItems();
            }
        }

        /// <summary>A fresh read of the ZDO; only on a valid object.</summary>
        public BallistaState State => BallistaState.Read(Net.GetZDO());

        /// <summary>Whether `player` holds this ballista on this machine.</summary>
        public bool HeldBy(Player player) => holding && player == Player.m_localPlayer && player.GetDoodadController() == (IDoodadController)this;

        public bool Interact(Humanoid character, bool hold, bool alt)
        {
            if (hold || !Net.IsValid() || Parts == null || character is not Player player)
            {
                return false;
            }
            if (HeldBy(player))
            {
                player.StopDoodadControl();
                return true;
            }
            if (Vector3.Distance(player.transform.position, Parts.StandAim.position) > UseRange)
            {
                return false;
            }
            if (Parts.Yaw.InverseTransformPoint(player.transform.position).z > Behind)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_ecp_bal_behind");
                return false;
            }
            Net.InvokeRPC(Request, player.GetPlayerID());
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        /// <summary>The owner: the ballista goes to the asker when nobody valid holds it, the ZDO with it.</summary>
        private void RPC_Request(long sender, long playerID)
        {
            if (!Net.IsOwner())
            {
                return;
            }
            long user = Net.GetZDO().GetLong(BallistaKeys.User, 0L);
            if (user != playerID && ValidUser(user))
            {
                Net.InvokeRPC(sender, Answer, false);
                return;
            }
            Net.GetZDO().Set(BallistaKeys.User, playerID);
            ZDOMan.instance.ForceSendZDO(sender, Net.GetZDO().m_uid);
            Net.GetZDO().SetOwner(sender);
            Net.InvokeRPC(sender, Answer, true);
        }

        private void RPC_Answer(long sender, bool granted)
        {
            Player? player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            if (!granted)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_inuse");
                return;
            }
            BallistaState state = State;
            (Yaw, Pitch, holding) = (state.Yaw, state.Pitch, true);
            (Arrived, Settled, Stuck) = (false, false, 0f);
            player.StartDoodadControl(this);
            player.HideHandItems(false, false);
            Collide(player, false);
        }

        /// <summary>The owner (whoever has the ZDO now): the holder lets go.</summary>
        private void RPC_Release(long sender, long playerID)
        {
            if (Net.IsOwner() && Net.GetZDO().GetLong(BallistaKeys.User, 0L) == playerID)
            {
                BallistaOperator.LetGo(this);
            }
        }

        /// <summary>
        /// The holder's body and the ballista pass through each other while held, as the game does for a chair or a ship's
        /// helm: the player's capsule (0.49 m across from its middle) is wider than the room behind the post, and rubbing
        /// on it slowed every side-step. Local: only the holder's own machine moves their body.
        /// </summary>
        private void Collide(Player player, bool collide)
        {
            if (player.m_collider == null)
            {
                return;
            }
            foreach (Collider part in GetComponentsInChildren<Collider>(true))
            {
                Physics.IgnoreCollision(player.m_collider, part, !collide);
            }
        }

        /// <summary>A holder who is still in the world, alive and at the ballista.</summary>
        private bool ValidUser(long user)
        {
            Player? player = user != 0L ? Player.GetPlayer(user) : null;
            return player != null && !player.IsDead() && Vector3.Distance(player.transform.position, transform.position) < HolderRange;
        }

        public void OnUseStop(Player player)
        {
            holding = false;
            player.ShowHandItems();
            Collide(player, true);
            if (!Net.IsValid())
            {
                return;
            }
            if (Net.IsOwner())
            {
                BallistaOperator.LetGo(this);
            }
            else
            {
                Net.InvokeRPC(Release, player.GetPlayerID());
            }
        }

        public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
        {
        }

        public Component GetControlledComponent() => Parts != null ? Parts.Yaw : transform;

        public Vector3 GetPosition() => transform.position;

        public bool IsValid() => this != null && Net != null && Net.IsValid();

        public string GetHoverName() => Localization.instance.Localize("$" + BallistaPiece.Word);

        public float GetHoverOffset() => 0f;

        public string GetHoverText()
        {
            if (!Net.IsValid())
            {
                return "";
            }
            BallistaState state = State;
            var text = new StringBuilder("$" + BallistaPiece.Word);
            text.Append(state.Settled == Spring.Loaded ? " ($hud_ecp_bal_loaded)" : " ($hud_ecp_bal_empty)");
            bool mine = Player.m_localPlayer != null && HeldBy(Player.m_localPlayer);
            if (mine || !ValidUser(state.User))
            {
                text.Append("\n[<color=yellow><b>$KEY_Use</b></color>] ").Append(mine ? "$hud_ecp_bal_letgo" : "$hud_ecp_bal_hold");
            }
            return Localization.instance.Localize(text.ToString());
        }
    }
}
