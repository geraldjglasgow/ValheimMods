using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A masthead pennant's size and its hoist on the staff over the masthead (<see cref="KwPennant"/>, the staff
    /// <see cref="KwMastTopShapes.StaffRadius"/>): its head <see cref="Head"/> under the staff's top, the foot of its truck,
    /// its edge on the staff's face (3 mm out from it, never inside it), and the rope lashings that bind it there: two
    /// turns of 1.2 cm line round the staff and the hoist's edge, flush with its head, flush with its foot and, on a hoist
    /// 30 cm wide or more, at its middle, in the rigging's rope (<see cref="KwSailCloth.Rope"/>). The lashings are a child
    /// of the pennant, drawn with it on every screen and never sent: rings round the staff, the same whichever way the
    /// cloth streams from it.
    /// </summary>
    public readonly struct KwPennantHoist
    {
        /// <summary>How far under the staff's top (its truck's foot) the hoist's head is.</summary>
        public const float Head = 0.012f;

        private const string LashingsName = "lashings";

        // The cloth's edge out from the staff's face; the line's radius, its clearance over the staff, and the width from
        // which a hoist has a third lashing at its middle.
        private const float Sit = 0.003f, Line = 0.006f, Clear = 0.004f, Wide = 0.3f;

        /// <summary>The pennant of a mast of that height whose staff's top is at <paramref name="staffTop"/> over its foot.</summary>
        public KwPennantHoist(float mast, float staffTop)
        {
            Length = Mathf.Clamp(0.3f * mast, 1.5f, 5f);
            Width = Mathf.Clamp(0.03f * mast + 0.12f, 0.18f, 0.4f);
            Top = staffTop - Head;
            AtHead = KwMastTopShapes.StaffRadius(mast, staffTop, Top) + Sit;
            AtFoot = KwMastTopShapes.StaffRadius(mast, staffTop, Top - Width) + Sit;
        }

        public float Length { get; }

        /// <summary>The hoist's width, down the staff from its head.</summary>
        public float Width { get; }

        /// <summary>The hoist's head's height over the mast's foot, on the staff's axis: the pennant's origin.</summary>
        public float Top { get; }

        /// <summary>How far out from the staff's axis the hoist's edge is at its head and at its foot: on the staff's face.</summary>
        public float AtHead { get; }
        public float AtFoot { get; }

        /// <summary>The first part of its length, from the staff, that runs straight out square to it: a tenth of it, at least the hoist's width.</summary>
        public float Root => Mathf.Max(0.1f * Length, Width);

        /// <summary>The lashings under the pennant (the hoist's head at its origin), in the rigging's rope; none without the game's rope.</summary>
        public void Lash(Transform pennant)
        {
            Material? rope = KwSailCloth.Rope;
            if (rope == null)
                return;
            GameObject made = new GameObject(LashingsName) { layer = pennant.gameObject.layer };
            made.transform.SetParent(pennant, false);
            made.AddComponent<MeshFilter>().sharedMesh = Lashings();
            MeshRenderer renderer = made.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = rope;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            made.AddComponent<KwMeshOwner>();
        }

        /// <summary>Two turns of line round the staff and the hoist's edge at each lashing's depth under the head, close round them.</summary>
        public Mesh Lashings()
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            foreach (float depth in Depths())
            {
                float staff = Mathf.Lerp(AtHead, AtFoot, depth / Width) - Sit;
                foreach (float turn in new[] { -Line, Line })
                    KwRound.Ring(b, new Vector3(0f, 0f, turn - depth), Vector3.forward, staff + Clear + Line, Line, 8, 4);
            }
            return b.ToMesh("KW_pennant_lashings");
        }

        /// <summary>The lashings' middles under the hoist's head: flush with its head and its foot, and at its middle on a wide hoist.</summary>
        public float[] Depths()
        {
            float end = 2f * Line;
            return Width >= Wide ? new[] { end, Width / 2f, Width - end } : new[] { end, Width - end };
        }
    }
}
