using EliteCreaturesReborn.Arena.Trial;
using PatchGuard;

namespace EliteCreaturesReborn.Arena
{
    /// <summary>
    /// The trial's shared face (features/arena.md sections 3 and 4): what every client may read about the running trial,
    /// and the two requests a client makes. Written by the trial (Arena/Trial), read by the hall (doors, runes, Hel's hover).
    /// The reads come straight from the arena core's ZDO on this machine (<see cref="ArenaCore.Current"/>, kept as the core
    /// wakes, never searched for): one or two ZDO lookups, cheap enough for every frame. Where the core is not loaded (far
    /// from the world's centre) no trial is running as far as this machine can see.
    /// </summary>
    public static class ArenaTrial
    {
        /// <summary>True while a trial runs (from the countdown to its end, breaks included), as this client sees the core's
        /// ZDO.</summary>
        public static bool Running => ArenaState.IsRunning(CoreZdo());

        /// <summary>True during the short break after a cleared wave, before the next one comes (15 s, 20 s after a boss
        /// wave): a trial runs, but nothing fights. Not during the countdown.</summary>
        public static bool Break
        {
            get
            {
                ZDO? zdo = CoreZdo();
                return zdo != null && (ArenaPhase)zdo.GetInt(ArenaKeys.PhaseHash) == ArenaPhase.Break;
            }
        }

        /// <summary>The wave running (during a break, the one just cleared), 0 during the countdown or with no
        /// trial.</summary>
        public static int Wave
        {
            get
            {
                ZDO? zdo = CoreZdo();
                return ArenaState.IsRunning(zdo) ? zdo!.GetInt(ArenaKeys.WaveHash) : 0;
            }
        }

        /// <summary>True while the running wave is a boss wave.</summary>
        public static bool BossWave
        {
            get
            {
                ZDO? zdo = CoreZdo();
                return zdo != null && new ArenaState(zdo).BossWave;
            }
        }

        /// <summary>The word key of Hel's hover line for calling the next wave ("Call the next wave").</summary>
        public const string CallNextWord = ArenaWords.CallHint;

        /// <summary>True when the local player, alive in the arena during a wave whose creatures have all come out, may
        /// call the next wave now (Hel's hover). The core's owner checks again, and refuses past a living boss.</summary>
        public static bool CanCallNext => ArenaCalls.CanCall(CoreZdo());

        /// <summary>Asks the core's owner to begin the next wave now, on top of the one fighting (Hel's E during a wave;
        /// features/arena.md section 3, "Calling the next wave"). Nothing happens when the call is not allowed.</summary>
        public static void RequestNextWave() => Guard.Run("ArenaTrial.RequestNextWave", () =>
        {
            ArenaCore? core = ArenaCore.Current;
            if (core != null && core.Zdo != null && CanCallNext)
            {
                ArenaCalls.Ask(core);
            }
        });

        /// <summary>Asks the core's owner to start a trial for the players in the hall (from Hel's interaction): the one
        /// the local player brought from Helgrind. Does nothing when a trial runs, the hall is not built, or the local
        /// player is not in the hall.</summary>
        public static void RequestStart() => Guard.Run("ArenaTrial.RequestStart", Ask);

        private static void Ask()
        {
            ArenaCore? core = ArenaCore.Current;
            if (core == null || core.Zdo == null || Running || !ArenaHall.Built || !HallPlayers.LocalInHall())
            {
                return;
            }
            int mode = ChosenMode();
            if (core.IsOwner)
            {
                ArenaStart.Begin(core, ZNet.GetUID(), mode); // this machine runs the trial: no round trip
            }
            else
            {
                core.View.InvokeRPC(ArenaKeys.StartRpc, mode);
            }
        }

        /// <summary>The trial the local player brought from Helgrind (<see cref="ArenaModes"/>), as the world has it open
        /// now: what Hel's hover offers and what waking her asks for.</summary>
        public static int ChosenMode()
        {
            Player? player = Player.m_localPlayer;
            int mode = ArenaModes.Unset;
            if (player != null && player.m_customData.TryGetValue(ArenaKeys.Mode, out string kept)
                && int.TryParse(kept, out int read))
            {
                mode = read;
            }
            return ArenaModes.Resolve(mode, ArenaModes.Highest());
        }

        /// <summary>The name of <see cref="ChosenMode"/> in this player's language ("Swamp", "Infinite").</summary>
        public static string ChosenName() => ArenaModes.Name(ChosenMode());

        private static ZDO? CoreZdo()
        {
            ArenaCore? core = ArenaCore.Current;
            return core != null ? core.Zdo : null;
        }
    }
}
