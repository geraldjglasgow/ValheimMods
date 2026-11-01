using System.Collections.Generic;
using Splatform;

namespace HighSeat
{
    /// <summary>
    /// Platform ids as dictionary and set keys, matched exactly as the game matches them (PlatformUserID's own ==: an id
    /// that is not valid equals every other id that is not valid, a valid one equals one with the same platform and the
    /// same user id) and hashed without garbage: the game's own GetHashCode hashes ToString(), a new string at every
    /// call. Equal ids always hash alike (every id that is not valid hashes 0), so the player list patches find a player
    /// in the history the game's FindIndex and List.Contains would find, in one step.
    /// </summary>
    internal sealed class IdKeys : IEqualityComparer<PlatformUserID>
    {
        public static readonly IdKeys Comparer = new IdKeys();

        public bool Equals(PlatformUserID a, PlatformUserID b) => a == b;

        public int GetHashCode(PlatformUserID id) =>
            id.IsValid ? id.m_platform.ToString().GetHashCode() * 31 + id.m_userID.GetHashCode() : 0;
    }
}
