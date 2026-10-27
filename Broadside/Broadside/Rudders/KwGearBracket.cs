using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A bracket holding a climbing stretch of the steering gear's shaft to a cabin's wall (<see cref="KwGearRisers"/>): a
    /// point on the shaft and the level reach from the shaft's axis to the wall's face, in the rudder's kit frame. Saved in
    /// the gear's text as "x,y,z,dx,dy", the brackets joined by ";" (<see cref="KwRudderGear"/>).
    /// </summary>
    public readonly struct KwGearBracket
    {
        public KwGearBracket(Vector3 at, Vector2 reach) => (At, Reach) = (at, reach);

        public Vector3 At { get; }
        public Vector2 Reach { get; }

        public override string ToString() => string.Join(",", F(At.x), F(At.y), F(At.z), F(Reach.x), F(Reach.y));

        /// <summary>The brackets read back from their text (none is fine); null when one does not read.</summary>
        public static KwGearBracket[]? Read(string text)
        {
            if (text.Length == 0)
                return new KwGearBracket[0];
            string[] each = text.Split(';');
            List<KwGearBracket> read = new List<KwGearBracket>(each.Length);
            foreach (string one in each)
            {
                string[] c = one.Split(',');
                float[] v = new float[5];
                if (c.Length != 5)
                    return null;
                for (int i = 0; i < 5; i++)
                {
                    if (!float.TryParse(c[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]) || float.IsNaN(v[i]) || Mathf.Abs(v[i]) >= 1000f)
                        return null;
                }
                read.Add(new KwGearBracket(new Vector3(v[0], v[1], v[2]), new Vector2(v[3], v[4])));
            }
            return read.ToArray();
        }

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
