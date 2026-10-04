using HarmonyLib;
using UnityEngine;
using Wayfare.Core;
using Wayfare.Portals;
using Wayfare.Targeting;

namespace Wayfare.SeaGates
{
    /// <summary>E on a pillar opens the large map as a picker that shows only sea gates: clicking another gate makes it
    /// this gate's destination, clicking this gate opens a text box to rename it. Like a portal's targeting session
    /// (<see cref="TargetingSession"/>) it lives only while the large map is open and ends with the world. The source
    /// is remembered by its gate id and looked up among the loaded gates each time, so a pillar destroyed or unpaired
    /// meanwhile simply ends the picker. Only a player who may change the gate (the right to cycle its access mode)
    /// can open it; the server and the anchor's owner check that again before writing (<see cref="SeaGatePickerEdits"/>).</summary>
    public static class SeaGatePicker
    {
        private const int NameLimit = SeaGatePickerEdits.NameLimit;

        private static readonly string NotYours = Language.Add("wf_sg_notowner", "You don't own this sea gate");

        private static long sourceId;

        public static bool Active { get; private set; }

        /// <summary>The gate the picker was opened at, while it is open and loaded; otherwise null.</summary>
        public static LoadedGate Source => Active ? SeaGateRegistry.FindGate(sourceId) : null;

        public static void Open(SeaGatePillar pillar)
        {
            Player player = Player.m_localPlayer;
            if (!WayfareConfig.Enabled.Value || !WayfareConfig.SeaGatesEnabled.Value || player == null || Minimap.instance == null)
                return;
            if (pillar == null || !pillar.IsValid)
                return;
            LoadedGate gate = SeaGateRegistry.GateOf(pillar);
            if (gate == null)
            {
                player.Message(MessageHud.MessageType.Center, SeaGateWords.Unpaired);
                return;
            }
            if (!MayChange(gate, pillar, player))
                return;
            if (TargetingSession.Active)
                TargetingSession.Close();
            sourceId = gate.Id;
            Active = true;
            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
            SeaGateMapIcons.RequestNow();
        }

        public static void Close()
        {
            if (!Active && sourceId == 0L)
                return;
            Active = false;
            sourceId = 0L;
            SeaGatePickerHint.Hide();
        }

        /// <summary>A left click on the large map while the picker is open; true when it hit a sea gate icon.</summary>
        public static bool TryClick(Vector2 screenPos)
        {
            if (!Active || !SeaGateMapIcons.TryHit(screenPos, out long gateId))
                return false;
            LoadedGate source = Source;
            if (source == null)
                Close();
            else if (gateId == source.Id)
                Rename(source);
            else
                Choose(source, gateId);
            return true;
        }

        /// <summary>Every frame: the picker ends when the map closes, portal targeting starts or the gate is gone.</summary>
        internal static void Tick()
        {
            if (!Active)
                return;
            Minimap map = Minimap.instance;
            if (map == null || map.m_mode != Minimap.MapMode.Large || Player.m_localPlayer == null || TargetingSession.Active || Source == null)
            {
                Close();
                return;
            }
            SeaGatePickerHint.Show();
        }

        private static bool MayChange(LoadedGate gate, SeaGatePillar pillar, Player player)
        {
            if (!PrivateArea.CheckAccess(pillar.transform.position))
            {
                player.Message(MessageHud.MessageType.Center, "$piece_noaccess");
                return false;
            }
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            if (PortalAccess.MayCycle(gate.AnchorZdo, player.GetPlayerID(), isAdmin))
                return true;
            player.Message(MessageHud.MessageType.Center, NotYours);
            return false;
        }

        private static void Choose(LoadedGate source, long destId)
        {
            SeaGatePickerEdits.SendDest(source, destId);
            string name = SeaGatePickerEdits.ListedName(destId);
            string line = string.Format(Localization.instance.Localize(SeaGateWords.DestinationSet), name);
            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, line);
            Close();
            if (Minimap.instance != null)
                Minimap.instance.SetMapMode(Minimap.MapMode.Small);
        }

        private static void Rename(LoadedGate source)
        {
            if (TextInput.instance != null)
                TextInput.instance.RequestText(source.Anchor, SeaGateWords.RenameTopic, NameLimit);
        }
    }

    /// <summary>Leaving the large map (Escape, the map key, the inventory) ends the picker with nothing changed. No
    /// config check: ending a picker that is open is always right, and does nothing when none is.</summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
    public static class SeaGatePickerMapModePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Minimap.MapMode mode)
        {
            if (SeaGatePicker.Active && mode != Minimap.MapMode.Large)
                SeaGatePicker.Close();
        }
    }

    /// <summary>A world unload tears the map down without necessarily changing its mode first; the picker must not
    /// outlive it (see <see cref="GameTeardownPatch"/>).</summary>
    [HarmonyPatch(typeof(Game), "OnDestroy")]
    public static class SeaGatePickerTeardownPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SeaGatePicker.Close();
            SeaGateMapIcons.Clear();
        }
    }
}
