using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A mast's shapes from its sails' layouts (<see cref="KwSailLayout"/>). The rig, in the plank wood: a step block, a
    /// mast that tapers to a masthead cap, and each sail's spars: for each square sail a yard across the ship that tapers
    /// to its ends, held to the mast by a parrel ring, the highest one's halyard over the masthead block (a pulley); for a
    /// sail along the ship a boom (and a gaff rising aft for a gaff sail, its own part, <see cref="GaffSpar"/>, as it comes
    /// down with the sail) with wooden hoops round the mast that hold the sail's leading edge, and a block for its
    /// halyard; and what else the mast carries, its crow's nest, fighting top, climbing pegs and pennant staff
    /// (<see cref="KwMastTopShapes"/>). The lashings, in the rope: rings round each yard and boom that bind the sails' edges
    /// to them. The cloth is <see cref="KwSailCanvas"/>'s.
    /// </summary>
    public static class KwSailShapes
    {
        public static KwDef Rig(KwMastSpec mast) => new KwDef(KwSailTool.Made, "Mast and sail", "sail") { Wood = true, Shape = b => RigShape(b, mast) };

        // The step, the mast and its cap, then each sail's spars, and the blocks for the highest square sail's halyard and
        // the sail along the ship's; then its platforms, pegs and staff.
        private static void RigShape(MeshBuilder b, KwMastSpec mast)
        {
            float r = KwSailLayout.RadiusOf(mast.Mast), top = mast.Mast;
            Solid.Box(b, new Vector3(-r - 0.12f, -r - 0.12f, 0f), new Vector3(r + 0.12f, r + 0.12f, 0.15f));
            KwRound.Taper(b, new Vector3(0f, 0f, -0.1f), new Vector3(0f, 0f, top), r, r * 0.72f);
            KwRound.Taper(b, new Vector3(0f, 0f, top), new Vector3(0f, 0f, top + 0.07f), r * 0.95f, r * 0.95f);
            List<KwSailLayout> sails = KwSailLayout.Of(mast);
            for (int i = 0; i < sails.Count; i++)
            {
                KwSailLayout s = sails[i];
                if (s.Square)
                    Yard(b, s);
                else
                    Booms(b, s);
                if (!s.Square || i == mast.Squares - 1)
                    Block(b, s.Block);
            }
            KwMastTopShapes.Shape(b, mast);
        }

        // A yard across the ship, thickest at the mast, and the parrel ring that holds it to the mast.
        private static void Yard(MeshBuilder b, KwSailLayout s)
        {
            float half = s.Width / 2f + 0.25f;
            Vector3 mid = new Vector3(s.YardX, 0f, s.At);
            KwRound.Taper(b, mid, mid + new Vector3(0f, half, 0f), s.Spar, s.Spar * 0.55f);
            KwRound.Taper(b, mid, mid - new Vector3(0f, half, 0f), s.Spar, s.Spar * 0.55f);
            KwRound.Ring(b, new Vector3(s.Spar * 0.5f, 0f, s.At), Vector3.forward, s.Radius + s.Spar * 0.6f, 0.035f);
        }

        /// <summary>Whether a mast's booms are left out of its rig's mesh and its lashings: a launched ship's, whose sails along the ship swing with their booms (<see cref="KwBoomSwing"/>).</summary>
        internal static bool LooseBooms;

        /// <summary>A sail along the ship's boom alone, or the lacing on it, about the mast's axis: a launched ship's swinging part (<see cref="KwBoomSwing"/>).</summary>
        public static Mesh Boom(KwSailLayout s, bool lacing)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            if (lacing)
                Lace(b, s);
            else
                Spar(b, s);
            return b.ToMesh(KwSailTool.Made + (lacing ? "_lacing" : "_boom"));
        }

        // The boom aft of the mast.
        private static void Spar(MeshBuilder b, KwSailLayout s) =>
            KwRound.Taper(b, new Vector3(-s.Radius, 0f, s.At), new Vector3(-s.Width - 0.2f, 0f, s.At), s.Spar, s.Spar * 0.7f);

        // The boom aft of the mast (unless it swings apart), and the hoops up the mast that the sail's leading edge is bound to.
        private static void Booms(MeshBuilder b, KwSailLayout s)
        {
            if (!LooseBooms)
                Spar(b, s);
            for (float z = s.At + 0.35f; z < s.At + Luff(s) - 0.05f; z += 0.6f)
                KwRound.Ring(b, new Vector3(0f, 0f, z), Vector3.forward, s.Radius + 0.05f, 0.03f);
        }

        /// <summary>How high the sail's leading edge runs up the mast above the boom.</summary>
        public static float Luff(KwSailLayout s) => s.Spec.Shape == 2 ? s.Height * 0.75f : s.Height;

        // A block: a wooden shell round a sheave, its sheave turning about the across axis.
        private static void Block(MeshBuilder b, Vector3 at)
        {
            Solid.Box(b, at - new Vector3(0.04f, 0.035f, 0.08f), at + new Vector3(0.04f, 0.035f, 0.08f));
            KwRound.Taper(b, at - new Vector3(0f, 0.045f, 0f), at + new Vector3(0f, 0.045f, 0f), 0.05f, 0.05f, 10);
        }

        /// <summary>The rope lashings of every sail on the mast in one mesh: rings binding each sail's head to its yard, or its foot to its boom.</summary>
        public static Mesh Lashings(List<KwSailLayout> sails)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            foreach (KwSailLayout s in sails)
            {
                if (s.Square)
                    Bind(b, s);
                else if (!LooseBooms)
                    Lace(b, s);
            }
            return b.ToMesh(KwSailTool.Made + "_lashings");
        }

        // Rings round the yard about every 45 cm across the sail.
        private static void Bind(MeshBuilder b, KwSailLayout s)
        {
            int count = Mathf.Max(3, Mathf.RoundToInt(s.Width / 0.45f));
            for (int i = 0; i < count; i++)
            {
                float y = -s.Width / 2f + (i + 0.5f) * s.Width / count;
                KwRound.Ring(b, new Vector3(s.YardX, y, s.At - s.Spar * 0.35f), Vector3.left, s.Spar + 0.035f, 0.013f, 8, 4);
            }
        }

        // Rings along the boom every half metre.
        private static void Lace(MeshBuilder b, KwSailLayout s)
        {
            for (float x = -0.45f; x > -s.Width; x -= 0.5f)
                KwRound.Ring(b, new Vector3(x, 0f, s.At + s.Spar * 0.35f), Vector3.right, s.Spar + 0.03f, 0.013f, 8, 4);
        }

        /// <summary>A gaff, a metre long along its own axis, stretched and set from the sail's throat to its peak as it is hoisted.</summary>
        public static Mesh GaffSpar(KwSailLayout s)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            KwRound.Taper(b, Vector3.zero, new Vector3(1f, 0f, 0f), s.Spar * 0.9f, s.Spar * 0.6f);
            return b.ToMesh(KwSailTool.Made + "_gaff");
        }

        /// <summary>
        /// The rig and the lashings in one mesh, for the see-through preview; the sails' cloths are shown beside it, each in
        /// its own look (<see cref="KwSailPreview"/>).
        /// </summary>
        public static Mesh Whole(KwMastSpec mast)
        {
            List<KwSailLayout> sails = KwSailLayout.Of(mast);
            List<CombineInstance> parts = new List<CombineInstance>
            {
                new CombineInstance { mesh = KwMesh.Build(Rig(mast)), transform = Matrix4x4.identity },
                new CombineInstance { mesh = Lashings(sails), transform = Matrix4x4.identity },
            };
            Mesh whole = new Mesh { name = KwSailTool.Made + "_whole", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            whole.CombineMeshes(parts.ToArray());
            foreach (CombineInstance part in parts)
                Object.Destroy(part.mesh);
            return whole;
        }
    }
}
