using HarmonyLib;
using MapClicks;
using UnityEngine;
using Wayfare.Core;
using Wayfare.Targeting;

namespace Wayfare.SeaGates
{
    /// <summary>The crew's side of a stop (<see cref="JumpChoice"/>): while the ship this player steers or stands aboard
    /// is stopped in a gate, the large map opens as a picker that shows only sea gates, the gate the ship is in in gold,
    /// and every crew member's pointer (<see cref="SeaGatePointers"/>). Only the helmsman picks: their click on another
    /// gate goes to the ship's owner (<see cref="SeaGateFields.PickRpc"/>, after the game's double click window, so a
    /// double click still places a pin), and their closing the map sails on (a pick of 0); while that stop lasts,
    /// sailing on is sent again now and then, in case the ship changed owner on the way. Anyone else closing the map
    /// only closes their own; opening the map again while the ship still waits brings the picker back. The map closes by
    /// itself once the jump begins or the stop ends. Like a portal's targeting session (<see cref="TargetingSession"/>)
    /// it lives only while the large map is open and ends with the world.</summary>
    public static class SeaGatePicker
    {
        private const float ResendSeconds = 1f;

        private static readonly string HelmHint = Language.Add("wf_sg_picker_hint",
            "Click a sea gate to sail there. Close the map to sail on. The crew sees your pointer.");
        private static readonly string CrewHint = Language.Add("wf_sg_picker_crew",
            "{0} at the helm picks the sea gate. The crew sees your pointer.");
        private static readonly string Helmsman = Language.Add("wf_sg_helmsman", "The helmsman");
        private static readonly string HelmPicks = Language.Add("wf_sg_helm_picks", "Only the helmsman picks the sea gate");

        private static long sourceId;
        private static int closedStop;     // the last stop whose map this player closed
        private static bool closedAtHelm;  // closed by the helmsman, which sailed on
        private static float resentAt;

        public static bool Active { get; private set; }

        /// <summary>The ship and stop the picker is open for.</summary>
        internal static Ship Ship { get; private set; }
        internal static int StopId { get; private set; }

        /// <summary>This player holds the helm: their click picks and their closing the map sails on.</summary>
        internal static bool AtHelm { get; private set; }

        /// <summary>The gate the ship stopped in, while the picker is open and the gate loaded; otherwise null.</summary>
        public static LoadedGate Source => Active ? SeaGateRegistry.FindGate(sourceId) : null;

        /// <summary>Every frame on a machine with a map (called by <see cref="SeaGateMapDriver"/>).</summary>
        internal static void Tick()
        {
            if (Active)
                Keep();
            else
                TryOpen();
        }

        private static void TryOpen()
        {
            Player player = Player.m_localPlayer;
            Ship aboard = ShipOf(player);
            ZDO zdo = StopOf(aboard);
            Minimap map = Minimap.instance;
            if (zdo == null || map == null)
                return;
            int stop = zdo.GetInt(SeaGateFields.StopKey, 0);
            bool atHelm = player.GetControlledShip() == aboard;
            bool reopened = !closedAtHelm && map.m_mode == Minimap.MapMode.Large;
            if (stop == closedStop && !reopened)
            {
                if (closedAtHelm && atHelm)
                    ResendSailOn(aboard, stop);
                return;
            }
            Open(aboard, stop, zdo.GetLong(SeaGateFields.StopGateKey, 0L), atHelm);
        }

        private static void Open(Ship aboard, int stop, long gateId, bool atHelm)
        {
            if (TargetingSession.Active)
                TargetingSession.Close();
            Ship = aboard;
            StopId = stop;
            sourceId = gateId;
            AtHelm = atHelm;
            Active = true;
            MapOpening.Large(Minimap.instance);
            SeaGateMapIcons.RequestNow();
        }

        /// <summary>The picker ends with the stop: the jump began, the ship sailed on or this player left the ship.
        /// The map it opened closes with it.</summary>
        private static void Keep()
        {
            Minimap map = Minimap.instance;
            if (map == null || map.m_mode != Minimap.MapMode.Large)
            {
                OnMapClosed();
                return;
            }
            ZDO zdo = StopOf(Ship);
            if (zdo != null && ShipOf(Player.m_localPlayer) == Ship && zdo.GetInt(SeaGateFields.StopKey, 0) == StopId)
            {
                SeaGatePickerHint.Show(Hint());
                return;
            }
            Close();
            map.SetMapMode(Minimap.MapMode.Small);
        }

