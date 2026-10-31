using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The update rate by distance. An object the player already has whose data changed goes every round within
    /// <see cref="NearRange"/> m of the player's reference position, at most once in <see cref="MidRounds"/> rounds out
    /// to <see cref="MidRange"/> m, at most once in <see cref="FarRounds"/> rounds beyond: in a crowd the players beside
    /// you come often and the far side of the town less often. An object new to the player, an owner change and anything
    /// force-sent never waits. While it waits the object stays in the player's pending set, so its latest state goes
    /// when its turn comes. How many rounds ago it last went is read from the time the game records for it
    /// (ZDOPeer.PeerZDOInfo.m_syncTime) against the start times of the last rounds; a round that runs over several frames
    /// counts as one.
    /// </summary>
    internal static class Throttle
    {
        private const float NearRange = 32f;
        private const float MidRange = 64f;
        private const int MidRounds = 2;
        private const int FarRounds = 4;

        private static readonly float[] Starts = new float[FarRounds];
        private static int _round;

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

        /// <summary>True when an object the player has (<paramref name="info"/>), wanted for newer data, waits this round.</summary>
        public static bool Waits(ZDOMan.ZDOPeer.PeerZDOInfo info, ZDO zdo, Vector3 refPos)
        {
            if (zdo.OwnerRevision > info.m_ownerRevision)
            {
                return false;
            }
            float distance2 = (zdo.GetPosition() - refPos).sqrMagnitude;
            if (distance2 <= NearRange * NearRange)
            {
                return false;
            }
            int every = distance2 <= MidRange * MidRange ? MidRounds : FarRounds;
            return info.m_syncTime >= StartOf(every - 1);
        }

        // The start of the round this many before the current one (the session's first round until that many have run).
        private static float StartOf(int roundsBack)
        {
            int slot = ((_round - roundsBack) % FarRounds + FarRounds) % FarRounds;
            return Starts[slot];
        }
    }
}
