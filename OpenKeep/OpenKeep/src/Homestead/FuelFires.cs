namespace OpenKeep.Homestead
{
    /// <summary>
    /// The fires auto fuel and the torch switch act on, and where they act: a <c>Fireplace</c> whose ZDO this machine
    /// owns (the game ticks every loaded fire on every client, only the owner writes it) and whose own <c>Piece</c> a
    /// player built. Camp and dungeon fires of locations (creator 0) are never touched.
    /// </summary>
    public static class FuelFires
    {
        public static bool OwnedPlayerFire(Fireplace fire)
        {
            ZNetView view = fire != null ? fire.m_nview : null;
            if (view == null || !view.IsValid() || !view.IsOwner())
                return false;
            Piece piece = fire.m_piece;
            return piece != null && piece.IsPlacedByPlayer();
        }

        /// <summary>The game's switch: <c>state</c> 1 (or unset) is on, 2 is off; an off fire burns nothing and gives no light.</summary>
        public static bool IsOn(ZDO zdo) => zdo.GetInt(ZDOVars.s_state, 1) == 1;
    }
}
