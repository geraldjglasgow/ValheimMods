using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The local player's spawn point on a bed riding a launched ship, as plain data (<see cref="KwBedSpawn"/>): the world
    /// it is in (its UID), the ship's own id (<see cref="KwShipRiders.Id"/>), the bed's spawn point in the ship's frame, and
    /// where in the world the player's profile last had it (<see cref="At"/>). Kept in the player's custom data as
    /// "ship;x;y;z;x;y;z" under a key of the world's.
    /// </summary>
    public sealed class KwBedRecord
    {
        public KwBedRecord(long world, long ship, Vector3 local, Vector3 at) => (World, Ship, Local, At) = (world, ship, local, at);

        public long World { get; }
        public long Ship { get; }

        /// <summary>The spawn point in the ship's frame.</summary>
        public Vector3 Local { get; }

        /// <summary>Where the profile's spawn point was last put, in the world.</summary>
        public Vector3 At { get; set; }

        public string Format() => string.Join(";", Ship.ToString(CultureInfo.InvariantCulture), F(Local.x), F(Local.y), F(Local.z), F(At.x), F(At.y), F(At.z));

        /// <summary>A record read back for this world; null when the text is not one.</summary>
        public static KwBedRecord? Parse(string? text, long world)
        {
            string[] parts = (text ?? "").Split(';');
            float[] n = new float[6];
            if (parts.Length != 7 || !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ship) || ship == 0L)
                return null;
            for (int i = 0; i < 6; i++)
            {
                if (!float.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out n[i]))
                    return null;
            }
            return new KwBedRecord(world, ship, new Vector3(n[0], n[1], n[2]), new Vector3(n[3], n[4], n[5]));
        }

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