        /// <summary>The ship this player steers, else the one they stand aboard; null ashore.</summary>
        private static Ship ShipOf(Player player)
        {
            if (player == null)
                return null;
            Ship steered = player.GetControlledShip();
            return steered != null ? steered : Ship.GetLocalShip();
        }

        /// <summary>The ZDO of a ship stopped in a gate for its helmsman to choose; otherwise null.</summary>
        private static ZDO StopOf(Ship stopped)
        {
            ZNetView view = stopped != null ? stopped.m_nview : null;
            ZDO zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            return zdo != null && SeaGateFields.GetState(zdo) == JumpState.Choosing ? zdo : null;
        }

        private static string Hint()
        {
            if (AtHelm)
                return Localization.instance.Localize(HelmHint);
            ShipControlls controls = Ship.m_shipControlls;
            Player helmsman = controls != null && controls.GetUser() != 0L ? Player.GetPlayer(controls.GetUser()) : null;
            string name = helmsman != null ? helmsman.GetPlayerName() : Localization.instance.Localize(Helmsman);
            return string.Format(Localization.instance.Localize(CrewHint), name);
        }

        /// <summary>The player left the large map (Escape, the map key, the inventory): the helmsman's ship sails on;
        /// anyone else only closes their own map.</summary>
        internal static void OnMapClosed()
        {
            if (!Active)
                return;
            closedStop = StopId;
            closedAtHelm = AtHelm;
            resentAt = Time.time;
            if (AtHelm)
                Send(Ship, StopId, 0L);
            Close();
        }

        public static void Close()
        {
            if (!Active && sourceId == 0L)
                return;
            Active = false;
            Ship = null;
            StopId = 0;
            sourceId = 0L;
            AtHelm = false;
            IconClick.Drop();
            SeaGatePickerHint.Hide();
        }

        /// <summary>A left click on the large map while the picker is open; true when it hit a sea gate icon. The gate
        /// the ship is in takes the click and does nothing; so does any gate for anyone but the helmsman.</summary>
        public static bool TryClick(Vector2 screenPos)
        {
            if (!Active || !SeaGateMapIcons.TryHit(screenPos, out long gateId))
                return false;
            if (!AtHelm && Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, HelmPicks);
            else if (gateId != sourceId)
                IconClick.Hold(() => Pick(gateId));
            return true;
        }

        private static void Pick(long gateId)
        {
            if (Active && AtHelm)
                Send(Ship, StopId, gateId);
        }

        private static void ResendSailOn(Ship stopped, int stop)
        {
            if (Time.time - resentAt < ResendSeconds)
                return;
            resentAt = Time.time;
            Send(stopped, stop, 0L);
        }

        private static void Send(Ship target, int stop, long gateId)
        {
            ZNetView view = target != null ? target.m_nview : null;
            if (view != null && view.IsValid())
                view.InvokeRPC(SeaGateFields.PickRpc, stop, gateId);
        }

        /// <summary>A world unload: nothing from this world's stops is kept.</summary>
        internal static void Reset()
        {
            Close();
            closedStop = 0;
            closedAtHelm = false;
        }
    }

    /// <summary>Leaving the large map (Escape, the map key, the inventory) while the picker is open: see
    /// <see cref="SeaGatePicker.OnMapClosed"/>. No config check: ending a picker that is open is always right, and does
    /// nothing when none is. The picker closes itself before it changes the map's mode, so its own close never reads as
    /// the player's.</summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
    public static class SeaGatePickerMapModePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Minimap.MapMode mode)
        {
            if (SeaGatePicker.Active && mode != Minimap.MapMode.Large)
                SeaGatePicker.OnMapClosed();
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
            SeaGatePicker.Reset();
            SeaGatePointers.Clear();
            SeaGateMapIcons.Clear();
        }
    }
}
