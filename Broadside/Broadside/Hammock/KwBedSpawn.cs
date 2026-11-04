using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A spawn point on a bed riding a launched ship moves with the ship. The game keeps a bed spawn point as a place in the
    /// world (the player's profile, per world) and takes a bed as the player's current one only within 1 m of it, both to
    /// sleep in it and to wake at it; a ship that sailed on would leave it behind ("set spawn" again, or waking at the
    /// world's start). So when the local player sets his spawn point on a bed riding a ship (<c>Bed.Interact</c>
    /// postfix: the profile's point is that bed's spawn point and the bed rides a ship here), the ship's own id and the
    /// point in the ship's frame are kept (<see cref="KwBedRecord"/>), and the profile's point is moved with the ship
    /// whenever this game has the ship (<see cref="Keep"/>: before the game asks whether a bed is the current one,
    /// <c>Bed.IsCurrent</c> prefix, and once a second, <see cref="KwBedShipWhere"/>), and on a respawn after death from
    /// the ship's place on the server when it is not loaded here (<see cref="KwBedRespawn"/>). A spawn point set on any
    /// other bed, cleared by the game, or moved by anything else (more than 1 m from where this put it) ends the record.
    /// The record is saved in the player's own save per world (<c>Player.Save</c> prefix, read back at
    /// <c>Player.OnSpawned</c>). Local only: the player's profile and save; nothing sent but the server's lookup.
    /// </summary>
    [HarmonyPatch]
    public static class KwBedSpawn
    {
        private const string Prefix = "broadside_bed_";
        private const float Moved = 0.05f, Stale = 1f;
        private static KwBedRecord? record;

        [HarmonyPatch(typeof(Bed), nameof(Bed.Interact)), HarmonyPostfix]
        private static void Interacted(Bed __instance, Humanoid human) => Guard.Run("Broadside bed aboard", Note, __instance, human);

        [HarmonyPatch(typeof(Bed), nameof(Bed.IsCurrent)), HarmonyPrefix]
        private static void Asked()
        {
            if (record != null)
                Guard.Run("Broadside bed aboard", Keep);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Save)), HarmonyPrefix]
        private static void Saving(Player __instance) => Guard.Run("Broadside bed aboard at save", Write, __instance);

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix]
        private static void Spawned(Player __instance) => Guard.Run("Broadside bed aboard at spawn", Read, __instance);

        private static PlayerProfile? Profile => Game.instance != null ? Game.instance.GetPlayerProfile() : null;

        // The spawn point just set (or already) on this bed: kept with its ship when it rides one, else no record.
        private static void Note(Bed bed, Humanoid human)
        {
            PlayerProfile? profile = Profile;
            if (human != Player.m_localPlayer || profile == null || ZNet.instance == null)
                return;
            Vector3 spawn = bed.GetSpawnPoint();
            if (!profile.HaveCustomSpawnPoint() || (profile.GetCustomSpawnPoint() - spawn).sqrMagnitude > 1e-4f)
                return;
            KwShipRiders? ship = bed.TryGetComponent(out KwShipRider rider) ? rider.On : null;
            bool aboard = ship != null && ship.Id != 0L;
            if (aboard && record == null)
                Log.Info("Broadside: your spawn point is a bed aboard a ship; it moves with the ship.");
            record = aboard ? new KwBedRecord(ZNet.instance.GetWorldUID(), ship!.Id, ship.transform.InverseTransformPoint(spawn), spawn) : null;
        }

        /// <summary>The profile's spawn point moved with the record's ship when this game has the ship.</summary>
        public static void Keep()
        {
            PlayerProfile? profile = Profile;
            KwBedRecord? bed = Current(profile);
            KwShipRiders? ship = bed != null ? KwShipRiders.Find(bed.Ship) : null;
            if (ship != null)
                Move(profile!, bed!, ship.transform.TransformPoint(bed!.Local));
        }

        /// <summary>
        /// The record for this world while the profile's spawn point is still where it was put; null otherwise (and the
        /// record ended when the point was cleared or moved by anything else; one of another world is kept for it).
        /// </summary>
        public static KwBedRecord? Current(PlayerProfile? profile)
        {
            if (record == null || profile == null || ZNet.instance == null || record.World != ZNet.instance.GetWorldUID())
                return null;
            if (!profile.HaveCustomSpawnPoint() || (profile.GetCustomSpawnPoint() - record.At).sqrMagnitude > Stale * Stale)
                record = null;
            return record;
        }

        /// <summary>The profile's spawn point put here, when it is more than a few centimetres from where it was.</summary>
        public static void Move(PlayerProfile profile, KwBedRecord bed, Vector3 at)
        {
            if ((at - bed.At).sqrMagnitude < Moved * Moved)
                return;
            profile.SetCustomSpawnPoint(at);
            bed.At = at;
        }

        /// <summary>The record ended (its ship is gone).</summary>
        public static void Forget() => record = null;

        private static string Key => Prefix + ZNet.instance.GetWorldUID();

        private static void Write(Player player)
        {
            if (player != Player.m_localPlayer || ZNet.instance == null)
                return;
            KwBedRecord? bed = Current(Profile);
            if (bed != null)
                player.m_customData[Key] = bed.Format();
            else
                player.m_customData.Remove(Key);
        }

        // At spawning: this world's record from the save, unless this session already has a newer one.
        private static void Read(Player player)
        {
            if (player != Player.m_localPlayer || ZNet.instance == null)
                return;
            long world = ZNet.instance.GetWorldUID();
            if (record != null && record.World == world)
                return;
            record = player.m_customData.TryGetValue(Key, out string text) ? KwBedRecord.Parse(text, world) : null;
        }
    }
}
