namespace OpenKeep.Homestead
{
    /// <summary>
    /// The conditions <c>Player.Repair</c> puts on the hovered piece, applied to each neighbour without the game's
    /// messages or ward flashes: the piece's crafting station within build range of the player
    /// (<c>Player.CheckCanRemovePiece</c>, waived with placement costs off or the world key <c>NoWorkbench</c>) and
    /// access to every ward the piece stands in (<c>PrivateArea.CheckAccess</c> at the piece's position, radius 0, as
    /// the game calls it). Damage and the one-second wait per piece are <c>WearNTear.Repair</c>'s own checks.
    /// </summary>
    public static class RepairChecks
    {
        public static bool StationInRange(Player player, Piece piece)
        {
            if (player.PlacementCostDisabled || piece.m_craftingStation == null)
                return true;
            if (CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position) != null)
                return true;
            return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench);
        }

        public static bool WardAllows(Piece piece)
        {
            return PrivateArea.CheckAccess(piece.transform.position, 0f, flash: false, wardCheck: false);
        }
    }
}
