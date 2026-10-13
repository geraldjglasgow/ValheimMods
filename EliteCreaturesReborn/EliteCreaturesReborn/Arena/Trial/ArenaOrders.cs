using EliteCreaturesReborn.Mutations;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// What a creature in the hall does about the trial, judged every second on its own owner
    /// (<see cref="ArenaFighter"/>; features/arena.md section 4). Untagged, inside the hall while a trial runs and not a
    /// pet: it joins the trial (adopted - a boss's next phase, a Summoner's adds, a Splintering creature's copies, which
    /// the game or the mod made on the dying creature's owner), and hunts. Tagged and tamed: the players' now, the tag
    /// comes off. Tagged, with its trial over (ended, another begun, Arena off, or no core to read for ten seconds) or its
    /// wave over (cleared, or swept when its three minutes ran out): it vanishes, dropping nothing but what a Thieving
    /// creature stole from the players. Otherwise it keeps hunting. A creature adopted in the countdown or a break joins
    /// the coming wave.
    /// </summary>
    internal static class ArenaOrders
    {
        /// <summary>Seconds a tagged creature waits on a core its machine does not hold before it vanishes.</summary>
        private const float NoCoreSeconds = 10f;

        public static void Judge(ArenaFighter fighter, float now)
        {
            ZDO? zdo = fighter.Zdo;
            if (zdo == null || !fighter.View.IsOwner() || fighter.Body.IsDead())
            {
                return;
            }
            long trial = ArenaTag.Trial(zdo);
            if (trial == 0L)
            {
                Adopt(fighter, zdo);
            }
            else if (fighter.Body.IsTamed())
            {
                Release(fighter, zdo);
            }
            else if (Over(fighter, zdo, trial, now))
            {
                Vanish(fighter);
            }
            else if (fighter.Ai != null)
            {
                ArenaHunt.Keep(fighter.Ai, fighter.Body);
            }
        }

        // Tamed in the middle of a fight: the players' own now, out of the trial and its count.
        private static void Release(ArenaFighter fighter, ZDO zdo)
        {
            ArenaTag.Clear(zdo);
            fighter.Refresh();
        }

        /// <summary>The creature leaves the world, with no death and so no drops; a Thieving one's stolen goods fall where
        /// it stood, as at any despawn. Owner only.</summary>
        public static void Vanish(ArenaFighter fighter)
        {
            ZDO? zdo = fighter.Zdo;
            if (zdo == null || !fighter.View.IsOwner())
            {
                return;
            }
            if (PouchStore.Count(zdo) > 0)
            {
                PouchDrop.DropAll(PouchStore.Load(zdo), fighter.transform.position);
            }
            fighter.View.Destroy();
        }

        private static void Adopt(ArenaFighter fighter, ZDO zdo)
        {
            Character body = fighter.Body;
            ZDO? core = CoreZdo();
            long running = ArenaState.RunningTrial(core);
            if (running == 0L || body.IsTamed() || !ArenaHall.Contains(body.transform.position))
            {
                return;
            }
            int wave = new ArenaState(core!).JoiningWave;
            ArenaTag.Write(zdo, running, body.IsBoss(), ArenaTable.BiomeNumber(wave), wave);
            ArenaHunt.Begin(body);
            fighter.Refresh();
        }

        private static bool Over(ArenaFighter fighter, ZDO zdo, long trial, float now)
        {
            if (!Config.ArenaSettings.Enabled)
            {
                return true;
            }
            ZDO? core = CoreZdo();
            if (core == null)
            {
                fighter.NoCoreSince = fighter.NoCoreSince < 0f ? now : fighter.NoCoreSince;
                return now - fighter.NoCoreSince >= NoCoreSeconds;
            }
            fighter.NoCoreSince = -1f;
            return ArenaState.RunningTrial(core) != trial || ArenaTag.Wave(zdo) <= core.GetInt(ArenaKeys.SweptHash);
        }

        private static ZDO? CoreZdo()
        {
            ArenaCore? core = ArenaCore.Current;
            return core != null ? core.Zdo : null;
        }
    }
}
