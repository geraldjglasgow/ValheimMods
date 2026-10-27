using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A mast's ropes drawn as the game's own ships draw theirs: line renderers in the game's rope (<c>cart_rope</c>, from the
    /// longship's mast rope), tiled along their length, through the points <see cref="KwRigRopes"/> lays them on: the fixed
    /// ones under one "ropes" child of the piece, with the fittings their feet are made fast to (<see cref="KwTieLook"/>:
    /// deadeyes on chainplates, cleats, eyebolts), the ones on what moves following each sail as it is raised and lowered
    /// (<see cref="Follow"/>), the bolt round each sail's cloth a child of the cloth. Where they tie on to the ship is worked
    /// out from the ship's own shape (<see cref="KwRigState"/>, <see cref="KwRigTies"/>). Kit axes as <see cref="KwSailLayout"/>.
    /// </summary>
    public static class KwSailRigging
    {
        public const string RopesName = "ropes", BoltName = "boltrope";
        internal const float Thin = 0.028f, Stout = 0.04f;

        /// <summary>The fixed ropes of every sail on the mast under a "ropes" child of the piece, tied on to the ship as it is now.</summary>
        public static void Rig(Transform piece, List<KwSailLayout> sails, Material? rope)
        {
            Transform ropes = Child(piece, RopesName);
            KwRigPlan plan = KwRigState.Fresh(piece, sails);
            foreach (KwRigRope r in KwRigRopes.Fixed(plan, sails))
                Line(ropes, r.Name, rope, r.Width, r.Points);
            for (int i = 0; i < sails.Count; i++)
                KwRigRopes.Moving(plan, sails, i, 1f);
            KwTieLook.Draw(ropes, plan.Ties);
        }

        /// <summary>
        /// The ropes on what moves of sail i, at hoist f (<see cref="KwRigRopes.Moving"/>): a square sail's sheets, a sail
        /// along the ship's halyard and a gaff's peak halyard; made the first time, their points set again after.
        /// </summary>
        public static void Follow(Transform piece, List<KwSailLayout> sails, int i, float f, Material? rope)
        {
            foreach (KwRigRope r in KwRigRopes.Moving(KwRigState.Of(piece, sails), sails, i, f))
                Keep(piece, r.Name, rope, r.Points);
        }

        // A moving rope under the piece: made the first time, its points set again after.
        private static void Keep(Transform piece, string name, Material? rope, List<Vector3> kit)
        {
            Transform? line = piece.Find(name);
            if (line == null)
                Line(piece, name, rope, Thin, kit);
            else
                Set(line.GetComponent<LineRenderer>(), kit);
        }

        /// <summary>
        /// The bolt rope round the cloth's free edges at hoist f, under the cloth: made, or its points set again; none on a
        /// worn cloth, whose free edges are torn (<see cref="KwSailWear"/>), so no rope runs past the cloth.
        /// </summary>
        public static void Bolt(Transform cloth, KwSailLayout s, float f, Material? rope)
        {
            Transform? bolt = cloth.Find(BoltName);
            if (KwSailWear.Worn(s.Spec.Cloth))
            {
                if (bolt != null)
                    Object.Destroy(bolt.gameObject);
                return;
            }
            if (bolt == null)
                Line(cloth, BoltName, rope, 0.022f, KwSailCanvas.Edge(s, f));
            else
                Set(bolt.GetComponent<LineRenderer>(), KwSailCanvas.Edge(s, f));
        }

        /// <summary>A rope from a to c sagging by <see cref="SagOf"/> its length at its middle.</summary>
        internal static List<Vector3> Sag(Vector3 a, Vector3 c)
        {
            List<Vector3> points = new List<Vector3>();
            float sag = SagOf((c - a).magnitude);
            for (int i = 0; i <= 6; i++)
                points.Add(Hanging(a, c, i / 6f, sag));
            return points;
        }

        /// <summary>How far a rope of a length sags at its middle: a fiftieth of it.</summary>
        internal static float SagOf(float length) => length * 0.02f;

        /// <summary>A point a share t of the way from a to c along a rope sagging that much at its middle.</summary>
        internal static Vector3 Hanging(Vector3 a, Vector3 c, float t, float sag) => Vector3.Lerp(a, c, t) - new Vector3(0f, 0f, sag * 4f * t * (1f - t));

        private static Transform Child(Transform parent, string name)
        {
            Transform? old = parent.Find(name);
            if (old != null)
            {
                old.name = name + "_gone";
                Object.Destroy(old.gameObject);
            }
            Transform made = new GameObject(name) { layer = parent.gameObject.layer }.transform;
            made.SetParent(parent, false);
            return made;
        }

        /// <summary>A rope: a line renderer in the parent's own space through kit points.</summary>
        internal static GameObject Line(Transform parent, string name, Material? rope, float width, List<Vector3> kit)
        {
            GameObject go = new GameObject(name) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.widthMultiplier = width;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (rope != null)
                line.sharedMaterial = rope;
            Set(line, kit);
            return go;
        }

        internal static void Set(LineRenderer line, List<Vector3> kit)
        {
            line.positionCount = kit.Count;
            for (int i = 0; i < kit.Count; i++)
                line.SetPosition(i, KwMesh.ToUnity(kit[i]));
        }
    }
}
