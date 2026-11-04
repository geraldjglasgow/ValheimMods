using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Waking at a bed aboard a ship after death (<see cref="KwBedSpawn"/>). The game's respawn (<c>Game.FindSpawnPoint</c>,
    /// every frame until it has a place) loads the area round the profile's spawn point and takes the bed found there; a
    /// ship that has sailed on since is not there. Before it looks, on a respawn after death (or with no logout point)
    /// with a record of a bed aboard (prefix): when this game has the record's ship, the spawn point is moved with it and
    /// the respawn waits until the ship is built and a moment more (its riders aboard), so the player wakes on the bed and
    /// on a deck that is there; when it has not, the server is asked once where the ship is (<see cref="KwBedShipWhere"/>),
    /// the respawn waiting up to <see cref="Answer"/> seconds for the answer, and the spawn point is moved to the bed's place
    /// on the ship there (the game then loads that area; the ship found there, the first case), or, the ship gone, the
    /// record ends. Never longer than <see cref="Patience"/> seconds all told; after that, or with no record, the game's own
    /// respawn runs unchanged (no bed at the point: the world's start, as for a bed broken while away).
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.FindSpawnPoint))]
    public static class KwBedRespawn
    {
        private const float Answer = 3f, Patience = 20f, Settle = 0.5f, Gap = 1f;
        private enum Asked { Not, Sent, Answered }

        private static float last = -99f, started, built;
        private static Asked asked;

        [HarmonyPrefix]
        private static bool Prefix(Game __instance, ref bool __result, ref Vector3 point, ref bool usedLogoutPoint)
        {
            if (!Guard.Run("Broadside bed aboard respawn", Holds, __instance))
                return true;
            (__result, point, usedLogoutPoint) = (false, Vector3.zero, false);
            return false;
        }

        // Whether the respawn waits this frame.
        private static bool Holds(Game game)
        {
            Begin();
            PlayerProfile? profile = game.m_playerProfile;
            if (profile == null || !game.m_respawnAfterDeath && profile.HaveLogoutPoint() || Time.time - started > Patience)
                return false;
            KwBedRecord? bed = KwBedSpawn.Current(profile);
            if (bed == null)
                return false;
            KwShipRiders? ship = KwShipRiders.Find(bed.Ship);
            return ship != null ? Boarding(ship, bed, profile) : Asking(bed, profile);
        }

        // A new respawn when the last call was more than a moment ago: nothing asked or seen yet.
        private static void Begin()
        {
            if (Time.time - last > Gap)
                (started, built, asked) = (Time.time, -1f, Asked.Not);
            last = Time.time;
        }

        // The ship here: the spawn point kept with it; held until it is built and has had a moment to take its riders aboard.
        private static bool Boarding(KwShipRiders ship, KwBedRecord bed, PlayerProfile profile)
        {
            KwBedSpawn.Move(profile, bed, ship.transform.TransformPoint(bed.Local));
            KwShipBody? body = ship.GetComponent<KwShipBody>();
            if (body != null && !body.Built)
                return true;
            if (built < 0f)
                built = Time.time;
            return Time.time - built < Settle;
        }

        // The ship not here: the server asked where it is, once a respawn; held until it answers, at most Answer seconds.
        private static bool Asking(KwBedRecord bed, PlayerProfile profile)
        {
            if (asked == Asked.Answered)
                return false;
            if (asked == Asked.Not)
            {
                asked = Asked.Sent;
                KwBedShipWhere.Ask(bed.Ship);
            }
            if (!KwBedShipWhere.Answered(bed.Ship, out bool found, out Vector3 at, out Quaternion turn))
                return Time.time - started < Answer;
            asked = Asked.Answered;
            if (found)
                KwBedSpawn.Move(profile, bed, at + turn * bed.Local);
            else
                KwBedSpawn.Forget();
            return false;
        }
    }
}
