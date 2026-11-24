using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// What a seer's robe's wearer has on round the legs, for the skirt's capsules (<see cref="RobeClothColliders"/>): every
    /// skinned renderer on the wearer's body (the body itself, which trousers paint, worn leggings, boots, anything another
    /// mod puts on the legs) in the bind pose, each vertex given to the limb of its heaviest bone (a thigh, shin or foot of
    /// either leg), in rest metres, below the hips line. Measured offline (2026-11-03, the workshop's cloth test) the body's
    /// calves stand up to 14 cm from the shin bone and the seer boots' cuffs 13 cm, where the old capsules were 6 cm round:
    /// a shin, a cuff or a heel then stood up to 2 cm outside the skirt even standing. A mesh the game will not let us read
    /// that reaches below the hips line adds <see cref="Unread"/> to every limb instead. Worked out each time the cloth is
    /// built (once per worn robe), nothing kept.
    /// </summary>
    internal sealed class RobeClothWorn
    {
        /// <summary>Thigh, shin and foot of the left leg, then of the right (the order of <see cref="RobeClothColliders"/>' limbs).</summary>
        public const int Limbs = 6;

        /// <summary>Metres added to every limb for a worn mesh that cannot be read.</summary>
        public const float Unread = 0.015f;

        public readonly List<Vector3>[] Points = new List<Vector3>[Limbs];
        public float Extra;

        private readonly Transform[] robeBones;
        private readonly Matrix4x4[] robeBinds;

        private RobeClothWorn(SkinnedMeshRenderer robe)
        {
            robeBones = robe.bones;
            robeBinds = robe.sharedMesh.bindposes;
            for (int i = 0; i < Limbs; i++)
                Points[i] = new List<Vector3>();
        }

        /// <summary>Everything skinned beside the robe on the wearer's body model, standing on <paramref name="floor"/> (world height).</summary>
        public static RobeClothWorn Of(SkinnedMeshRenderer robe, SkinnedMeshRenderer body, float floor)
        {
            var worn = new RobeClothWorn(robe);
            if (body == null || body.transform.parent == null)
                return worn;
            foreach (SkinnedMeshRenderer r in body.transform.parent.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (r != robe && r.sharedMesh != null && r.name != RobeClothMesh.SkirtName)
                    worn.Add(r, floor);
            }
            return worn;
        }

        private void Add(SkinnedMeshRenderer r, float floor)
        {
            Mesh mesh = r.sharedMesh;
            if (!mesh.isReadable)
            {
                if (r.bounds.min.y < floor + RobeClothFixed.Line)
                    Extra = Unread;
                return;
            }
            Matrix4x4[] toRobe = Maps(r, out int[] limb);
            Vector3[] vertices = mesh.vertices;
            BoneWeight[] weights = mesh.boneWeights;
            for (int i = 0; i < vertices.Length && i < weights.Length; i++)
            {
                int b = Heaviest(weights[i]);
                if (b < 0 || b >= limb.Length || limb[b] < 0)
                    continue;
                Vector3 p = RobeClothFixed.ToRest(toRobe[b].MultiplyPoint3x4(vertices[i]));
                if (p.y < RobeClothFixed.Line)
                    Points[limb[b]].Add(p);
            }
        }

        /// <summary>For each of the renderer's bones on a leg, its limb and the map from its mesh into the robe's (both at bind).</summary>
        private Matrix4x4[] Maps(SkinnedMeshRenderer r, out int[] limb)
        {
            Transform[] bones = r.bones;
            Matrix4x4[] binds = r.sharedMesh.bindposes;
            var map = new Matrix4x4[bones.Length];
            limb = new int[bones.Length];
            for (int b = 0; b < bones.Length; b++)
            {
                int k = b < binds.Length && bones[b] != null ? Array.IndexOf(robeBones, bones[b]) : -1;
                limb[b] = k >= 0 && k < robeBinds.Length ? LimbOf(bones[b].name) : -1;
                if (limb[b] >= 0)
                    map[b] = robeBinds[k].inverse * binds[b];
            }
            return map;
        }

        private static int LimbOf(string bone)
        {
            int side = bone.StartsWith("Left", StringComparison.Ordinal) ? 0 : bone.StartsWith("Right", StringComparison.Ordinal) ? 3 : -1;
            if (side < 0)
                return -1;
            if (bone.Contains("UpLeg"))
                return side;
            if (bone.Contains("Leg"))
                return side + 1;
            return bone.Contains("Foot") || bone.Contains("Toe") ? side + 2 : -1;
        }

        private static int Heaviest(BoneWeight w)
        {
            int bone = w.boneIndex0;
            float most = w.weight0;
            if (w.weight1 > most) { bone = w.boneIndex1; most = w.weight1; }
            if (w.weight2 > most) { bone = w.boneIndex2; most = w.weight2; }
            return w.weight3 > most ? w.boneIndex3 : bone;
        }
    }
}
