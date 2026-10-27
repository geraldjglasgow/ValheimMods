using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The steering gear's toothed parts (<see cref="KwGearTrain"/>), in kit axes (x forward, y left, z up, metres), cut
    /// like a mill's: wooden wheels with wooden teeth, banded in the rudder's fittings. The quadrant on the rudder's stock
    /// (in the blade's frame, so it turns with the blade): a hub, three arms and a toothed rim facing aft, its teeth on its
    /// top face square to the shaft where the pinion meets them. The pinion: eight teeth round the shaft's after end, one
    /// pointing down at the quadrant with the shaft unturned. A bevel wheel: a cone's ring with its teeth along the cone,
    /// one at the point where it meets its partner (the wheel nearer the captain's wheel) or a gap there (the other), so
    /// with the two turning alike about their shafts' ways the teeth keep meshing. Tooth places are plain functions, so
    /// offline checks see the same teeth the meshes are cut with.
    /// </summary>
    public static class KwGearShapes
    {
        private const float Face = 0.07f, BevelFace = 0.04f, Fill = 0.46f;
        private static readonly Vector3 Up = new Vector3(0f, 0f, 1f);

        /// <summary>The quadrant on the rudder's stock, in the blade's frame (the stock's axis at the origin).</summary>
        public static void Quadrant(MeshBuilder wood, MeshBuilder fitting, KwRudderKit k)
        {
            float offset = KwGearTrain.Offset(k.Tier, k.Fit), r = KwGearTrain.Radius(offset), top = KwGearTrain.PlateTop(k.Lay);
            float contact = KwGearTrain.Contact(offset), span = KwRudder.MaxAngle + KwGearTrain.Spare, low = top - KwGearTrain.Plate;
            float hub = k.Stock + 0.02f;
            Solid.Log(wood, new Vector3(0f, 0f, low - 0.03f), new Vector3(0f, 0f, KwGearTrain.HubTop(k.Lay)), hub, 10);
            foreach (float z in new[] { low - 0.02f, top + 0.012f })
                KwRound.Ring(fitting, new Vector3(0f, 0f, z), Up, hub + 0.004f, 0.01f, 12, 4);
            for (float a = contact - span; a < contact + span - 0.01f; a += 4f)
                Rim(wood, a, Mathf.Min(a + 4f, contact + span), r, low, top);
            foreach (float a in new[] { contact - span + 5f, contact, contact + span - 5f })
                Arm(wood, a, hub, r - KwGearTrain.Band, low + 0.006f, top - 0.006f);
            int teeth = Mathf.FloorToInt(span / KwGearTrain.Pitch - 0.5f);
            for (int t = -teeth; t < teeth; t++)
                Ridge(wood, QuadrantTooth(t, offset, top), KwGearTrain.Band * 2f, Width(KwGearTrain.Working * KwGearTrain.Pitch * Mathf.Deg2Rad), KwGearTrain.Tooth);
        }

        /// <summary>
        /// A quadrant tooth's middle at its foot, its way along (square to the shaft where the pinion meets it) and up: the
        /// teeth sit half a tooth either side of the place the pinion meets with the rudder amidships (a gap there).
        /// </summary>
        public static (Vector3 At, Vector3 Along, Vector3 Up) QuadrantTooth(int t, float offset, float top)
        {
            float turn = (t + 0.5f) * KwGearTrain.Pitch;
            Vector3 at = new Vector3(-KwGearTrain.Working, offset, top);
            return (KwGearTrain.Turn(at, Up, turn), KwGearTrain.Turn(Vector3.right, Up, turn), Up);
        }

        /// <summary>The pinion round the shaft's after end (its middle at a point), the shaft running a way.</summary>
        public static void Pinion(MeshBuilder wood, MeshBuilder fitting, Vector3 at, Vector3 way)
        {
            float core = KwGearTrain.Pinion - KwGearTrain.Tooth / 2f;
            Solid.Log(wood, at - way * (Face / 2f), at + way * (Face / 2f), core, 12);
            KwRound.Ring(fitting, at + way * (Face / 2f + 0.006f), way, KwGearTrain.Shaft + 0.008f, 0.009f, 10, 4);
            for (int i = 0; i < KwGearTrain.PinionTeeth; i++)
            {
                (Vector3 c, Vector3 out_) = PinionTooth(i, at, way);
                Ridge(wood, (c, way, out_), Face, Width(2f * Mathf.PI * KwGearTrain.Pinion / KwGearTrain.PinionTeeth), KwGearTrain.Tooth);
            }
        }

        /// <summary>A pinion tooth's middle at its foot and the way it points, the shaft unturned: the first one straight down.</summary>
        public static (Vector3 At, Vector3 Out) PinionTooth(int i, Vector3 at, Vector3 way)
        {
            Vector3 out_ = KwGearTrain.Turn(-Up, way, i * 360f / KwGearTrain.PinionTeeth);
            return (at + out_ * (KwGearTrain.Pinion - KwGearTrain.Tooth / 2f), out_);
        }

        /// <summary>
        /// A bevel wheel: at a corner (the cones' apex), on the shaft going out from it a way, meeting its partner toward
        /// another way; a tooth at the meeting (lead) or a gap.
        /// </summary>
        public static void Bevel(MeshBuilder wood, MeshBuilder fitting, Vector3 apex, Vector3 axis, Vector3 toward, bool lead)
        {
            float half = Mathf.Acos(Mathf.Clamp(Vector3.Dot(axis, toward), -1f, 1f)) / 2f, back = KwGearTrain.Bevel / Mathf.Tan(half);
            float near = back - BevelFace / 2f * Mathf.Cos(half), far = back + BevelFace / 2f * Mathf.Cos(half);
            KwRound.Taper(wood, apex + axis * near, apex + axis * far, near * Mathf.Tan(half) - 0.012f, far * Mathf.Tan(half) - 0.012f, 14);
            Solid.Log(wood, apex + axis * far, apex + axis * (far + 0.035f), 0.045f, 10);
            KwRound.Ring(fitting, apex + axis * (far + 0.03f), axis, 0.047f, 0.008f, 10, 4);
            for (int i = 0; i < KwGearTrain.BevelTeeth; i++)
            {
                (Vector3 pitch, Vector3 r) = BevelTooth(apex, axis, toward, lead, i);
                Vector3 slant = (axis * Mathf.Cos(half) + r * Mathf.Sin(half)).normalized, normal = r * Mathf.Cos(half) - axis * Mathf.Sin(half);
                Ridge(wood, (pitch - normal * (KwGearTrain.Tooth / 2f), slant, normal), BevelFace, Width(2f * Mathf.PI * KwGearTrain.Bevel / KwGearTrain.BevelTeeth), KwGearTrain.Tooth);
            }
        }

        /// <summary>A bevel wheel's tooth on its pitch circle, the shafts unturned, and the way it points out from the wheel's axis.</summary>
        public static (Vector3 Pitch, Vector3 Out) BevelTooth(Vector3 apex, Vector3 axis, Vector3 toward, bool lead, int i)
        {
            float half = Mathf.Acos(Mathf.Clamp(Vector3.Dot(axis, toward), -1f, 1f)) / 2f;
            Vector3 contact = (toward - axis * Vector3.Dot(axis, toward)).normalized;
            Vector3 r = KwGearTrain.Turn(contact, axis, (i + (lead ? 0f : 0.5f)) * 360f / KwGearTrain.BevelTeeth);
            return (apex + axis * (KwGearTrain.Bevel / Mathf.Tan(half)) + r * KwGearTrain.Bevel, r);
        }

        /// <summary>A coupling's half where two stretches meet nearly in line: a flange on the shaft going out a way, bolted.</summary>
        public static void Flange(MeshBuilder wood, MeshBuilder fitting, Vector3 joint, Vector3 axis)
        {
            Solid.Log(wood, joint + axis * 0.004f, joint + axis * 0.03f, 0.07f, 12);
            for (int i = 0; i < 4; i++)
            {
                Vector3 bolt = joint + KwGearTrain.Turn(KwGearTrain.Square(axis), axis, 45f + i * 90f) * 0.05f;
                Solid.Log(fitting, bolt - axis * 0.002f, bolt + axis * 0.04f, 0.009f, 6);
            }
        }

        // A tooth's width at its pitch line, from the pitch between teeth.
        private static float Width(float pitch) => pitch * Fill;

        // A tooth: a ridge standing on its foot's middle, long along its way, tapering to two thirds its width at its top.
        private static void Ridge(MeshBuilder b, (Vector3 At, Vector3 Along, Vector3 Up) tooth, float length, float width, float height)
        {
            Vector3 along = tooth.Along * (length / 2f), up = tooth.Up * height, side = Vector3.Cross(tooth.Up, tooth.Along).normalized;
            Vector3 foot = side * (width / 2f), crown = side * (width / 3f), a = tooth.At;
            Solid.Hexa(b, new[]
            {
                a - along - foot, a + along - foot, a + along + foot, a - along + foot,
                a - along - crown + up, a + along - crown + up, a + along + crown + up, a - along + crown + up,
            });
        }

        // A piece of the quadrant's rim between two angles (degrees from forward toward port), its teeth's band wide.
        private static void Rim(MeshBuilder b, float a0, float a1, float r, float low, float top)
        {
            Vector3 i0 = Ray(a0, r - KwGearTrain.Band), o0 = Ray(a0, r + KwGearTrain.Band), i1 = Ray(a1, r - KwGearTrain.Band), o1 = Ray(a1, r + KwGearTrain.Band);
            Vector3 dz = new Vector3(0f, 0f, top - low), z0 = new Vector3(0f, 0f, low);
            Solid.Hexa(b, new[] { i0 + z0, o0 + z0, o1 + z0, i1 + z0, i0 + z0 + dz, o0 + z0 + dz, o1 + z0 + dz, i1 + z0 + dz });
        }

        // An arm of the quadrant from its hub out to its rim at an angle.
        private static void Arm(MeshBuilder b, float a, float from, float to, float low, float top)
        {
            Vector3 way = Ray(a, 1f), side = Vector3.Cross(Up, way) * 0.026f, z0 = new Vector3(0f, 0f, low), dz = new Vector3(0f, 0f, top - low);
            Vector3 p = way * from, q = way * to;
            Solid.Hexa(b, new[] { p - side + z0, q - side + z0, q + side + z0, p + side + z0, p - side + z0 + dz, q - side + z0 + dz, q + side + z0 + dz, p + side + z0 + dz });
        }

        private static Vector3 Ray(float degrees, float r) => new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad), 0f) * r;
    }
}
