namespace GrindstoneSkills
{
    /// <summary>
    /// The player ID behind a character's ZDOID, read from the player's ZDO (ZDOVars.s_playerID), on whichever machine
    /// has that ZDO: a hit's target owner reads its attacker this way. Players' ZDOs reach every machine near them, so
    /// the owner of something a player is hitting has it. 0 when the ZDO is unknown or not a player's.
    /// </summary>
    public static class PlayerIds
    {
        public static long Of(ZDOID character)
        {
            if (character.IsNone() || ZDOMan.instance == null)
                return 0L;
            ZDO zdo = ZDOMan.instance.GetZDO(character);
            return zdo == null ? 0L : zdo.GetLong(ZDOVars.s_playerID);
        }
    }
}
