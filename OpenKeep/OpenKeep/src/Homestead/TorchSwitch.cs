namespace OpenKeep.Homestead
{
    /// <summary>
    /// Torches Night Only on the fire's ZDO owner. The game's switch is the <c>state</c> ZDO int (1 on, 2 off), read by
    /// <c>IsBurning</c> and <c>UpdateFireplace</c> on every client: an off fire burns no fuel and hides its enabled
    /// object (light, flames, fire area). <c>RPC_ToggleOn</c> flips it and is registered on every fire whether or not it
    /// has <c>m_canTurnOff</c> (that flag only lets the player's Use flip it), so vanilla torches need no prefab change.
    /// The owner flips a scheduled fire only when the torches' night begins or ends: <see cref="PhaseKey"/> keeps the
    /// phase last applied, so an owner change, a reload or a player's own switch in between is left alone. A fire that
    /// leaves the schedule (the setting off, the prefab no longer listed, or kept lit by a player through
    /// <see cref="TorchKeep"/>) is lit again when this mod put it out, and the mark cleared; back on the schedule it
    /// takes the current phase on its next tick.
    /// </summary>
    public static class TorchSwitch
    {
        /// <summary>ZDO int: 0 never switched, 1 put out for the day, 2 lit for the night.</summary>
        public const string PhaseKey = "OpenKeep.torchPhase";

        private const int Day = 1;
        private const int Night = 2;
        private static readonly int PhaseHash = PhaseKey.GetStableHashCode();

        public static void Tick(Fireplace fire)
        {
            ZDO zdo = fire.m_nview.GetZDO();
            int applied = zdo.GetInt(PhaseHash, 0);
            if (Scheduled(zdo))
                Follow(fire, zdo, applied);
            else if (applied != 0)
                Release(fire, zdo, applied);
        }

        /// <summary>A fire this mod put out for the day that is still out: the hover says when it lights. Any client.</summary>
        public static bool OutForDay(Fireplace fire)
        {
            ZNetView view = fire.m_nview;
            if (view == null || !view.IsValid())
                return false;
            ZDO zdo = view.GetZDO();
            return zdo.GetInt(PhaseHash, 0) == Day && !FuelFires.IsOn(zdo) && Scheduled(zdo);
        }

        /// <summary>Torches Night Only is on and the fire's prefab is in Torch Pieces.</summary>
        public static bool Switches(ZDO zdo) => TorchSettings.TorchesNightOnly.Value && TorchPrefabs.IsListed(zdo.GetPrefab());

        /// <summary>The schedule applies: a switched fire no player keeps lit.</summary>
        private static bool Scheduled(ZDO zdo) => Switches(zdo) && !TorchKeep.IsKept(zdo);

        /// <summary>Owner only: the schedule forgets the phase it applied, so the next tick applies the current one at once.</summary>
        public static void Forget(ZDO zdo) => zdo.Set(PhaseHash, 0);

        private static void Follow(Fireplace fire, ZDO zdo, int applied)
        {
            bool? night = TorchNight.Now();
            if (night == null)
                return;
            int phase = night.Value ? Night : Day;
            if (phase == applied)
                return;
            SetOn(fire, zdo, night.Value);
            zdo.Set(PhaseHash, phase);
        }

        private static void Release(Fireplace fire, ZDO zdo, int applied)
        {
            if (applied == Day)
                SetOn(fire, zdo, true);
            Forget(zdo);
        }

        /// <summary>The game's own switch, on the owner (handled at once, the ZDO replicates the state to every client).</summary>
        public static void SetOn(Fireplace fire, ZDO zdo, bool on)
        {
            if (FuelFires.IsOn(zdo) != on)
                fire.m_nview.InvokeRPC("RPC_ToggleOn");
        }
    }
}
