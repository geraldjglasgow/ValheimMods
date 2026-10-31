namespace HighSeat
{
    /// <summary>
    /// The ground under the held zones (<see cref="KeptZones"/>), ten times a second just before the game's own check
    /// (ZoneSystem.UpdateTTL), which lets a zone's ground go once it has gone unpoked for 4 s (m_zoneTTL) and no object
    /// instance is left in it. A built zone is poked: its timer is reset, as the game resets the zones round its own
    /// position, so the game never even checks it. A kept zone's missing ground is built through the game's own
    /// PokeLocalZone, the way the game builds its own: at most BuildTries asked a pass (the rest wait, so the terrain
    /// builder's queue is not flooded) and at most one built; only a zone the world has already generated, so keeping a
    /// place never makes new land, trees or locations far from every player. A leaving zone is poked until it is
    /// released and its objects are gone (<see cref="KeptSectors"/>); the game then lets its ground go 4 s later once
    /// nothing is left in it, so the ground always outlives the objects on it, the zone's terrain compiler among them.
    /// While nothing may be kept, nothing is built.
    /// </summary>
    internal static class KeptGround
    {
        private const int BuildTries = 3;

        public static void Keep(ZoneSystem zones, bool build)
        {
            int tries = build ? BuildTries : 0;
            foreach (Vector2s zone in KeptZones.Kept)
            {
                if (Poke(zones, zone) || tries == 0 || !zones.IsZoneGenerated(zone))
                {
                    continue;
                }
                tries--;
                if (zones.PokeLocalZone(zone))
                {
                    tries = 0;
                }
            }
            foreach (Vector2s zone in KeptZones.Leaving)
            {
                Poke(zones, zone);
            }
        }

        // True when the zone's ground is built; its timer starts again.
        private static bool Poke(ZoneSystem zones, Vector2s zone)
        {
            if (!zones.m_zones.TryGetValue(zone, out ZoneSystem.ZoneData data))
            {
                return false;
            }
            data.m_ttl = 0f;
            return true;
        }
    }
}
