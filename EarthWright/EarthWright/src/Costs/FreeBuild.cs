using EarthWright.Core;

namespace EarthWright.Costs
{
    /// <summary>
    /// Free build: the local player's key (F7 by default) switches it while a terrain tool is out, when the server's
    /// "Allow Free Build" lets this player (nobody, admins or everyone). While it is on, terrain entries cost no
    /// stamina, tool wear, materials or volume stone and need no station; the cost line says "Free build". It is
    /// off again as soon as the server stops allowing it, and it starts off for every new character or world session.
    /// Other tools and pieces keep all their costs.
    /// </summary>
    public static class FreeBuild
    {
        private static bool wanted;
        private static Player lastPlayer;

        /// <summary>Free build applies to this player's terrain work right now.</summary>
        public static bool On => wanted && Allowed && GeneralSettings.Active;

        /// <summary>The server's setting lets this player use free build.</summary>
        public static bool Allowed
        {
            get
            {
                switch (CostSettings.AllowFreeBuild.Value)
                {
                    case FreeBuildAccess.Everyone:
                        return true;
                    case FreeBuildAccess.Admins:
                        return Side.LocalIsAdmin;
                    default:
                        return false;
                }
            }
        }

        internal static void Tick()
        {
            Player player = Player.m_localPlayer;
            if (!ReferenceEquals(player, lastPlayer))
            {
                lastPlayer = player;
                wanted = false;
            }
            if (LocalTool.InTerrainTool && Keys.Pressed(CostSettings.FreeBuildKey))
                Toggle();
        }

        private static void Toggle()
        {
            if (!Allowed)
            {
                wanted = false;
                Messages.Center(CostWords.FreeDenied);
                return;
            }
            wanted = !wanted;
            Messages.Center(wanted ? CostWords.FreeOn : CostWords.FreeOff);
        }
    }
}
