using System;
using UnityEngine;

namespace HighSeatPlus.Crowd
{
    /// <summary>
    /// Who is drawn simpler, decided again a few times a second by <see cref="CrowdTick"/>. Another player is drawn
    /// simpler only when they are none of these: among the <see cref="NearestInFull"/> nearest players, within
    /// <see cref="FightRange"/> metres (fighting distance), in the local player's party (Party, when installed:
    /// <see cref="PartyLink"/>). So with ten or fewer others nothing changes. Distance is to the nearer of the local
    /// player and the camera, so the people near a build camera or a dead player's view are drawn in full too. Drawn
    /// simpler means no cloth simulation and a lower animation rate (<see cref="FarPlayer.Simplify"/>); nobody is ever
    /// hidden, and hits, colliders and positions are untouched, so PvP stays fair. Nobody flickers at a boundary: someone
    /// in full turns simpler only past wider marks (<see cref="RangeMargin"/> more metres, <see cref="RankMargin"/> more
    /// places) and not within <see cref="KeepFullSeconds"/> of turning full or of loading in, while someone drawn simpler
    /// is back in full at the first decision that finds them near. At most <see cref="MaxSimplerPerDecision"/> turn
    /// simpler per decision, so arriving in a big crowd spreads that work over a few seconds; coming back in full is never
    /// held back.
    /// </summary>
    internal static class CrowdDecision
    {
        /// <summary>The nearest players drawn in full however far they are.</summary>
        internal const int NearestInFull = 10;

        /// <summary>Metres: anyone this near is near enough to fight and drawn in full.</summary>
        internal const float FightRange = 30f;

        /// <summary>Metres past <see cref="FightRange"/> before someone in full turns simpler.</summary>
        internal const float RangeMargin = 5f;

        /// <summary>Places past <see cref="NearestInFull"/> before someone in full turns simpler.</summary>
        internal const int RankMargin = 2;

        /// <summary>Seconds someone stays in full after turning full or loading in.</summary>
        internal const float KeepFullSeconds = 3f;

        /// <summary>Players turned simpler in one decision at most.</summary>
        internal const int MaxSimplerPerDecision = 8;

        private const float NearSqr = FightRange * FightRange;
        private const float FarSqr = (FightRange + RangeMargin) * (FightRange + RangeMargin);

        private static float[] _sorted = new float[64];

        public static void Decide(float now)
        {
            if (!Eyes(out Vector3 body, out Vector3 eye))
            {
                return;
            }
            CrowdList.Refresh(body, eye, now);
            RankLimits(out float nearest, out float nearestWide);
            long self = LocalPlayerId();
            int budget = MaxSimplerPerDecision;
            for (int i = 0; i < CrowdList.Count; i++)
            {
                FarPlayer far = CrowdList.At(i);
                if (far.Simple && !StaysSimple(far, nearest, self))
                {
                    far.Restore(now);
                }
                else if (!far.Simple && budget > 0 && TurnsSimple(far, nearestWide, now, self))
                {
                    far.Simplify();
                    budget--;
                }
            }
        }

        // Drawn simpler now: back in full once among the nearest, within fighting distance or in the party.
        private static bool StaysSimple(FarPlayer far, float nearest, long self) =>
            far.SqrDistance > NearSqr && far.SqrDistance > nearest && !InParty(far, self);

        // Drawn in full now: simpler only past the wider marks, and not just after turning full.
        private static bool TurnsSimple(FarPlayer far, float nearestWide, float now, long self) =>
            now >= far.FullUntil && far.SqrDistance > FarSqr && far.SqrDistance > nearestWide && !InParty(far, self);

        private static bool InParty(FarPlayer far, long self)
        {
            if (far.PlayerId == 0L)
            {
                far.PlayerId = far.Player.GetPlayerID();
            }
            return PartyLink.SameParty(self, far.PlayerId);
        }

        /// <summary>
        /// The squared distance of the <see cref="NearestInFull"/>th nearest player and of the one
        /// <see cref="RankMargin"/> places further; infinite when there are not that many, so then everyone is near.
        /// </summary>
        private static void RankLimits(out float nearest, out float nearestWide)
        {
            int count = CrowdList.Count;
            if (_sorted.Length < count)
            {
                _sorted = new float[Mathf.NextPowerOfTwo(count)];
            }
            for (int i = 0; i < count; i++)
            {
                _sorted[i] = CrowdList.At(i).SqrDistance;
            }
            Array.Sort(_sorted, 0, count);
            nearest = Nth(count, NearestInFull);
            nearestWide = Nth(count, NearestInFull + RankMargin);
        }

        private static float Nth(int count, int n) => n <= count ? _sorted[n - 1] : float.PositiveInfinity;

        /// <summary>The local player's body and the camera; with no local player (dead) both are the camera.</summary>
        private static bool Eyes(out Vector3 body, out Vector3 eye)
        {
            Camera camera = Utils.GetMainCamera();
            Player local = Player.m_localPlayer;
            body = eye = Vector3.zero;
            if (camera == null && local == null)
            {
                return false;
            }
            eye = camera != null ? camera.transform.position : local.transform.position;
            body = local != null ? local.transform.position : eye;
            return true;
        }

        // The profile's id is the one Party keys the local player by; it is there while the player is dead too.
        private static long LocalPlayerId()
        {
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            return profile != null ? profile.GetPlayerID() : 0L;
        }
    }
}
