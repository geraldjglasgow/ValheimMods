namespace Wayfare.Core
{
    /// <summary>How a portal picks its destination (General, Teleport Mode). Map: walking in opens the world map with
    /// every portal on it. TargetTeleport: walking into a named portal opens a window with a dropdown of every other
    /// named portal. Default: the game's own portals, paired by tag, with Wayfare's portal features off.</summary>
    public enum TeleportMode
    {
        Map,
        TargetTeleport,
        Default,
    }
}
