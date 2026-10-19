namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Which raid comes: one of the game's own events (<c>RandEventSystem.m_events</c>) that the world has unlocked, drawn
    /// by <see cref="RaidPick"/> when a raid is sounded. The event name is kept in the host's ZDO so every
    /// wave, on whichever machine runs it, brings that event's spawn list; the face is the creature players read the
    /// raid named after.
    /// </summary>
    public sealed class RaidChoice
    {
        public RaidChoice(string eventName, string face)
        {
            EventName = eventName;
            Face = face;
        }

        /// <summary>The game event's <c>m_name</c>, "army_goblin".</summary>
        public string EventName { get; }

        /// <summary>The creature the raid is named after ("Fuling raid") as a word every machine reads in its own language:
        /// the creature's own name word, "$enemy_goblin", or its prefab's name where it has none.</summary>
        public string Face { get; }
    }
}
