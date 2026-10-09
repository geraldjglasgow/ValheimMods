using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// The outside of a body's hips and legs as seen from the hips' upright axis: for each height band and direction round
    /// the axis, how far the farthest body vertex stands from it (metres, rest space). Both legs together make one shape,
    /// so a plate hanging in front of the thighs is measured against the thighs. Empty cells take the nearest filled one
    /// round the axis. Made once per body, for <see cref="ChestPull"/>.
    /// </summary>
    internal sealed class BodyEnvelope
    {
        public const float Low = 0.30f;
        public const float High = 1.20f;
        private const float Step = 0.02f;
        private const int Sectors = 36;

        private readonly float[,] radius;

        public BodyEnvelope(BodySurface body)
        {
            Axis = HipAxis(body);
            radius = new float[Mathf.CeilToInt((High - Low) / Step) + 1, Sectors];
            for (int i = 0; i < body.Rest.Length; i++)
            {
                Vector3 r = body.Rest[i];
                if (body.Upper[i] || r.y < Low || r.y > High)
                    continue;
                int band = Band(r.y), sector = Sector(Angle(r));
                radius[band, sector] = Mathf.Max(radius[band, sector], Distance(r));
            }
            for (int band = 0; band < radius.GetLength(0); band++)
                FillBand(band);
        }

        /// <summary>The hips' upright axis, x and z in rest space.</summary>
        public Vector2 Axis { get; }

        /// <summary>How far the body reaches from the axis at this height, in the direction of <paramref name="rest"/>.</summary>
        public float At(Vector3 rest) => radius[Band(Mathf.Clamp(rest.y, Low, High)), Sector(Angle(rest))];

        public float Distance(Vector3 rest) => new Vector2(rest.x - Axis.x, rest.z - Axis.y).magnitude;

        /// <summary>The horizontal direction from the axis out to <paramref name="rest"/>.</summary>
        public Vector3 Outward(Vector3 rest) => new Vector3(rest.x - Axis.x, 0f, rest.z - Axis.y).normalized;

        private float Angle(Vector3 rest) => Mathf.Atan2(rest.z - Axis.y, rest.x - Axis.x);

        private static int Band(float height) => Mathf.RoundToInt((height - Low) / Step);

        private static int Sector(float angle) => ((int)Mathf.Floor((angle + Mathf.PI) / (2f * Mathf.PI) * Sectors) % Sectors + Sectors) % Sectors;

        private static Vector2 HipAxis(BodySurface body)
        {
            Vector2 sum = Vector2.zero;
            int count = 0;
            for (int i = 0; i < body.Rest.Length; i++)
            {
                Vector3 r = body.Rest[i];
                if (!body.Upper[i] && r.y >= Unwrap.Crotch && r.y <= LegRegion.Waist)
                {
                    sum += new Vector2(r.x, r.z);
                    count++;
                }
            }
            return count > 0 ? sum / count : Vector2.zero;
        }

        /// <summary>Each empty direction of a band takes its nearest filled neighbour's reach.</summary>
        private void FillBand(int band)
        {
            var filled = new float[Sectors];
            for (int s = 0; s < Sectors; s++)
            {
                filled[s] = radius[band, s];
                for (int step = 1; filled[s] <= 0f && step <= Sectors / 2; step++)
                    filled[s] = Mathf.Max(radius[band, (s + step) % Sectors], radius[band, (s - step + Sectors) % Sectors]);
            }
            for (int s = 0; s < Sectors; s++)
                radius[band, s] = filled[s];
        }
    }
}
