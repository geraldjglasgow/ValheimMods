using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The frame a rig hit collider's marks are read in (<see cref="KwRigDamageTakers"/>): its piece's place on the ship, a
    /// swinging boom's turn about its mast, or a rudder's blade; its local axes are the piece's kit axes in Unity's.
    /// </summary>
    public sealed class KwRigHitFrame : MonoBehaviour
    {
        public Transform? Frame;

        /// <summary>The frame a collider's marks are read in, or null for a collider that is not one of the rig's.</summary>
        public static Transform? Of(Collider? collider)
        {
            if (collider == null)
                return null;
            KwRigHitFrame? own = collider.GetComponent<KwRigHitFrame>();
            if (own != null)
                return own.Frame;
            for (Transform? t = collider.transform; t != null; t = t.parent)
            {
                if (t.name == KwRudder.BladeName && t.parent != null && t.parent.name == "rudder")
                    return t;
            }
            return null;
        }
    }

    /// <summary>
    /// The rig's hit colliders as they are made (<see cref="KwRigDamageHits"/>): each on its own object on the "hitbox"
    /// layer (which collides with nothing: players walk through it and the camera's, building's and using's rays pass it;
    /// the game's projectiles and the shot router's trace meet it), tagged for the router (<see cref="KwShipHitRouter.Tag"/>)
    /// and told its frame (<see cref="KwRigHitFrame"/>). A spar is a capsule along it a little thicker than its thickest;
    /// a sail, a cap and a rudder's blade are boxes.
    /// </summary>
    public static class KwRigDamageBoxes
    {
        private const float Margin = 0.03f;
        private static int layer = -1;

        /// <summary>The layer every rig hit collider is on.</summary>
        public static int Layer => layer >= 0 ? layer : layer = LayerMask.NameToLayer("hitbox");

        /// <summary>An empty frame under a parent, placed in the parent's axes.</summary>
        public static Transform Holder(Transform parent, string name, Vector3 at, Quaternion turn)
        {
            Transform made = new GameObject(name) { layer = Layer }.transform;
            made.SetParent(parent, false);
            made.SetLocalPositionAndRotation(at, turn);
            return made;
        }

        /// <summary>A spar's capsule in a frame (the spar in the frame's kit axes), tagged as a kind, piece and part.</summary>
        public static CapsuleCollider Spar(Transform frame, KwRigSpar spar, string kind, int piece, int part)
        {
            Transform at = Holder(frame, "hit_" + kind + part, Vector3.zero, Quaternion.identity);
            CapsuleCollider capsule = at.gameObject.AddComponent<CapsuleCollider>();
            Size(capsule, spar);
            Tagged(at.gameObject, frame, kind, piece, part);
            return capsule;
        }

        /// <summary>A spar's capsule set to the spar again (a gaff moved by its sail's hoist, a stump).</summary>
        public static void Size(CapsuleCollider capsule, KwRigSpar spar)
        {
            Transform at = capsule.transform;
            at.SetLocalPositionAndRotation(KwMesh.ToUnity((spar.A + spar.C) / 2f), Turned(KwMesh.ToUnity(spar.Axis)));
            float r = Mathf.Max(spar.R0, spar.R1, spar.RMid) + Margin;
            (capsule.direction, capsule.radius, capsule.height) = (2, r, spar.Length + 2f * r);
        }

        /// <summary>A turn whose forward is a way (its up kept off that way).</summary>
        public static Quaternion Turned(Vector3 way) => Quaternion.LookRotation(way, Mathf.Abs(way.normalized.y) > 0.9f ? Vector3.forward : Vector3.up);

        /// <summary>A box in a frame, its middle and sizes in the frame's kit axes, tagged as a kind, piece and part.</summary>
        public static BoxCollider Box(Transform frame, Vector3 middle, Vector3 size, string kind, int piece, int part)
        {
            Transform at = Holder(frame, "hit_" + kind + part, Vector3.zero, Quaternion.identity);
            BoxCollider box = at.gameObject.AddComponent<BoxCollider>();
            Place(box, middle, size);
            Tagged(at.gameObject, frame, kind, piece, part);
            return box;
        }

        /// <summary>A box's middle and sizes set again, in its frame's kit axes.</summary>
        public static void Place(BoxCollider box, Vector3 middle, Vector3 size)
        {
            box.center = KwMesh.ToUnity(middle);
            box.size = new Vector3(Mathf.Abs(size.y), Mathf.Abs(size.z), Mathf.Abs(size.x));
        }

        private static void Tagged(GameObject go, Transform frame, string kind, int piece, int part)
        {
            KwShipHitRouter.Tag(go, kind, piece, part);
            go.AddComponent<KwRigHitFrame>().Frame = frame;
        }

        /// <summary>A square sail's box at hoist f, in its mast's kit axes: under its yard, as deep as the cloth hangs, its belly in it.</summary>
        public static (Vector3 Middle, Vector3 Size) Square(KwSailLayout s, float f)
        {
            float deep = Mathf.Max(f * s.Height + 0.6f * s.Bags(f), 0.35f), belly = s.BellyAt(Mathf.Max(f * s.Height, 0.1f)) + 0.2f;
            Vector3 top = s.ClothOrigin;
            return (top + new Vector3(belly / 2f - 0.1f, 0f, -deep / 2f), new Vector3(belly, s.Width, deep));
        }

        /// <summary>A sail along the ship's box at hoist f, in its swing's kit axes: over its boom, aft of the mast, its belly in it.</summary>
        public static (Vector3 Middle, Vector3 Size) Along(KwSailLayout s, float f)
        {
            float high = Mathf.Max(s.Bundle(f) + f * s.Height, 0.3f), belly = s.BellyAt(Mathf.Max(f * s.Height, 0.1f)) * 0.8f + 0.2f;
            Vector3 foot = s.ClothOrigin;
            return (foot + new Vector3(-s.Width / 2f, belly / 2f - 0.1f, high / 2f), new Vector3(s.Width, belly, high));
        }
    }
}

