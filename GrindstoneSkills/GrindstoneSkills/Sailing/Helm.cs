namespace GrindstoneSkills
{
    /// <summary>
    /// Who steers a ship. The game keeps the helmsman's player ID in the ship's ZDO (ShipControlls.GetUser, set by
    /// the owner when a player takes the helm) and the players aboard in Ship.m_players (filled by the ship's trigger
    /// on every client), so every client that has the ship loaded can find the helmsman.
    /// </summary>
    public static class Helm
    {
        /// <summary>The player at the ship's helm, or null when nobody aboard steers it.</summary>
        public static Player Helmsman(Ship ship)
        {
            ShipControlls controls = ship != null ? ship.m_shipControlls : null;
            long user = controls != null ? controls.GetUser() : 0L;
            if (user == 0L)
                return null;
            foreach (Player player in ship.m_players)
            {
                if (player != null && player.GetPlayerID() == user)
                    return player;
            }
            return null;
        }
    }
}
