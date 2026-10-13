namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The trial as it stands, read and written straight in the arena core's ZDO (features/arena.md section 4: "the trial
    /// lives in one networked object"), as <see cref="Raids.RaidState"/> keeps a raid. Nothing is kept beside it, so a
    /// machine that takes the core over carries the trial on from here, and every client draws the doors, the runes and
    /// the HUD line from the same values. Only the core's owner writes; anyone reads. Times are whole milliseconds of the
    /// server clock (<see cref="Util.NetTime"/>). The ZDO skips a write that changes nothing.
    /// </summary>
    public readonly struct ArenaState
    {
        private static readonly int ScoreKey = ArenaKeys.Score.GetStableHashCode();
        private static readonly int KillsKey = ArenaKeys.Kills.GetStableHashCode();
        private static readonly int LeftKey = ArenaKeys.Left.GetStableHashCode();
        private static readonly int NextKey = ArenaKeys.Next.GetStableHashCode();
        private static readonly int SeenKey = ArenaKeys.SeenAt.GetStableHashCode();
        private static readonly int TurnKey = ArenaKeys.SpawnTurn.GetStableHashCode();

        public ArenaState(ZDO zdo) => Zdo = zdo;

        /// <summary>The core's ZDO the values live in.</summary>
        public ZDO Zdo { get; }

        public ArenaPhase Phase
        {
            get => (ArenaPhase)Zdo.GetInt(ArenaKeys.PhaseHash);
            set => Zdo.Set(ArenaKeys.PhaseHash, (int)value);
        }

        /// <summary>True from Hel's waking to the trial's end.</summary>
        public bool Running => IsRunning(Phase);

        /// <summary>The trial's id: when it began. Arena creatures carry it in their tags.</summary>
        public long TrialId { get => Zdo.GetLong(ArenaKeys.TrialHash); set => Zdo.Set(ArenaKeys.TrialHash, value); }

        public int Wave { get => Zdo.GetInt(ArenaKeys.WaveHash); set => Zdo.Set(ArenaKeys.WaveHash, value); }

        public long Score { get => Zdo.GetLong(ScoreKey); set => Zdo.Set(ScoreKey, value); }

        public int Kills { get => Zdo.GetInt(KillsKey); set => Zdo.Set(KillsKey, value); }

        public int Left { get => Zdo.GetInt(LeftKey); set => Zdo.Set(LeftKey, value); }

        public long Next { get => Zdo.GetLong(NextKey); set => Zdo.Set(NextKey, value); }

        public long SeenAt { get => Zdo.GetLong(SeenKey); set => Zdo.Set(SeenKey, value); }

        public int SpawnTurn { get => Zdo.GetInt(TurnKey); set => Zdo.Set(TurnKey, value); }

        /// <summary>True while the running wave is a boss wave.</summary>
        public bool BossWave => Phase == ArenaPhase.Fighting && ArenaTable.IsBossWave(Wave);

        public static bool IsRunning(ArenaPhase phase) => phase == ArenaPhase.Countdown || phase == ArenaPhase.Fighting;

        /// <summary>True when the ZDO carries a running trial; a cheap test for every frame.</summary>
        public static bool IsRunning(ZDO? zdo) => zdo != null && IsRunning((ArenaPhase)zdo.GetInt(ArenaKeys.PhaseHash));

        /// <summary>The running trial's id in the ZDO; 0 when none runs.</summary>
        public static long RunningTrial(ZDO? zdo) => IsRunning(zdo) ? zdo!.GetLong(ArenaKeys.TrialHash) : 0L;

        /// <summary>The owner, as Hel wakes: a new trial with its countdown. Everything the last one left is cleared.</summary>
        public void Begin(long now)
        {
            TrialId = now;
            Wave = 0;
            Score = 0L;
            Kills = 0;
            Left = 0;
            SpawnTurn = 0;
            SeenAt = now;
            Next = now + Ms(ArenaTable.CountdownSeconds);
            Phase = ArenaPhase.Countdown;
        }

        /// <summary>Seconds as whole milliseconds of the server clock.</summary>
        public static long Ms(float seconds) => (long)(seconds * 1000f);
    }
}
