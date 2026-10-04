using UnityEngine;
using Wayfare.Core;
using Wayfare.Portals;

namespace Wayfare.SeaGates
{
    /// <summary>The component on every sea gate pillar (added to the prefab by <see cref="SeaGatePiece"/>). Registers
    /// the pillar's RPCs, joins <see cref="SeaGateRegistry"/>, and on the placer's machine starts pairing a fresh
    /// pillar. A placement ghost has no ZDO (<c>ZNetView.m_forceDisableInit</c>) and does none of this. Either pillar
    /// of a gate answers E (the destination picker) and Alt+E (the gate's access mode, kept on the anchor, the same
    /// rule and modes as portals); its hover text is drawn by <see cref="SeaGatePillarHover"/>.</summary>
    public sealed class SeaGatePillar : MonoBehaviour, Hoverable, Interactable, TextReceiver
    {
        private static readonly string NotOwner = Language.Add("wf_sg_notowner", "You don't own this sea gate");

        public ZNetView View { get; private set; }

        public ZDO Zdo => View != null && View.IsValid() ? View.GetZDO() : null;
        public bool IsValid => Zdo != null;
        public long Id => SeaGateFields.GetId(Zdo);

        private static bool On => WayfareConfig.Enabled.Value && WayfareConfig.SeaGatesEnabled.Value;

        private void Awake()
        {
            View = GetComponent<ZNetView>();
            if (View == null || View.GetZDO() == null)
                return;
            View.Register<long, int>(SeaGateFields.PairRpc, (sender, partnerId, sides) => SeaGatePairing.OnPair(this, sender, partnerId, sides));
            View.Register<long>(SeaGateFields.UnpairRpc, (sender, partnerId) => SeaGatePairing.OnUnpair(this, sender, partnerId));
            View.Register<ZPackage>(SeaGateFields.SetDestRpc, (sender, pkg) => SeaGatePickerEdits.OnForwarded(this, sender, SeaGateEditKind.Dest, pkg));
            View.Register<ZPackage>(SeaGateFields.SetNameRpc, (sender, pkg) => SeaGatePickerEdits.OnForwarded(this, sender, SeaGateEditKind.Name, pkg));
            View.Register<ZPackage>(SeaGateFields.SetModeRpc, (sender, pkg) => SeaGatePickerEdits.OnForwarded(this, sender, SeaGateEditKind.Mode, pkg));
            SeaGateRegistry.Add(this);
        }

        /// <summary>A pillar with no id yet was just placed by this machine's player: give it an id and pair it.</summary>
        private void Start()
        {
            if (IsValid && View.IsOwner() && Id == 0L)
                SeaGatePairing.PairNew(this);
        }

        private void OnDestroy() => SeaGateRegistry.Remove(this);

        public string GetHoverText() => SeaGatePillarHover.Text(this, On);

        public string GetHoverName() => SeaGateWords.PillarName;

        public float GetHoverOffset() => 0f;

        /// <summary>E opens the destination picker, Alt+E cycles the gate's access mode. An unpaired pillar only says so.</summary>
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            Player player = user as Player;
            if (hold || !On || !IsValid || player == null || player != Player.m_localPlayer)
                return false;
            LoadedGate gate = SeaGateRegistry.GateOf(this);
            if (gate == null)
            {
                player.Message(MessageHud.MessageType.Center, SeaGateWords.Unpaired);
                return true;
            }
            if (!PrivateArea.CheckAccess(transform.position))
            {
                player.Message(MessageHud.MessageType.Center, "$piece_noaccess");
                return true;
            }
            if (alt)
                TryCycle(gate.Anchor, player);
            else
                SeaGatePicker.Open(this);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        /// <summary>The gate's name as stored on its anchor; empty when unpaired or unnamed.</summary>
        public string GetText()
        {
            LoadedGate gate = SeaGateRegistry.GateOf(this);
            return gate != null ? SeaGateFields.GetName(gate.AnchorZdo) : "";
        }

        /// <summary>A rename from the text box: sent by way of the server, which checks the right
        /// (<see cref="SeaGatePickerEdits"/>).</summary>
        public void SetText(string text)
        {
            LoadedGate gate = SeaGateRegistry.GateOf(this);
            if (gate != null)
                SeaGatePickerEdits.SendName(gate.Anchor, text);
        }

        /// <summary>Client side of Alt+E: the same right as on a portal (unowned, own, or admin), checked here to say
        /// so and again by the server and the anchor's owner, which hold the gate's mode and owner.</summary>
        private static void TryCycle(SeaGatePillar anchor, Player player)
        {
            ZDO zdo = anchor.Zdo;
            if (zdo == null)
                return;
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            if (!PortalAccess.MayCycle(zdo, player.GetPlayerID(), isAdmin))
            {
                player.Message(MessageHud.MessageType.Center, NotOwner);
                return;
            }
            PortalMode mode = PortalFields.GetMode(zdo, WayfareConfig.UnownedPortalsArePublic.Value);
            SeaGatePickerEdits.SendMode(anchor, (PortalMode)(((int)mode + 1) % 3));
        }
    }
}
