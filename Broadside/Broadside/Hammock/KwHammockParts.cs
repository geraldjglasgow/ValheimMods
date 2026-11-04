using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hammock's ends as meshes and colliders, plain geometry in the hammock's frame (kit axes, <see cref="KwHammockSpec"/>).
    /// A post: a round post from its foot to <see cref="Cap"/> over its peg, standing on a sawn foot across the span and a
    /// toe in toward the hammock, braced down to the foot's ends and the toe, and a peg out of it toward its rope point, its
    /// tip turned up to hold the ropes (in the plank wood). A hook: an iron eye bolt, a washer on the surface, a shank
    /// <see cref="Shank"/> out of it and an eye of radius <see cref="Eye"/> at its end, upright (under a ceiling, turned
    /// toward the hammock); the ropes hang from the eye's bottom (<see cref="HookRope"/>). The colliders: solid boxes for each
    /// post, foot and toe (standing on the floor, so the game's support finds it) and round each hook (into its surface, so
    /// the game's support finds what holds it).
    /// </summary>
    public static class KwHammockParts
    {
        /// <summary>A post's radius, its top over its peg, its foot's half length across, its toe's length; the peg's reach to its rope point.</summary>
        public const float PostRadius = 0.065f, Cap = 0.17f, Foot = 0.42f, Toe = 0.45f, PegIn = 0.14f;

        /// <summary>The hook's shank out of the surface, its eye's radius.</summary>
        public const float Shank = 0.053f, Eye = 0.022f;

        private const int Links = 8;
        private static readonly Vector3 Up = new Vector3(0f, 0f, 1f);
        private static readonly float[] Sides = { -1f, 1f };

        /// <summary>Where a hook's ropes hang (the bottom of its eye), in any frame: its point on the surface, the way out of it, up.</summary>
        public static Vector3 HookRope(Vector3 at, Vector3 face, Vector3 up) => at + face * (Shank + Eye) - up * Eye;

        /// <summary>Both ends' posts (none for a hook) in the plank wood.</summary>
        public static void Posts(MeshBuilder b, KwHammockSpec spec)
        {
            foreach (KwHammockMount end in new[] { spec.A, spec.B })
            {
                if (end.Post)
                    Post(b, end);
            }
        }

        /// <summary>Both ends' hooks (none for a post) in iron.</summary>
        public static void Hooks(MeshBuilder b, KwHammockSpec spec)
        {
            foreach (KwHammockMount end in new[] { spec.A, spec.B })
            {
                if (!end.Post)
                    Hook(b, end);
            }
        }

        /// <summary>The level way in from an end: a post's toward its rope point, a hook's toward the hammock's middle.</summary>
        public static Vector3 Inward(KwHammockMount end)
        {
            Vector3 way = end.Rope - end.Base;
            way.z = 0f;
            return end.Post && way.sqrMagnitude > 1e-6f ? way.normalized : new Vector3(end.Base.x > 0f ? -1f : 1f, 0f, 0f);
        }

        /// <summary>One post on its foot and toe, braced, its peg out to its rope point.</summary>
        public static void Post(MeshBuilder b, KwHammockMount end)
        {
            Vector3 foot = end.Base, way = Inward(end), across = new Vector3(-way.y, way.x, 0f);
            Solid.Log(b, foot, foot + Up * (end.Peg + Cap), PostRadius, 8);
            Slab(b, foot + Up * 0.055f, across * Foot, way * 0.07f, Up * 0.055f);
            Slab(b, foot + way * (Toe * 0.5f) + Up * 0.045f, way * (Toe * 0.5f), across * 0.06f, Up * 0.045f);
            foreach (float side in Sides)
                Solid.Log(b, foot + Up * 0.62f, foot + across * (side * (Foot - 0.07f)) + Up * 0.1f, 0.03f, 6);
            Solid.Log(b, foot + Up * 0.5f, foot + way * (Toe - 0.06f) + Up * 0.08f, 0.03f, 6);
            Vector3 peg = foot + Up * end.Peg, tip = new Vector3(end.Rope.x, end.Rope.y, peg.z) + way * 0.025f;
            Solid.Log(b, peg + way * (PostRadius - 0.01f), tip, 0.02f, 6);
            Solid.Log(b, tip - Up * 0.015f, tip + Up * 0.06f, 0.018f, 6);
        }

        /// <summary>One hook: the washer on its surface, the shank out of it, the eye at its end.</summary>
        public static void Hook(MeshBuilder b, KwHammockMount end)
        {
            Vector3 at = end.Base, face = end.Face, ring = Ring(end), centre = at + face * (Shank + Eye);
            Solid.Log(b, at - face * 0.004f, at + face * 0.01f, 0.03f, 8);
            Solid.Log(b, at - face * 0.02f, at + face * Shank, 0.009f, 6);
            for (int i = 0; i < Links; i++)
                Solid.Log(b, centre + Round(face, ring, i) * Eye, centre + Round(face, ring, i + 1) * Eye, 0.007f, 4);
        }

        // The eye's plane: out of the surface and up, or under a ceiling out of it and toward the hammock.
        private static Vector3 Ring(KwHammockMount end)
        {
            Vector3 up = Up - end.Face * Vector3.Dot(Up, end.Face);
            return up.sqrMagnitude > 0.1f ? up.normalized : Inward(end);
        }

        private static Vector3 Round(Vector3 face, Vector3 ring, int i)
        {
            float angle = i * 2f * Mathf.PI / Links;
            return face * Mathf.Cos(angle) + ring * Mathf.Sin(angle);
        }

        // A box about its centre along three half-extent ways (corners as Solid.Box lays them).
        private static void Slab(MeshBuilder b, Vector3 c, Vector3 x, Vector3 y, Vector3 z) => Solid.Hexa(b, new[]
        {
            c - x - y - z, c + x - y - z, c + x + y - z, c - x + y - z,
            c - x - y + z, c + x - y + z, c + x + y + z, c - x + y + z,
        });

        /// <summary>The solid boxes of both ends under the model, on its layer.</summary>
        public static void Boxes(Transform model, KwHammockSpec spec)
        {
            foreach (KwHammockMount end in new[] { spec.A, spec.B })
            {
                if (end.Post)
                    PostBoxes(model, end);
                else
                    Box(model, "col_hook", end.Base + end.Face * 0.035f, Facing(end.Face), new Vector3(0.07f, 0.07f, 0.13f));
            }
        }

        // The post, its foot across and its toe in; the foot's and toe's boxes turned with the toe.
        private static void PostBoxes(Transform model, KwHammockMount end)
        {
            Vector3 foot = end.Base, way = Inward(end);
            float top = end.Peg + Cap;
            Box(model, "col_post", foot + Up * (top * 0.5f), Quaternion.identity, new Vector3(0.13f, top, 0.13f));
            Quaternion turn = Quaternion.LookRotation(KwMesh.ToUnity(way), Vector3.up);
            Box(model, "col_foot", foot + Up * 0.055f, turn, new Vector3(2f * Foot, 0.11f, 0.14f));
            Box(model, "col_toe", foot + way * (Toe * 0.5f) + Up * 0.045f, turn, new Vector3(0.12f, 0.09f, Toe));
        }

        // A turn whose z is the way out of a hook's surface.
        private static Quaternion Facing(Vector3 face)
        {
            Vector3 way = KwMesh.ToUnity(face);
            return Quaternion.LookRotation(way, Mathf.Abs(way.y) > 0.9f ? Vector3.forward : Vector3.up);
        }

        /// <summary>A box collider on its own child: its centre in kit axes, its turn and size in Unity's (the child's own).</summary>
        public static void Box(Transform model, string name, Vector3 centre, Quaternion turn, Vector3 size, int layer = -1)
        {
            GameObject box = new GameObject(name) { layer = layer >= 0 ? layer : model.gameObject.layer };
            box.transform.SetParent(model, false);
            box.transform.SetLocalPositionAndRotation(KwMesh.ToUnity(centre), turn);
            box.AddComponent<BoxCollider>().size = size;
        }
    }
}
