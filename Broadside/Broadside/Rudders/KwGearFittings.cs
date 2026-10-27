using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The steering gear's plain parts (<see cref="KwGearTrain"/>), in kit axes (x forward, y left, z up, metres): the line
    /// shaft, a pole of the rudder's wood; the hangers holding it under the deck, each an upright block from a plate on the
    /// deck's underside down to a bearing ring round the shaft; the bearings where the shaft goes through the stern post,
    /// a plate on each of its faces; the bush where the short shaft goes up through the deck into the wheel's stand; and,
    /// where the shaft climbs a cabin's wall to a wheel on its roof, the brackets holding it to the wall (each a block from
    /// a plate on the wall's face out to a bearing ring round the shaft) and a collar where it comes up through a floor.
    /// </summary>
    public static class KwGearFittings
    {
        private const float Block = 0.035f, Ring = 0.012f;

        /// <summary>Half the size of a bracket's plate on its wall.</summary>
        public const float BracketPlate = 0.065f;
        private static readonly Vector3 Up = new Vector3(0f, 0f, 1f);

        /// <summary>A stretch of shaft from a to c, banded at both ends.</summary>
        public static void Shaft(MeshBuilder wood, MeshBuilder fitting, Vector3 a, Vector3 c)
        {
            Solid.Log(wood, a, c, KwGearTrain.Shaft, 10);
            Vector3 way = (c - a).normalized;
            float length = (c - a).magnitude;
            if (length < 0.2f)
                return;
            foreach (Vector3 band in new[] { a + way * 0.05f, c - way * 0.05f })
                KwRound.Ring(fitting, band, way, KwGearTrain.Shaft + 0.002f, 0.006f, 10, 3);
        }

        /// <summary>A hanger: a point on the shaft (running a way) under a deck whose top is given, held from the deck's underside.</summary>
        public static void Hanger(MeshBuilder wood, MeshBuilder fitting, Vector3 at, Vector3 way, float top)
        {
            float under = top - KwPlanks.Thick, foot = at.z + KwGearTrain.Shaft + Ring;
            Vector3 side = Vector3.Cross(Up, way).normalized * Block, along = way * (Block * 0.8f);
            Vector3 c = new Vector3(at.x, at.y, 0f);
            Solid.Hexa(wood, new[]
            {
                c - side - along + Up * foot, c - side + along + Up * foot, c + side + along + Up * foot, c + side - along + Up * foot,
                c - side - along + Up * under, c - side + along + Up * under, c + side + along + Up * under, c + side - along + Up * under,
            });
            KwRound.Ring(fitting, at, way, KwGearTrain.Shaft + Ring, Ring, 12, 4);
            Plate(fitting, new Vector3(at.x, at.y, under), -Up, 0.075f);
            foreach (float s in new[] { 1f, -1f })
                Solid.Log(fitting, at + side.normalized * s * (KwGearTrain.Shaft + Ring), new Vector3(at.x, at.y, foot + 0.04f) + side.normalized * s * (Block + 0.004f), 0.006f, 5);
        }

        /// <summary>The bearings where the shaft goes through the stern post: a plate round it on each of the post's faces.</summary>
        public static void PostBearings(MeshBuilder fitting, KwRudderKit k)
        {
            Vector3 at = KwGearTrain.PinionAt(k.Tier, k.Fit, k.Lay);
            foreach (float x in new[] { -0.004f, k.Fit.PostDepth + 0.004f })
            {
                Vector3 face = new Vector3(x, at.y, at.z), outward = Vector3.right * Mathf.Sign(x);
                Plate(fitting, face, outward, 0.065f);
                KwRound.Ring(fitting, face + outward * 0.012f, Vector3.right, KwGearTrain.Shaft + Ring, Ring, 12, 4);
            }
        }

        /// <summary>The bush under the deck where the short shaft goes up into the wheel's stand: a plate and a bearing ring.</summary>
        public static void Bush(MeshBuilder fitting, Vector3 under, float top)
        {
            Vector3 at = new Vector3(under.x, under.y, top - KwPlanks.Thick);
            Plate(fitting, at, -Up, 0.08f);
            KwRound.Ring(fitting, at - Up * 0.025f, Up, KwGearTrain.Shaft + Ring, Ring, 12, 4);
        }

        /// <summary>A bracket: a point on an upright shaft held to a wall a level reach away (from the shaft's axis to the wall's face), its plate flat on the face.</summary>
        public static void Bracket(MeshBuilder wood, MeshBuilder fitting, Vector3 at, Vector3 reach)
        {
            Vector3 way = reach.normalized, side = Vector3.Cross(Up, way) * Block, up = Up * Block;
            Vector3 near = at + way * (KwGearTrain.Shaft + Ring), far = at + reach;
            Solid.Hexa(wood, new[]
            {
                near - side - up, far - side - up, far + side - up, near + side - up,
                near - side + up, far - side + up, far + side + up, near + side + up,
            });
            KwRound.Ring(fitting, at, Up, KwGearTrain.Shaft + Ring, Ring, 12, 4);
            Plate(fitting, far - way * 0.006f, -way, BracketPlate);
        }

        /// <summary>A collar on a floor's top where the shaft comes up through it: a plate and a bearing ring over it.</summary>
        public static void Collar(MeshBuilder fitting, Vector3 top)
        {
            Plate(fitting, top, Up, 0.07f);
            KwRound.Ring(fitting, top + Up * 0.025f, Up, KwGearTrain.Shaft + Ring, Ring, 12, 4);
        }

        // A square iron plate, half a size across, lying flat against a face (its normal out of that face), 12 mm thick, its
        // four bolts' heads standing out of it.
        private static void Plate(MeshBuilder fitting, Vector3 at, Vector3 normal, float half)
        {
            Vector3 u = KwGearTrain.Square(normal) * half, v = Vector3.Cross(normal, u.normalized) * half, n = normal * 0.006f;
            Solid.Hexa(fitting, new[] { at - u - v - n, at + u - v - n, at + u + v - n, at - u + v - n, at - u - v + n, at + u - v + n, at + u + v + n, at - u + v + n });
            foreach (float a in new[] { 1f, -1f })
            {
                foreach (float b in new[] { 1f, -1f })
                    Solid.Log(fitting, at + (u * a + v * b) * 0.75f + n, at + (u * a + v * b) * 0.75f + n * 2.4f, 0.008f, 6);
            }
        }
    }
}
