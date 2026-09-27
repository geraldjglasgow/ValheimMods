namespace EarthWright.Gear
{
    /// <summary>
    /// Longer reach while a terrain tool is in the local player's hand: the player's placement distance
    /// (<c>m_maxPlaceDistance</c>, which the game's placement ray test reads) is set to "Reach", and the game's own value is
    /// put back as soon as the tool is put away, EarthWright is switched off or the player object changes. The game's
    /// placement ray is 50 m long, hence the setting's upper bound. Placement is decided on the placing client, so this
    /// needs nothing from the server beyond the synced setting.
    /// </summary>
    public static class Reach
    {
        private static Player owner;
        private static float saved;

        internal static void Update()
        {
            Player player = Player.m_localPlayer;
            if (!ReferenceEquals(owner, player))
                Restore();
            if (player != null && HeldTool.Active)
                Apply(player);
            else
                Restore();
        }

        private static void Apply(Player player)
        {
            if (owner == null)
            {
                owner = player;
                saved = player.m_maxPlaceDistance;
            }
            player.m_maxPlaceDistance = GearSettings.Reach.Value;
        }

        private static void Restore()
        {
            if (ReferenceEquals(owner, null))
                return;
            if (owner != null)
                owner.m_maxPlaceDistance = saved;
            owner = null;
        }
    }
}
