using System.Collections.Generic;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Every ZDO key and RPC name of Hel's Trial (features/arena.md section 4), in one place and all prefixed "ecr_arena_",
    /// as the raids keep theirs (<see cref="Raids.RaidKeys"/>). Three kinds of object carry them: the arena core holds the
    /// trial itself (<see cref="ArenaState"/>), each arena creature carries its tag (<see cref="ArenaTag"/>), and a
    /// player's own custom data keeps the point `elite arena` brings them back to. The hashes are made once, for the
    /// paths that read every tick or every frame (the HUD line, the hall's doors and runes).
    /// </summary>
    public static class ArenaKeys
    {
        // ---- The trial, on the arena core's ZDO. Written by the core's owner only. ----

        /// <summary>The <see cref="ArenaPhase"/>, as an int; 0 (Idle) between trials.</summary>
        public const string Phase = "ecr_arena_phase";

        /// <summary>Server clock, ms, when the trial began: also the trial's id in its creatures' tags.</summary>
        public const string TrialId = "ecr_arena_trial";

        /// <summary>The wave running (0 during the countdown), or the one just cleared during a break; after the end, the
        /// wave it ended at.</summary>
        public const string Wave = "ecr_arena_wave";

        /// <summary>The trial's first biome, 0 Meadows to 7 the Deep North, from the trial chosen at Helgrind
        /// (<see cref="ArenaModes"/>).</summary>
        public const string StartBiome = "ecr_arena_start_biome";

        /// <summary>The trial's last biome (<see cref="TrialSpan.Last"/>); a core from before 2026-10-23 has none and reads
        /// the Deep North.</summary>
        public const string LastBiome = "ecr_arena_last_biome";

        /// <summary>True for the Infinite trial (<see cref="TrialSpan.Endless"/>): it goes on past its last biome's boss.
        /// A core from before 2026-10-23 has none and reads true.</summary>
        public const string Endless = "ecr_arena_endless";

        /// <summary>The last wave cleared (or swept after its time ran out): an arena creature of that wave or an earlier
        /// one vanishes on its owner. 0 at the start.</summary>
        public const string Swept = "ecr_arena_swept";

        /// <summary>The waves above <see cref="Swept"/> already cleared while an earlier one still fights (only after a
        /// wave was called early), a bit each: bit 0 for wave Swept + 1 (<see cref="ArenaClears"/>).</summary>
        public const string Done = "ecr_arena_done";

        /// <summary>The boss waves beaten (their boss killed, not swept), one bit each: bit k-1 for wave 5k. Each pays an
        /// idol at the trial's end.</summary>
        public const string BossesBeaten = "ecr_arena_bosses";

        /// <summary>The players who have said they are ready (F) during this break or the countdown, by their peers;
        /// emptied as every wave begins (<see cref="ArenaReady"/>).</summary>
        public const string Ready = "ecr_arena_ready";

        /// <summary>The archways with creatures still to come, one bit each (bit k for <c>portal_k</c>): the open portals
        /// every client draws.</summary>
        public const string Portals = "ecr_arena_portals";

        /// <summary>The souls the trial has earned so far: each kill's souls, which every player of the trial gets in full;
        /// a player's own share is this less what was earned before they joined and what they spent
        /// (<see cref="Wallets"/>).</summary>
        public const string Souls = "ecr_arena_souls";

        /// <summary>Each player's soul wallet (<see cref="SoulWallets"/>): their peer, the souls earned before they joined,
        /// the souls spent and the repairs made at Hel's Anvil.</summary>
        public const string Wallets = "ecr_arena_wallets";

        /// <summary>Arena creatures killed in the trial so far.</summary>
        public const string Kills = "ecr_arena_kills";

        /// <summary>True once the hall's three Bat-fiend guardians have come, the first time anyone was in the hall
        /// (<see cref="ArenaGuardians"/>): never again in this world. Outside the trial's keys, so a trial never resets it.</summary>
        public const string GuardiansCame = "ecr_arena_guardians";

        /// <summary>On a guardian's own ZDO: it drops a Recasting and a Sealed Rune (<see cref="ArenaGuardians"/>).</summary>
        public const string Guardian = "ecr_arena_guardian";

        /// <summary>The creatures still to beat: alive plus still to come. The HUD's "7 left".</summary>
        public const string Left = "ecr_arena_left";

        /// <summary>Server clock, ms: the end of the countdown, the running wave's time limit, or the end of the break.</summary>
        public const string Next = "ecr_arena_next";

        /// <summary>Server clock, ms: the last time a living player was in the hall, stamped every second or so.</summary>
        public const string SeenAt = "ecr_arena_seen";

        /// <summary>Server clock, ms, when Hel recalls the last few creatures of the waves fighting (<see cref="ArenaRecall"/>);
        /// 0 while no recall runs. Every client marks the stragglers while it is set.</summary>
        public const string RecallAt = "ecr_arena_recall";

        /// <summary>The creatures the waves fighting had when the latest began, alive and still to come: what a quarter
        /// left is counted from (<see cref="ArenaRecall"/>).</summary>
        public const string WaveSize = "ecr_arena_size";

        /// <summary>The archway the next wave's first creature is given to, 0 to 3, so the archways take turns.</summary>
        public const string SpawnTurn = "ecr_arena_turn";

        /// <summary>The creatures still to come (<see cref="ArenaQueue"/>): kind, wave and count, so a new owner sends the
        /// same ones.</summary>
        public const string Queue = "ecr_arena_queue";

        /// <summary>The peers of every player who has been in the hall during this trial (<see cref="TrialPlayers"/>):
        /// whom its messages go to.</summary>
        public const string Players = "ecr_arena_players";

        // ---- The tag, on each arena creature's ZDO. Written by its owner as it is made or adopted. ----

        /// <summary>The trial it fights in (<see cref="TrialId"/>); 0 for any other creature.</summary>
        public const string CreatureTrial = "ecr_arena_of";

        /// <summary>True on the wave's boss, and on whatever its death or its aspects bring in its place.</summary>
        public const string CreatureBoss = "ecr_arena_boss";

        /// <summary>The biome number (1 Meadows to 8 Deep North) of the wave it came in: its kill's souls multiplier.</summary>
        public const string CreatureTier = "ecr_arena_tier";

        /// <summary>The wave it fights in: once that wave is cleared or swept (<see cref="Swept"/>), it vanishes.</summary>
        public const string CreatureWave = "ecr_arena_wave_of";

        // ---- The player's own custom data (Player.m_customData), written by the player's own client. ----

        /// <summary>"x,y,z,yaw" (an older "x,y,z" still reads): where Helgrind or `elite arena` took the player from, and
        /// the way out brings them back to (<see cref="Travel.ArenaTravel"/>).</summary>
        public const string ReturnPoint = "ecr_arena_return";

        /// <summary>The trial the player brought from the Helgrind they walked through (<see cref="ArenaModes"/>: 0 to 7 a
        /// biome's, 8 Infinite), sent to the core's owner when they wake Hel.</summary>
        public const string Mode = "ecr_arena_mode";

        // ---- On Helgrind's own ZDO, written by whoever pressed E there (taking its ownership first). ----

        /// <summary>The trial the gate is set to (<see cref="ArenaModes"/>); none set: the highest open biome's.</summary>
        public const string GateMode = "ecr_helgrind_mode";

        // ---- RPC names. ----

        /// <summary>On the core's ZNetView, to its owner: a player at Hel asks for a trial, with the trial they chose
        /// (an int, <see cref="Mode"/>).</summary>
        public const string StartRpc = "ecr_arena_start";

        /// <summary>On the core's ZNetView, to its owner: end the trial now (`elite arena stop`).</summary>
        public const string StopRpc = "ecr_arena_stop";

        /// <summary>Routed to the server: `elite arena stop` from a machine that does not hold the core; the server
        /// passes it on to the core's owner.</summary>
        public const string StopFindRpc = "ecr_arena_stop_find";

        /// <summary>On the core's ZNetView, to its owner: an arena creature died (trial id, points, counts as a kill).</summary>
        public const string KillRpc = "ecr_arena_kill";

        /// <summary>On the core's ZNetView, to its owner: a player pressed F to say they are ready, or to take it back.</summary>
        public const string ReadyRpc = "ecr_arena_ready_vote";

        /// <summary>On the core's ZNetView, to its owner: a player at Hel calls the next wave now (the caller's name).</summary>
        public const string CallRpc = "ecr_arena_call_next";

        /// <summary>On the core's ZNetView, to its owner: a player at Hel's Anvil asks to repair (the anvil's position).</summary>
        public const string RepairRpc = "ecr_arena_repair";

        /// <summary>Routed, to the player who asked: the anvil's answer (repaired or not, the souls it cost).</summary>
        public const string RepairedRpc = "ecr_arena_repaired";

        /// <summary>Routed, to each player of the trial: a burst of cold soul-fire at a point (where, how large).</summary>
        public const string FlareRpc = "ecr_arena_flare";

        /// <summary>Routed, to one player of the trial as it ends: their reward (a package, <c>RewardList</c>).</summary>
        public const string RewardRpc = "ecr_arena_reward";

        /// <summary>Routed, to one player of the trial: a message, its word key and values as one string (LangWire).</summary>
        public const string MessageRpc = "ecr_arena_message";

        public static readonly int PhaseHash = Phase.GetStableHashCode();
        public static readonly int TrialHash = TrialId.GetStableHashCode();
        public static readonly int WaveHash = Wave.GetStableHashCode();
        public static readonly int CreatureTrialHash = CreatureTrial.GetStableHashCode();
        public static readonly int CreatureBossHash = CreatureBoss.GetStableHashCode();
        public static readonly int CreatureTierHash = CreatureTier.GetStableHashCode();
        public static readonly int CreatureWaveHash = CreatureWave.GetStableHashCode();
        public static readonly int SweptHash = Swept.GetStableHashCode();
        public static readonly int DoneHash = Done.GetStableHashCode();
        public static readonly int StartBiomeHash = StartBiome.GetStableHashCode();
        public static readonly int LastBiomeHash = LastBiome.GetStableHashCode();
        public static readonly int EndlessHash = Endless.GetStableHashCode();
        public static readonly int GateModeHash = GateMode.GetStableHashCode();
        public static readonly int PortalsHash = Portals.GetStableHashCode();
        public static readonly int BossesBeatenHash = BossesBeaten.GetStableHashCode();
        public static readonly int RecallAtHash = RecallAt.GetStableHashCode();

        /// <summary>The archways' portal points in the walls' bundle (each on its recess's back face, 2.5 m up, facing the
        /// hall): where creatures come out, one archway per bit of <see cref="Portals"/>.</summary>
        public static readonly IReadOnlyList<string> PortalPoints = new[]
        {
            "portal_0", "portal_1", "portal_2", "portal_3", "portal_4", "portal_5", "portal_6", "portal_7",
        };
    }
}
