namespace HighSeat
{
    /// <summary>
    /// The server taking a creature or giving one to a player, and what goes with
    /// it: the animators follow (<see cref="AlwaysAnimate"/>), and the player who gains it, or the one who loses it to the
    /// server, gets the ZDO at the front of their next send (ZDOMan.ForceSendZDO), so they start, or stop, running it as
    /// soon as the network allows. The server writes the creature's ZDO until the moment it gives it away, so the new
    /// owner starts from the server's latest state; a creature taken from a player starts from the last state that player
    /// sent, as with the game's own hand-overs.
    /// </summary>
    internal static class Handover
    {
        public static void Take(ZDO zdo, long server)
        {
            long previous = zdo.GetOwner();
            zdo.SetOwner(server);
            if (previous != 0L && previous != server)
            {
                ZDOMan.instance.ForceSendZDO(previous, zdo.m_uid);
            }
            AlwaysAnimate.Follow(zdo, owned: true);
        }

        public static void Give(ZDO zdo, long uid)
        {
            zdo.SetOwner(uid);
            ZDOMan.instance.ForceSendZDO(uid, zdo.m_uid);
            AlwaysAnimate.Follow(zdo, owned: false);
        }
    }
}
