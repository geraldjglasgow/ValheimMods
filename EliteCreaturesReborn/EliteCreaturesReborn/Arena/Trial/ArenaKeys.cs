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

        /// <summary>The last wave cleared (or swept after its time ran out): an arena creature of that wave or an earlier
        /// one vanishes on its owner. 0 at the start.</summary>
        public const string Swept = "ecr_arena_swept";

        /// <summary>The boss waves beaten (their boss killed, not swept), one bit each: bit k-1 for wave 10k. Each pays an
        /// idol at the trial's end.</summary>
        public const string BossesBeaten = "ecr_arena_bosses";

        /// <summary>The archways with creatures still to come, one bit each (bit k for <c>portal_k</c>): the open portals
        /// every client draws.</summary>
        public const string Portals = "ecr_arena_portals";

        /// <summary>The trial's score so far (or its final score once it has ended).</summary>
        public const string Score = "ecr_arena_score";

        /// <summary>Arena creatures killed in the trial so far.</summary>
        public const string Kills = "ecr_arena_kills";

        /// <summary>The creatures still to beat: alive plus still to come. The HUD's "7 left".</summary>
        public const string Left = "ecr_arena_left";

        /// <summary>Server clock, ms: the end of the countdown, the running wave's time limit, or the end of the break.</summary>
        public const string Next = "ecr_arena_next";

        /// <summary>Server clock, ms: the last time a living player was in the hall, stamped every second or so.</summary>
        public const string SeenAt = "ecr_arena_seen";

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

        /// <summary>The biome number (1 Meadows to 8 Deep North) of the wave it came in: its kill's score multiplier.</summary>
        public const string CreatureTier = "ecr_arena_tier";

        /// <summary>The wave it fights in: once that wave is cleared or swept (<see cref="Swept"/>), it vanishes.</summary>
        public const string CreatureWave = "ecr_arena_wave_of";

        // ---- The player's own custom data (Player.m_customData), written by the player's own client. ----

        /// <summary>"x,y,z,yaw" (an older "x,y,z" still reads): where Helgrind or `elite arena` took the player from, and
        /// the way out brings them back to (<see cref="Travel.ArenaTravel"/>).</summary>
        public const string ReturnPoint = "ecr_arena_return";

        // ---- RPC names. ----

        /// <summary>On the core's ZNetView, to its owner: a player at Hel asks for a trial.</summary>
        public const string StartRpc = "ecr_arena_start";

        /// <summary>On the core's ZNetView, to its owner: end the trial now (`elite arena stop`).</summary>
        public const string StopRpc = "ecr_arena_stop";

        /// <summary>Routed to the server: `elite arena stop` from a machine that does not hold the core; the server
        /// passes it on to the core's owner.</summary>
        public const string StopFindRpc = "ecr_arena_stop_find";

        /// <summary>On the core's ZNetView, to its owner: an arena creature died (trial id, points, counts as a kill).</summary>
        public const string KillRpc = "ecr_arena_kill";

        /// <summary>Routed, to one player of the trial as it ends: their reward (a package, <c>RewardList</c>).</summary>
        public const string RewardRpc = "ecr_arena_reward";

        /// <summary>Routed, to one player of the trial: a message (word key and up to three words to put in it).</summary>
        public const string MessageRpc = "ecr_arena_message";

        public static readonly int PhaseHash = Phase.GetStableHashCode();
        public static readonly int TrialHash = TrialId.GetStableHashCode();
        public static readonly int WaveHash = Wave.GetStableHashCode();
        public static readonly int CreatureTrialHash = CreatureTrial.GetStableHashCode();
        public static readonly int CreatureBossHash = CreatureBoss.GetStableHashCode();
        public static readonly int CreatureTierHash = CreatureTier.GetStableHashCode();
        public static readonly int CreatureWaveHash = CreatureWave.GetStableHashCode();
        public static readonly int SweptHash = Swept.GetStableHashCode();
        public static readonly int PortalsHash = Portals.GetStableHashCode();
        public static readonly int BossesBeatenHash = BossesBeaten.GetStableHashCode();

        /// <summary>The archways' portal points in the walls' bundle (each on its recess's back face, 2.5 m up, facing the
        /// hall): where creatures come out, one archway per bit of <see cref="Portals"/>.</summary>
        public static readonly IReadOnlyList<string> PortalPoints = new[] { "portal_0", "portal_1", "portal_2", "portal_3" };
    }
}
