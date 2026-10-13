namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>Where the trial stands, kept in the arena core's ZDO as an int.</summary>
    public enum ArenaPhase
    {
        /// <summary>No trial: Hel sleeps, the doors stand open. 0, so a fresh core reads as idle.</summary>
        Idle = 0,

        /// <summary>Hel has woken: the doors close and the first wave comes in ten seconds.</summary>
        Countdown = 1,

        /// <summary>The waves are coming.</summary>
        Fighting = 2,
    }
}
