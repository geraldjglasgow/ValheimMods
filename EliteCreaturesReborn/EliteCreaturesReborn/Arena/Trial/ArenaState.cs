using UnityEngine;

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
        private static readonly int SoulsKey = ArenaKeys.Souls.GetStableHashCode();
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

        /// <summary>True from Hel's waking to the trial's end, breaks included.</summary>
        public bool Running => IsRunning(Phase);

        /// <summary>The trial's id: when it began. Arena creatures carry it in their tags.</summary>
        public long TrialId { get => Zdo.GetLong(ArenaKeys.TrialHash); set => Zdo.Set(ArenaKeys.TrialHash, value); }

        public int Wave { get => Zdo.GetInt(ArenaKeys.WaveHash); set => Zdo.Set(ArenaKeys.WaveHash, value); }

        /// <summary>The trial's first biome, 0 (Meadows) to 7 (the Deep North).</summary>
        public int StartBiome
        {
            get => Zdo.GetInt(ArenaKeys.StartBiomeHash);
            set => Zdo.Set(ArenaKeys.StartBiomeHash, value);
        }

        /// <summary>The biome tier, 1-8, of a wave of this trial.</summary>
        public int TierOf(int wave) => ArenaTable.BiomeNumber(StartBiome, wave);

        /// <summary>The last wave cleared or swept: its creatures, and any of an earlier wave, vanish.</summary>
        public int Swept { get => Zdo.GetInt(ArenaKeys.SweptHash); set => Zdo.Set(ArenaKeys.SweptHash, value); }

        /// <summary>The waves above <see cref="Swept"/> already cleared while an earlier one still fights, a bit each
        /// (<see cref="DoneBit"/>).</summary>
        public int Done { get => Zdo.GetInt(ArenaKeys.DoneHash); set => Zdo.Set(ArenaKeys.DoneHash, value); }

        /// <summary>A wave's bit in <see cref="Done"/>: bit 0 for wave Swept + 1; 0 for a wave out of its reach.</summary>
        public int DoneBit(int wave)
        {
            int at = wave - Swept - 1;
            return at >= 0 && at <= ArenaTable.MaxWavesAtOnce ? 1 << at : 0;
        }

        /// <summary>The boss waves beaten, a bit each (bit k-1 for wave 5k): the idols the trial pays.</summary>
        public long BossesBeaten
        {
            get => Zdo.GetLong(ArenaKeys.BossesBeatenHash);
            set => Zdo.Set(ArenaKeys.BossesBeatenHash, value);
        }

        /// <summary>The bit of a boss wave in <see cref="BossesBeaten"/>.</summary>
        public static long BossBit(int wave) => 1L << Mathf.Clamp(wave / ArenaTable.WavesPerBiome - 1, 0, 62);

        /// <summary>The archways with creatures still to come, a bit each: the open portals.</summary>
        public int Portals { get => Zdo.GetInt(ArenaKeys.PortalsHash); set => Zdo.Set(ArenaKeys.PortalsHash, value); }

        /// <summary>The souls the trial has earned: what a player there from the start has earned.</summary>
        public long Souls { get => Zdo.GetLong(SoulsKey); set => Zdo.Set(SoulsKey, value); }

        public int Kills { get => Zdo.GetInt(KillsKey); set => Zdo.Set(KillsKey, value); }

        public int Left { get => Zdo.GetInt(LeftKey); set => Zdo.Set(LeftKey, value); }

        public long Next { get => Zdo.GetLong(NextKey); set => Zdo.Set(NextKey, value); }

        public long SeenAt { get => Zdo.GetLong(SeenKey); set => Zdo.Set(SeenKey, value); }

        public int SpawnTurn { get => Zdo.GetInt(TurnKey); set => Zdo.Set(TurnKey, value); }

        /// <summary>True while the running wave is a boss wave.</summary>
        public bool BossWave => Phase == ArenaPhase.Fighting && ArenaTable.IsBossWave(Wave);

        /// <summary>The wave a creature appearing in the hall now joins: the one on, or during the countdown or a break
        /// the one coming.</summary>
        public int JoiningWave => Phase == ArenaPhase.Fighting ? Wave : Wave + 1;

        /// <summary>The wave a creature appearing now joins, a boss (Kall's next phase) the boss wave still fighting when
        /// the next was called before it.</summary>
        public int JoiningWaveOf(bool boss)
        {
            int bossWave = Wave - Wave % ArenaTable.WavesPerBiome;
            bool open = boss && Phase == ArenaPhase.Fighting && bossWave > Swept && (Done & DoneBit(bossWave)) == 0;
            return open ? bossWave : JoiningWave;
        }

        public static bool IsRunning(ArenaPhase phase) =>
            phase == ArenaPhase.Countdown || phase == ArenaPhase.Fighting || phase == ArenaPhase.Break;

        /// <summary>True when the ZDO carries a running trial; a cheap test for every frame.</summary>
        public static bool IsRunning(ZDO? zdo) => zdo != null && IsRunning((ArenaPhase)zdo.GetInt(ArenaKeys.PhaseHash));

        /// <summary>The running trial's id in the ZDO; 0 when none runs.</summary>
        public static long RunningTrial(ZDO? zdo) => IsRunning(zdo) ? zdo!.GetLong(ArenaKeys.TrialHash) : 0L;

        /// <summary>The owner, as Hel wakes: a new trial with its countdown, from biome <paramref name="startBiome"/>.
        /// Everything the last one left is cleared.</summary>
        public void Begin(long now, int startBiome)
        {
            TrialId = now;
            StartBiome = startBiome;
            Wave = 0;
            Swept = 0;
            Done = 0;
            Portals = 0;
            BossesBeaten = 0L;
            Souls = 0L;
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
