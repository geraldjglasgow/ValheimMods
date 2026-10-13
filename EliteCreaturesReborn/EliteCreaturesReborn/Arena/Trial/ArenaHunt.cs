namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// An arena creature hunts the players at once (features/arena.md section 3: "hunting the players at once"), on its
    /// owner: alerted, its target the nearest living player in the hall, and set to hunt players - the game's own hunt,
    /// kept in its ZDO, so whoever owns it next keeps hunting - and never sent away by daylight, which a night creature
    /// otherwise is when it loses sight of its prey. Also on its owner, every second (<see cref="ArenaFighter"/>), a
    /// creature left with no target is pointed at the nearest player again.
    /// </summary>
    internal static class ArenaHunt
    {
        /// <summary>A creature just made or adopted. Owner only.</summary>
        public static void Begin(Character body)
        {
            MonsterAI? ai = body.GetComponent<MonsterAI>();
            if (ai == null)
            {
                return;
            }
            ai.SetHuntPlayer(true);
            ai.SetDespawnInDay(false);
            ai.Alert();
            Aim(ai, body);
        }

        /// <summary>Points the creature at the nearest player in the hall when it has no one to fight. Owner only.</summary>
        public static void Keep(MonsterAI ai, Character body)
        {
            if (ai.m_targetCreature == null && !body.IsTamed())
            {
                Aim(ai, body);
            }
        }

        private static void Aim(MonsterAI ai, Character body)
        {
            Player? prey = HallPlayers.Nearest(body.transform.position);
            if (prey != null)
            {
                ai.m_targetCreature = prey;
                ai.m_targetStatic = null;
            }
        }
    }
}
