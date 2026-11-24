using System;
using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Which of a seer's robe's vertices its cloth (<see cref="RobeCloth"/>) moves, worked out once per robe mesh from the
    /// bind pose (metres above the feet at rest; the mesh's z is up, 95 metres a unit as the player's armature): below the
    /// hips line (<see cref="Line"/>) everything the legs carry moves (the skirt and what hangs from it: its lining,
    /// over-robes, fur rolls, lobe bands and tabs); a band above the line is held fixed on the body and the cloth hangs from
    /// it; the rest (the bodice, sleeves, mantle, collar, belt and sash, anything the arms or head carry, and any charm or
    /// medallion below the line the legs do not carry) is left out of the cloth and skinned as before. A moving vertex in a
    /// triangle with a left-out one is held fixed instead, so no triangle links the cloth to a part skinned as before (none in
    /// any set today: no vertex below the line carries any arm, hand or head weight, checked on all 16 meshes). Null for a
    /// mesh with nothing to move or that cannot be read.
    /// </summary>
    internal static class RobeClothFixed
    {
        /// <summary>The hips line, metres: the seat and the top of the thighs (the crotch is at 0.887, the hip joints above).</summary>
        public const float Line = 0.92f;

        /// <summary>Metres per mesh unit in the player's bind pose (the game's armature is scaled 100, the model 0.95).</summary>
        public const float Scale = 95f;

        // The fixed band above the line, the least share of the legs' bones that makes a vertex below the line move, and
        // the share of the arms' or head's bones that leaves a vertex out (a sleeve, a collar).
        private const float Band = 0.10f;
        private const float Carried = 0.05f;
        private const float Upper = 0.02f;

        private static readonly string[] UpperBones = { "Arm", "Hand", "Shoulder", "Neck", "Head", "Jaw" };
        private static readonly string[] LegBones = { "Leg", "Foot", "Toe" };
        private static readonly Dictionary<Mesh, VertexAttribute[]> made = new Dictionary<Mesh, VertexAttribute[]>();

        /// <summary>Each vertex's part in the cloth (fixed, moving or left out) for this robe mesh on these bones.</summary>
        public static VertexAttribute[] Of(SkinnedMeshRenderer robe, Mesh mesh)
        {
            if (!made.TryGetValue(mesh, out VertexAttribute[] attributes))
                made[mesh] = attributes = Work(mesh, robe.bones);
            return attributes;
        }

        /// <summary>A mesh-space point in rest metres, Unity's axes (x to the right, y up, z forward).</summary>
        public static Vector3 ToRest(Vector3 p) => new Vector3(Scale * p.x, Scale * p.z, -Scale * p.y);

        /// <summary>A point in rest metres back in the mesh's space (<see cref="ToRest"/> undone).</summary>
        public static Vector3 FromRest(Vector3 r) => new Vector3(r.x / Scale, -r.z / Scale, r.y / Scale);

        private static VertexAttribute[] Work(Mesh mesh, Transform[] bones)
        {
            if (!mesh.isReadable || mesh.vertexCount == 0 || mesh.boneWeights.Length != mesh.vertexCount)
                return null;
            VertexAttribute[] attributes = Parts(mesh, bones);
            Anchor(mesh.triangles, attributes);
            int moving = 0;
            foreach (VertexAttribute attribute in attributes)
                moving += attribute.IsMove() ? 1 : 0;
            Plugin.Log.LogInfo($"EliteEquipment: {mesh.name} as cloth: {moving} of {attributes.Length} vertices move below {Line} m");
            return moving > 0 ? attributes : null;
        }

        private static VertexAttribute[] Parts(Mesh mesh, Transform[] bones)
        {
            Vector3[] vertices = mesh.vertices;
            BoneWeight[] weights = mesh.boneWeights;
            float[] upper = Flags(bones, UpperBones), legs = Flags(bones, LegBones);
            var attributes = new VertexAttribute[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                attributes[i] = Attribute(Scale * vertices[i].z, Share(weights[i], upper), Share(weights[i], legs));
            return attributes;
        }

        /// <summary>Each moving vertex of a triangle that has a left-out vertex is held fixed instead.</summary>
        private static void Anchor(int[] triangles, VertexAttribute[] attributes)
        {
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                if (!attributes[a].IsInvalid() && !attributes[b].IsInvalid() && !attributes[c].IsInvalid())
                    continue;
                Hold(attributes, a);
                Hold(attributes, b);
                Hold(attributes, c);
            }
        }

        private static void Hold(VertexAttribute[] attributes, int vertex)
        {
            if (attributes[vertex].IsMove())
                attributes[vertex] = VertexAttribute.Fixed;
        }

        private static VertexAttribute Attribute(float height, float upper, float legs)
        {
            if (upper >= Upper || height >= Line + Band)
                return VertexAttribute.Invalid;
            return height < Line && legs >= Carried ? VertexAttribute.Move : VertexAttribute.Fixed;
        }

        /// <summary>1 for each bone whose name has one of <paramref name="parts"/> in it, 0 for the others.</summary>
        private static float[] Flags(Transform[] bones, string[] parts)
        {
            var flags = new float[bones.Length];
            for (int b = 0; b < bones.Length; b++)
            {
                string name = bones[b] != null ? bones[b].name : null;
                flags[b] = name != null && Array.Exists(parts, part => name.Contains(part)) ? 1f : 0f;
            }
            return flags;
        }

        private static float Share(BoneWeight w, float[] flags) =>
            On(flags, w.boneIndex0) * w.weight0 + On(flags, w.boneIndex1) * w.weight1
            + On(flags, w.boneIndex2) * w.weight2 + On(flags, w.boneIndex3) * w.weight3;

        private static float On(float[] flags, int bone) => bone >= 0 && bone < flags.Length ? flags[bone] : 0f;
    }
}
