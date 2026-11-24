using System;
using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The capsules a seer's robe cloth (<see cref="RobeCloth"/>) is pushed by: one round each thigh, shin and foot of both
    /// legs, on the body's bones, sized from what the wearer has on (<see cref="RobeClothWorn"/>): its line through the
    /// middle of the worn shape (the box round it across the bone, in each half of the limb) and a round at each end (a
    /// tapered capsule) that holds the skirt's outside <see cref="Margin"/> off the bulkiest of the body, trousers and boots
    /// (the cloth keeps a particle's middle a collider's round plus the particle's own off its line). A limb with nothing
    /// worn on it keeps its own round on the bone line. Never so round that it pushes the skirt off its own shape at rest:
    /// each round is cut to the nearest moving vertex beside it, less the particle and a gap, to no less than 60% (worked out
    /// per build). Made on the bones; the caller destroys them (they are listed in the list it passes).
    /// </summary>
    internal static class RobeClothColliders
    {
        private sealed class Limb
        {
            public string From, To;
            public float Radius, Before, After;
        }

        /// <summary>A capsule in rest metres: its two rounds' middles and their rounds.</summary>
        private struct Shape
        {
            public Vector3 Start, End;
            public float Near, Far;
        }

        // Metres: a thigh from the hip joint to the knee, a shin from the knee to the ankle (a little into the foot), a foot
        // from behind the heel to past the toes (the toe bone is the ball of the foot); the round without anything worn.
        private static readonly Limb[] Limbs =
        {
            new Limb { From = "LeftUpLeg", To = "LeftLeg", Radius = 0.085f, Before = 0.03f, After = 0.02f },
            new Limb { From = "LeftLeg", To = "LeftFoot", Radius = 0.06f, Before = 0f, After = 0.02f },
            new Limb { From = "LeftFoot", To = "LeftToeBase", Radius = 0.05f, Before = 0.05f, After = 0.06f },
            new Limb { From = "RightUpLeg", To = "RightLeg", Radius = 0.085f, Before = 0.03f, After = 0.02f },
            new Limb { From = "RightLeg", To = "RightFoot", Radius = 0.06f, Before = 0f, After = 0.02f },
            new Limb { From = "RightFoot", To = "RightToeBase", Radius = 0.05f, Before = 0.05f, After = 0.06f },
        };

        /// <summary>Metres the skirt's outside is held off what is worn.</summary>
        private const float Margin = 0.01f;

        private const float Least = 0.02f;
        private const float Gap = 0.005f;
        private const float Kept = 0.6f;

        private static readonly HashSet<Mesh> logged = new HashSet<Mesh>();

        /// <summary>The six capsules for this robe's skirt on its body's bones; each holder is added to <paramref name="made"/>.</summary>
        public static List<ColliderComponent> Make(SkinnedMeshRenderer robe, Mesh skirt, VertexAttribute[] attributes, RobeClothWorn worn, List<GameObject> made)
        {
            Matrix4x4[] binds = skirt.bindposes;
            Transform[] bones = robe.bones;
            List<Vector3> movers = Movers(skirt, attributes);
            var colliders = new List<ColliderComponent>(Limbs.Length);
            var shapes = new Shape[Limbs.Length];
            for (int i = 0; i < Limbs.Length; i++)
            {
                int from = Bone(bones, binds, Limbs[i].From), to = Bone(bones, binds, Limbs[i].To);
                if (from < 0 || to < 0)
                    continue;
                Shape fitted = Fit(Limbs[i], Rest(binds, from), Rest(binds, to), worn.Points[i], worn.Extra);
                shapes[i] = Clear(fitted, movers);
                colliders.Add(Capsule(bones[from], binds[from], shapes[i], made));
            }
            Log(skirt, shapes);
            return colliders;
        }

        /// <summary>The limb's capsule round what is worn on it, or its own round on the bone line with nothing worn.</summary>
        private static Shape Fit(Limb limb, Vector3 a, Vector3 b, List<Vector3> points, float extra)
        {
            Vector3 along = (b - a).normalized;
            float length = (b - a).magnitude;
            Vector3 u = Vector3.Cross(along, Mathf.Abs(along.x) < 0.9f ? Vector3.right : Vector3.forward).normalized;
            Vector3 w = Vector3.Cross(along, u);
            Vector4 near = Box(points, a, along, u, w, -limb.Before, length * 0.5f);
            Vector4 far = Box(points, a, along, u, w, length * 0.5f, length + limb.After);
            if (near.w <= 0f && far.w <= 0f)
                return Round(limb, a - along * limb.Before, b + along * limb.After);
            near = near.w > 0f ? near : far;
            far = far.w > 0f ? far : near;
            Shape shape = Cover(new Shape
            {
                Start = a + along * (near.w - limb.Before) + u * near.x + w * near.y,
                End = a + along * (length + limb.After - far.w) + u * far.x + w * far.y,
                Near = near.w,
                Far = far.w,
            }, points);
            float grow = Margin + extra - RobeClothBuild.Particle;
            shape.Near = Mathf.Max(shape.Near + grow, Least);
            shape.Far = Mathf.Max(shape.Far + grow, Least);
            return shape;
        }

        private static Shape Round(Limb limb, Vector3 start, Vector3 end)
        {
            Vector3 along = (end - start).normalized;
            return new Shape { Start = start + along * limb.Radius, End = end - along * limb.Radius, Near = limb.Radius, Far = limb.Radius };
        }

        /// <summary>
        /// x, y: the middle across the bone (on <paramref name="u"/> and <paramref name="w"/>) of the points from
        /// <paramref name="lo"/> to <paramref name="hi"/> along it; w: their round about that middle; zero without points.
        /// </summary>
        private static Vector4 Box(List<Vector3> points, Vector3 a, Vector3 along, Vector3 u, Vector3 w, float lo, float hi)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 p in points)
            {
                float t = Vector3.Dot(p - a, along);
                if (t < lo || t > hi)
                    continue;
                var across = new Vector2(Vector3.Dot(p - a, u), Vector3.Dot(p - a, w));
                min = Vector2.Min(min, across);
                max = Vector2.Max(max, across);
            }
            if (min.x > max.x)
                return Vector4.zero;
            Vector2 middle = (min + max) / 2f;
            return new Vector4(middle.x, middle.y, 0f, Round(points, a, along, u, w, lo, hi, middle));
        }

        private static float Round(List<Vector3> points, Vector3 a, Vector3 along, Vector3 u, Vector3 w, float lo, float hi, Vector2 middle)
        {
            float round = 0f;
            foreach (Vector3 p in points)
            {
                float t = Vector3.Dot(p - a, along);
                if (t >= lo && t <= hi)
                    round = Mathf.Max(round, (new Vector2(Vector3.Dot(p - a, u), Vector3.Dot(p - a, w)) - middle).magnitude);
            }
            return round;
        }

        /// <summary>Both rounds grown by the most any point stands outside the capsule.</summary>
        private static Shape Cover(Shape shape, List<Vector3> points)
        {
            float worst = 0f;
            Vector3 d = shape.End - shape.Start;
            foreach (Vector3 p in points)
            {
                float t = Mathf.Clamp01(Vector3.Dot(p - shape.Start, d) / Mathf.Max(d.sqrMagnitude, 1e-8f));
                worst = Mathf.Max(worst, Vector3.Distance(p, shape.Start + d * t) - Mathf.Lerp(shape.Near, shape.Far, t));
            }
            shape.Near += worst;
            shape.Far += worst;
            return shape;
        }

        /// <summary>Each round cut to the nearest moving vertex beside its half (less the particle and a gap), to no less than 60%.</summary>
        private static Shape Clear(Shape shape, List<Vector3> movers)
        {
            Vector3 d = shape.End - shape.Start;
            float near = shape.Near, far = shape.Far;
            foreach (Vector3 p in movers)
            {
                float t = Vector3.Dot(p - shape.Start, d) / Mathf.Max(d.sqrMagnitude, 1e-8f);
                if (t < 0f || t > 1f)
                    continue;
                float room = Vector3.Distance(p, shape.Start + d * t) - RobeClothBuild.Particle - Gap;
                if (t < 0.5f)
                    near = Mathf.Min(near, room);
                else
                    far = Mathf.Min(far, room);
            }
            shape.Near = Mathf.Max(near, shape.Near * Kept);
            shape.Far = Mathf.Max(far, shape.Far * Kept);
            return shape;
        }

        /// <summary>A capsule on the limb's bone: its +Y end the Start round, its length end to end over both rounds (MagicaCloth's).</summary>
        private static ColliderComponent Capsule(Transform bone, Matrix4x4 bind, Shape shape, List<GameObject> made)
        {
            Vector3 dir = (shape.Start - shape.End).normalized;
            Vector3 top = RobeClothFixed.FromRest(shape.Start + dir * shape.Near), bottom = RobeClothFixed.FromRest(shape.End - dir * shape.Far);
            var holder = new GameObject("EE_RobeClothLeg");
            holder.transform.SetParent(bone, false);
            holder.transform.localPosition = bind.MultiplyPoint3x4((top + bottom) / 2f);
            holder.transform.localRotation = Quaternion.FromToRotation(Vector3.up, bind.MultiplyVector(top - bottom).normalized);
            holder.transform.localScale = Vector3.one;
            made.Add(holder);
            var capsule = holder.AddComponent<MagicaCapsuleCollider>();
            capsule.direction = MagicaCapsuleCollider.Direction.Y;
            float scale = holder.transform.lossyScale.x, length = (top - bottom).magnitude * RobeClothFixed.Scale;
            capsule.SetSize(shape.Near / scale, shape.Far / scale, length / scale);
            capsule.UpdateParameters();
            return capsule;
        }

        private static void Log(Mesh skirt, Shape[] shapes)
        {
            if (!logged.Add(skirt))
                return;
            Plugin.Log.LogInfo($"EliteEquipment: {skirt.name} cloth's leg colliders (cm, top/bottom round): thighs " +
                $"{shapes[0].Near * 100f:0.#}/{shapes[0].Far * 100f:0.#}, shins {shapes[1].Near * 100f:0.#}/{shapes[1].Far * 100f:0.#}, " +
                $"feet {shapes[2].Near * 100f:0.#}/{shapes[2].Far * 100f:0.#} (left leg)");
        }

        private static List<Vector3> Movers(Mesh mesh, VertexAttribute[] attributes)
        {
            Vector3[] vertices = mesh.vertices;
            var movers = new List<Vector3>();
            for (int i = 0; i < vertices.Length && i < attributes.Length; i++)
            {
                if (attributes[i].IsMove())
                    movers.Add(RobeClothFixed.ToRest(vertices[i]));
            }
            return movers;
        }

        private static Vector3 Rest(Matrix4x4[] binds, int bone) => RobeClothFixed.ToRest(binds[bone].inverse.MultiplyPoint3x4(Vector3.zero));

        private static int Bone(Transform[] bones, Matrix4x4[] binds, string name)
        {
            int index = Array.FindIndex(bones, bone => bone != null && bone.name == name);
            return index < binds.Length ? index : -1;
        }
    }
}
