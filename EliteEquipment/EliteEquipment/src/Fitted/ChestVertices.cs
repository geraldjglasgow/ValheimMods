using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>What a chest vertex below the waist is, as <see cref="ChestPull"/> draws it in.</summary>
    internal enum ChestPart { Other, Arm, Plate, Cloth }

    /// <summary>
    /// A chest's vertices while <see cref="ChestPull"/> moves them: rest positions (metres), texture coordinates, skin
    /// weights, and for each one below <see cref="From"/> what it is (the arms and hands, which never move; the hanging
    /// cloth, by its texture; plates, the rest) and how far it stands out beyond the leggings and a gap. The gap is widest
    /// behind (<see cref="BackGap"/>) so the plates over the seat and the backs of the legs stay loose (the user: "shouldn't
    /// be suctioned so bad to the leg, especially on the back"); the arms are told by distance as well as weight, since
    /// some plate fringes are weighted to the hands.
    /// </summary>
    internal sealed class ChestVertices
    {
        public const float From = 1.06f;
        public const int Sectors = 36;
        private const float FrontGap = 0.025f;
        private const float SideGap = 0.04f;
        private const float BackGap = 0.11f;
        private const float ArmReach = 0.36f;
        private const float ArmLow = 0.80f;

        public ChestVertices(Vector3[] rest, Vector2[] uv, BoneWeight[] weights, bool[] upper, BodyEnvelope envelope)
        {
            Rest = rest;
            Uv = uv;
            Weights = weights;
            Upper = upper;
            Envelope = envelope;
            Kind = new ChestPart[rest.Length];
            Excess = new float[rest.Length];
        }

        public Vector3[] Rest { get; }
        public Vector2[] Uv { get; }
        public BoneWeight[] Weights { get; }

        /// <summary>Weighted to the arms, hands or head at all.</summary>
        public bool[] Upper { get; }

        public BodyEnvelope Envelope { get; }
        public ChestPart[] Kind { get; }

        /// <summary>How far each vertex below the waist stands out beyond the leggings and the gap (negative inside).</summary>
        public float[] Excess { get; }

        /// <summary>The fitted leggings' thickness, metres.</summary>
        public float Thickness { get; private set; }

        public void Measure(float thickness, ChestShape shape)
        {
            Thickness = thickness;
            for (int i = 0; i < Rest.Length; i++)
            {
                Vector3 r = Rest[i];
                if (r.y >= From)
                    continue;
                bool arm = Upper[i] && Envelope.Distance(r) > ArmReach && r.y > ArmLow;
                Kind[i] = arm ? ChestPart.Arm : shape.IsCloth(Uv[i]) ? ChestPart.Cloth : r.y > BodyEnvelope.Low ? ChestPart.Plate : ChestPart.Other;
                Excess[i] = Envelope.Distance(r) - Envelope.At(r) - thickness - Gap(r);
            }
        }

        public float Depth(int i) => Mathf.Max(0f, From - Rest[i].y);

        public int Sector(int i)
        {
            float angle = Mathf.Atan2(Rest[i].z - Envelope.Axis.y, Rest[i].x - Envelope.Axis.x);
            return ((int)Mathf.Floor((angle + Mathf.PI) / (2f * Mathf.PI) * Sectors) % Sectors + Sectors) % Sectors;
        }

        /// <summary>The room left between plate and leggings: most behind (the seat, the backs of the legs), least in front.</summary>
        private float Gap(Vector3 r)
        {
            float forward = Envelope.Outward(r).z;
            return SideGap + (BackGap - SideGap) * Mathf.Max(0f, -forward) + (FrontGap - SideGap) * Mathf.Max(0f, forward);
        }
    }
}
