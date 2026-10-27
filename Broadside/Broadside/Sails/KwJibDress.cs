using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One jib in the world (<see cref="KwJibDress"/>): its cut, its cloth's layout, the cloth and what holds it.</summary>
    public sealed class KwJib
    {
        public KwJib(int index, KwJibCut cut, KwSailLayout layout, Transform cloth, Transform holder, KwJibStay stay)
        {
            (Index, Cut, Layout, Cloth, Holder, Stay) = (index, cut, layout, cloth, holder, stay);
        }

        public int Index { get; }
        public KwJibCut Cut { get; }
        public KwSailLayout Layout { get; }
        public Transform Cloth { get; }
        public Transform Holder { get; }
        public KwJibStay Stay { get; }
    }

    /// <summary>
    /// A bowsprit's jibs made in the world from its plan (<see cref="KwJibPlan"/>), the same on the dock and on a launched
    /// ship, under a "jibs" child of the bowsprit (the old one gone). Each jib is a triangle sail of the sail code's own
    /// (<see cref="KwSailCanvas"/>, <see cref="KwSailDrape"/>), its cloth set into the jib's shape on three nested transforms
    /// (<see cref="KwJibShear"/>), in its cloth's material with the bowsprit's pattern painted on to the jib's own shape
    /// (<see cref="Look"/>), the texture cut to the jib unstretched, as on the other sails (<see cref="KwJibUv"/>), and
    /// alive in the wind (<see cref="KwSailMotion"/>), its bolt rope
    /// round it; a club along its foot (lowered, its cloth lies in flakes on it, as a gaff sail's on its boom); its stay
    /// from the foremast down to its tack; two sheets from the club's end to the ship's sides (<see cref="KwSailAnchor"/>);
    /// and its halyard from its head up the stay and down the mast, which follows it as it is set and furled
    /// (<see cref="Pose"/>). Ropes in the game's rope. Kit axes of the bowsprit.
    /// </summary>
    public static class KwJibDress
    {
        public const string HolderName = "jibs";
        private const float OverClub = 0.045f;

        /// <summary>Makes the jibs, every one fully set; none when the plan has none.</summary>
        public static List<KwJib> Dress(Transform frame, KwBowsprit spec, KwJibStay stay)
        {
            Clear(frame);
            List<KwJib> jibs = new List<KwJib>();
            List<KwJibCut> cuts = KwJibPlan.Of(spec, stay);
            if (cuts.Count == 0)
                return jibs;
            Transform holder = Child(frame, HolderName, Vector3.zero);
            for (int i = 0; i < cuts.Count; i++)
            {
                jibs.Add(Jib(holder, cuts[i], i, spec, stay));
                Ropes(holder, cuts[i], i);
            }
            foreach (KwJib jib in jibs)
                Pose(jib, 1f);
            return jibs;
        }

        /// <summary>The bowsprit's jibs taken away.</summary>
        public static void Clear(Transform frame)
        {
            Transform? old = frame.Find(HolderName);
            if (old == null)
                return;
            old.name = HolderName + "_gone";
            Object.Destroy(old.gameObject);
        }

        /// <summary>A jib set to hoist f: its cloth, its bolt rope and its halyard.</summary>
        public static void Pose(KwJib jib, float f)
        {
            KwSailRigging.Bolt(jib.Cloth, jib.Layout, f, KwSailCloth.Rope);
            KwSailMotion.Pose(jib.Cloth, jib.Layout, f);
            (Vector3 head, _) = KwSailCanvas.Top(jib.Layout, f);
            KwJibCut cut = jib.Cut;
            Vector3 aside = Vector3.Cross(Vector3.forward, cut.Forward) * 0.05f, at = cut.Tack + new Vector3(0f, 0f, OverClub) + cut.StayDir * head.z;
            Vector3 fall = new Vector3(cut.StayTop.x, cut.StayTop.y, jib.Stay.Foot.z + 0.3f);
            Keep(jib.Holder, "halyard" + jib.Index, new List<Vector3> { at + aside, cut.StayTop + aside, fall + aside });
        }

        /// <summary>
        /// A jib's cloth's layout: a triangle sail its foot and luff, in the bowsprit's cloth and pattern, its texture laid on
        /// from its own triangle in it (<see cref="KwJibUv"/>), not stretched over the cloth by the shear.
        /// </summary>
        public static KwSailLayout Layout(KwJibCut cut, KwBowsprit spec, KwJibStay stay) =>
            new KwSailLayout(new KwSailSpec(1, stay.Mast.Mast, cut.Foot, cut.Luff, spec.Cloth, cut.Luff + 2f, 0, spec.Pattern)) { Uv = KwJibUv.Of(Frame(cut)) };

        /// <summary>A jib's material: its cloth, the pattern painted to its own triangle in the texture.</summary>
        public static Material? Look(KwJibCut cut, KwBowsprit spec) => KwSailLooks.Of(spec.Cloth, spec.Pattern, Frame(cut));

        /// <summary>
        /// A jib's frame: its triangle (its foot over its luff, the angle between them) and the metres its texture spans,
        /// where its cloth lies in the texture.
        /// </summary>
        public static KwSailFrame Frame(KwJibCut cut) => KwSailFrame.Of(1, cut.Foot, cut.Luff, (cut.Alpha - cut.Beta) * Mathf.Rad2Deg, cut.Cloth);

        /// <summary>Where a jib's cloth is drawn in the bowsprit's frame, its shear in one matrix (a hammer ghost's, <see cref="KwSailGhost"/>).</summary>
        public static Matrix4x4 ClothAt(KwJibCut cut)
        {
            KwJibShear shear = KwJibShear.Of(cut.Alpha, cut.Beta);
            Matrix4x4 root = Matrix4x4.TRS(KwMesh.ToUnity(cut.Tack + new Vector3(0f, 0f, OverClub)), shear.RootTurn(cut.Yaw), Vector3.one);
            return root * Matrix4x4.Scale(shear.Stretch) * Matrix4x4.Rotate(shear.BackTurn);
        }

        // One jib: its club, and its cloth on a turn, a stretch and a turn back (the shear) from just over the club.
        private static KwJib Jib(Transform holder, KwJibCut cut, int i, KwBowsprit spec, KwJibStay stay)
        {
            KwSailLayout layout = Layout(cut, spec, stay);
            Club(holder, cut, i);
            KwJibShear shear = KwJibShear.Of(cut.Alpha, cut.Beta);
            Transform root = Child(holder, "jib" + i, cut.Tack + new Vector3(0f, 0f, OverClub));
            root.localRotation = shear.RootTurn(cut.Yaw);
            Transform stretch = Child(root, "stretch", Vector3.zero);
            stretch.localScale = shear.Stretch;
            Transform made = Child(stretch, KwSailDress.Cloth(0), Vector3.zero);
            made.localRotation = shear.BackTurn;
            made.gameObject.AddComponent<MeshFilter>().sharedMesh = KwSailCanvas.Cloth(layout, 1f);
            made.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Look(cut, spec);
            made.gameObject.AddComponent<KwMeshOwner>();
            return new KwJib(i, cut, layout, made, holder, stay);
        }

        // A light spar along the jib's foot, from its tack a little past its clew.
        private static void Club(Transform holder, KwJibCut cut, int i)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            KwRound.Taper(b, Vector3.zero, new Vector3(cut.Foot + 0.12f, 0f, 0f), 0.04f, 0.03f, 8);
            Transform club = Child(holder, "club" + i, cut.Tack);
            club.localRotation = Quaternion.LookRotation(KwMesh.ToUnity(cut.FootDir));
            club.gameObject.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("KW_jib_club");
            club.gameObject.AddComponent<MeshRenderer>().sharedMaterial = KwPrefabs.Planks;
            club.gameObject.AddComponent<KwMeshOwner>();
        }

        // The jib's stay, from the mast down to its tack, and its two sheets from the club's end out to the ship's sides.
        private static void Ropes(Transform holder, KwJibCut cut, int i)
        {
            Material? rope = KwSailCloth.Rope;
            KwSailRigging.Line(holder, "stay" + i, rope, KwSailRigging.Stout, KwSailRigging.Sag(cut.StayTop, cut.Tack));
            Vector3 end = cut.Tack + cut.FootDir * (cut.Foot + 0.08f), left = Vector3.Cross(Vector3.forward, cut.Forward);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 aim = end - cut.Forward * 1.2f + left * (side * 3f) + new Vector3(0f, 0f, -4f);
                KwSailRigging.Line(holder, $"sheet{i}_{side:0}", rope, KwSailRigging.Thin, KwSailRigging.Sag(end, KwSailAnchor.On(holder, end, aim)));
            }
        }

        // A moving rope under the holder: made the first time, its points set again after.
        private static void Keep(Transform holder, string name, List<Vector3> kit)
        {
            Transform? line = holder.Find(name);
            if (line == null)
                KwSailRigging.Line(holder, name, KwSailCloth.Rope, KwSailRigging.Thin, kit);
            else
                KwSailRigging.Set(line.GetComponent<LineRenderer>(), kit);
        }

        // An empty child at a kit point, on its parent's layer.
        private static Transform Child(Transform parent, string name, Vector3 kit)
        {
            Transform made = new GameObject(name) { layer = parent.gameObject.layer }.transform;
            made.SetParent(parent, false);
            made.localPosition = KwMesh.ToUnity(kit);
            return made;
        }
    }
}