namespace Broadside
{
    /// <summary>
    /// What a rig piece's hit colliders are sized and placed by (<see cref="KwRigHitSet"/>): its working fitting on a drawn
    /// ship (the fitting at the piece's place: a mast's sails, a bowsprit's jibs, a rudder), a post cap's box (its foot, a
    /// carved neck's lean either way, its height) and a fallen spar's stump.
    /// </summary>
    internal static class KwRigHitFind
    {
        /// <summary>The ship's fitting of a name made at a piece's place; null where there is none (a dedicated server).</summary>
        public static Transform? Fitting(Transform? fittings, string name, KwRigPiece piece)
        {
            if (fittings == null)
                return null;
            foreach (Transform child in fittings)
            {
                if (child.name == name && (child.localPosition - piece.Piece.Position).sqrMagnitude < 1e-4f)
                    return child;
            }
            return null;
        }

        /// <summary>
        /// A mast's crow's nest's and fighting top's boxes among the ship's colliders (<see cref="KwMastTopColliders"/>: named
        /// "top", marked with the mast's index), each with how far up its column it is, so a mast shot down takes them with it.
        /// </summary>
        public static List<(Collider Box, float Up)> Tops(Transform? colliders, KwRigPiece mast)
        {
            List<(Collider Box, float Up)> tops = new List<(Collider Box, float Up)>();
            for (int i = 0; colliders != null && i < colliders.childCount; i++)
            {
                Transform child = colliders.GetChild(i);
                Vector3 kit = mast.ToKit(child.localPosition);
                bool its = child.TryGetComponent(out KwMastTopOf of) ? of.Index == mast.Index : new Vector2(kit.x, kit.y).magnitude < 3f && kit.z > 0.5f;
                if (child.name == "top" && its && child.TryGetComponent(out Collider box))
                    tops.Add((box, mast.Main.Along(kit)));
            }
            return tops;
        }

        /// <summary>A post cap's box's middle in its kit axes: half way up it.</summary>
        public static Vector3 CapMiddle(KwRigPiece cap) => new Vector3(0f, 0f, cap.Main.Length / 2f);

        /// <summary>A post cap's box's sizes in its kit axes: its foot, and a quarter of its length fore and aft for a carving's lean.</summary>
        public static Vector3 CapSize(KwRigPiece cap)
        {
            KwCapSpec spec = cap.Cap!.Value;
            return new Vector3(spec.Depth + 0.5f * spec.Length, spec.Width + 0.06f, cap.Main.Length);
        }

        /// <summary>A fallen spar's stump: from its foot to where it broke, at least 0.4 m so the hammer can find it.</summary>
        public static KwRigSpar Stump(KwRigSpar spar, float at)
        {
            float s = Mathf.Clamp(at, 0.4f, spar.Length);
            return new KwRigSpar(spar.Part, spar.A, spar.At(s), spar.R0, spar.Radius(s), 0f, spar.Sides);
        }
    }
}
