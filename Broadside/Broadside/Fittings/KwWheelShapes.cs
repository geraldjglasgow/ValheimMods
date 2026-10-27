using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The captain's wheel's meshes (<see cref="KwWheel"/>), in kit axes (x forward, y left, z up, metres). The stand, in
    /// the piece's frame: a foot plate on the deck, two frames fore and aft standing on it and a cap over them, the axle's
    /// bearing through both, and between them a closed pedestal from the foot plate up round the axle, where the steering
    /// gear's short shaft comes up through the deck and turns the axle out of sight (<see cref="KwGearTrain"/>). The wheel,
    /// in its own frame on the hub (x along the axle toward the bow): the axle, the hub, a rim of sixteen segments and eight
    /// spokes running out past it as handles.
    /// </summary>
    public static class KwWheelShapes
    {
        /// <summary>The rim's radius; how far out the handles reach.</summary>
        public const float Rim = 0.5f, Handle = 0.72f;

        private const float RimWidth = 0.06f, FrameHalf = 0.22f, CapZ = KwWheel.AxleZ + 0.2f;

        public static void Stand(MeshBuilder b)
        {
            Solid.Box(b, new Vector3(-0.32f, -0.28f, 0f), new Vector3(0.3f, 0.28f, 0.06f));
            foreach (float x in new[] { -0.24f, 0.14f })
                Solid.Box(b, new Vector3(x, -FrameHalf, 0.06f), new Vector3(x + 0.1f, FrameHalf, CapZ));
            Solid.Box(b, new Vector3(-0.28f, -FrameHalf - 0.03f, CapZ), new Vector3(0.28f, FrameHalf + 0.03f, CapZ + 0.06f));
            Solid.Box(b, new Vector3(-0.15f, -0.13f, 0.05f), new Vector3(0.15f, 0.13f, KwWheel.AxleZ + 0.11f));
            foreach (float x in new[] { -0.26f, 0.12f })
                Solid.Log(b, new Vector3(x, 0f, KwWheel.AxleZ), new Vector3(x + 0.14f, 0f, KwWheel.AxleZ), 0.06f, 10);
        }

        // On the hub, the axle along x: the axle into the stand, the hub, the rim, and eight spokes out past it as handles.
        public static void Wheel(MeshBuilder b)
        {
            Solid.Log(b, new Vector3(-0.04f, 0f, 0f), new Vector3(0.6f, 0f, 0f), 0.035f);
            Solid.Log(b, new Vector3(-0.07f, 0f, 0f), new Vector3(0.07f, 0f, 0f), 0.1f);
            for (int i = 0; i < 16; i++)
            {
                float a0 = i * Mathf.PI / 8f, a1 = (i + 1) * Mathf.PI / 8f;
                Solid.Hexa(b, new[]
                {
                    Round(a0, Rim - RimWidth, -0.03f), Round(a0, Rim, -0.03f), Round(a1, Rim, -0.03f), Round(a1, Rim - RimWidth, -0.03f),
                    Round(a0, Rim - RimWidth, 0.03f), Round(a0, Rim, 0.03f), Round(a1, Rim, 0.03f), Round(a1, Rim - RimWidth, 0.03f),
                });
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f + Mathf.PI / 2f;
                Solid.Log(b, Round(a, 0.08f, 0f), Round(a, Rim - 0.02f, 0f), 0.022f, 6);
                Solid.Log(b, Round(a, Rim + 0.01f, 0f), Round(a, Handle, 0f), 0.03f, 6);
            }
        }

        // A point round the axle: angle a from the port side toward the top, radius r, x along the axle.
        private static Vector3 Round(float a, float r, float x) => new Vector3(x, r * Mathf.Cos(a), r * Mathf.Sin(a));
    }
}
