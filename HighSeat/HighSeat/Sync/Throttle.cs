namespace HighSeat
{
    /// <summary>
    /// The update rate. An object the player already has whose data changed goes every round within
    /// <see cref="NearRange"/> m of the player's reference position, at most once in <see cref="MidRounds"/> rounds out
    /// to <see cref="MidRange"/> m, at most once in <see cref="FarRounds"/> rounds beyond; and in a crowd only the nearest
    /// few go every round whatever the distance (<see cref="CrowdRank"/>): the slower of the two rates applies. So in a
    /// crowd the players beside you come often and the rest of the crowd and the far side of the town less often. An
    /// object new to the player, an owner change and anything force-sent never waits. While it waits the object stays in
    /// the player's pending set, so its latest state goes when its turn comes, and the player holds it until the round its
    /// turn comes, worked out here by the same rule (<see cref="Holds"/>: the round it last went in and its rate), so
    /// meanwhile it costs one lookup a round. How
    /// many rounds ago it last went is read from the time the game records for it (ZDOPeer.PeerZDOInfo.m_syncTime) against
    /// the start times of the last rounds; a round that runs over several frames counts as one. The round's numbers are
    /// written only at its start, on the main thread, and read by the threads that prepare sends (<see cref="PrepPool"/>).
    /// </summary>
    internal static class Throttle
    {
        private const float NearRange = 32f;
        private const float MidRange = 64f;
        private const int MidRounds = 2;
        private const int FarRounds = 4;

        private static readonly float[] Starts = new float[FarRounds];
        private static int _round;

        /// <summary>The round under way: 1 for the session's first.</summary>
        public static int Round => _round;

        public static void Reset()
        {
            _round = 0;
        }

        /// <summary>A round starts at this Time.time; the session's first fills every slot, standing in for older ones.</summary>
        public static void Started(float time)
        {
            _round++;
            for (int i = 0; i < Starts.Length && _round == 1; i++)
            {
                Starts[i] = time;
            }
            Starts[_round % FarRounds] = time;
        }

        /// <summary>
        /// True when an object the player has (<paramref name="info"/>), wanted for newer data, waits this round, with the
        /// hold that keeps it until its round. <paramref name="distance2"/> is its squared distance from the player's
        /// reference position, <paramref name="crowd"/> the crowd ranks of this send.
        /// </summary>
        public static bool Waits(ZDOMan.ZDOPeer.PeerZDOInfo info, ZDO zdo, float distance2, CrowdRank crowd, out Holds.Hold hold)
        {
            hold = default;
            if (zdo.OwnerRevision > info.m_ownerRevision)
            {
                return false;
            }
            int every = Every(distance2, crowd);
            if (every <= 1 || info.m_syncTime < StartOf(every - 1))
            {
                return false;
            }
            hold = new Holds.Hold { Sent = SentIn(info.m_syncTime, every), Every = every, Distance2 = distance2 };
            return true;
        }

        /// <summary>Rounds between two sends of an object at this squared distance, in this send's crowd: 1, 2 or 4.</summary>
        public static int Every(float distance2, CrowdRank crowd)
        {
            return System.Math.Max(ByDistance(distance2), crowd.Every(distance2));
        }

        private static int ByDistance(float distance2)
        {
            if (distance2 <= NearRange * NearRange)
            {
                return 1;
            }
            return distance2 <= MidRange * MidRange ? MidRounds : FarRounds;
        }

        // The round the player last got it in, for an object that waits (so it went in one of the last every - 1 rounds
        // or this one): the latest of them that had started by then.
        private static int SentIn(float syncTime, int every)
        {
            for (int back = 0; back < every - 1; back++)
            {
                if (syncTime >= StartOf(back))
                {
                    return _round - back;
                }
            }
            return _round - (every - 1);
        }

        // The start of the round this many before the current one (the session's first round until that many have run).
        private static float StartOf(int roundsBack)
        {
            int slot = ((_round - roundsBack) % FarRounds + FarRounds) % FarRounds;
            return Starts[slot];
        }
    }
}
