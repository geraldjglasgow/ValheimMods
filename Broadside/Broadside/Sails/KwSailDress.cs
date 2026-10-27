using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A mast's parts made from what its piece saves (<see cref="KwMastSpec"/>), the same on every peer: each sail's cloth
    /// (<see cref="Cloth"/>, its own child, in its cloth's material with its pattern painted on, <see cref="KwSailLooks"/>,
    /// alive in the wind, <see cref="KwSailMotion"/>), a gaff
    /// sail's gaff, the rope lashings of every sail in one mesh, the ropes (<see cref="KwSailRigging"/>), its pennant
    /// (<see cref="KwPennant"/>) and the climb up its pegs (<see cref="KwMastClimb"/>); and each sail
    /// set to a hoist (<see cref="Pose"/>): its cloth, its bolt rope, its gaff and the ropes on them. On a placed mast
    /// (<see cref="KwSailRig"/>).
    /// </summary>
    public static class KwSailDress
    {
        public const string ClothName = "cloth", LashingsName = "lashings", GaffName = "gaff";

        /// <summary>A sail's cloth's name on its mast: "cloth0" for the first sail, "cloth1" for the next.</summary>
        public static string Cloth(int sail) => ClothName + sail;

        private static string Gaff(int sail) => GaffName + sail;

        /// <summary>Makes or remakes a mast's cloths, lashings and ropes from what its piece saves, every sail fully set; the sails' layouts, or null when it saves nothing readable.</summary>
        public static List<KwSailLayout>? Dress(Transform piece, string saved)
        {
            if (!KwMastSpec.TryParse(saved, out KwMastSpec mast))
                return null;
            List<KwSailLayout> sails = KwSailLayout.Of(mast);
            Material? rope = KwSailCloth.Rope;
            for (int i = 0; i < sails.Count; i++)
            {
                KwSailLayout s = sails[i];
                Part(piece, Cloth(i), KwSailCanvas.Cloth(s, 1f), KwSailLooks.Of(s.Spec), s.ClothOrigin);
                if (s.Spec.Shape == 2)
                    Part(piece, Gaff(i), KwSailShapes.GaffSpar(s), KwPrefabs.Planks, s.ClothOrigin);
            }
            Part(piece, LashingsName, KwSailShapes.Lashings(sails), rope, Vector3.zero);
            KwSailRigging.Rig(piece, sails, rope);
            KwPennant.Add(piece, mast);
            KwMastClimb.Add(piece, mast, piece.GetComponentInParent<Ship>() != null);
            for (int i = 0; i < sails.Count; i++)
                Pose(piece, sails, i, 1f);
            return sails;
        }

        /// <summary>A dressed mast's sail i (<see cref="Dress"/>) set to hoist f: its cloth, bolt rope, gaff and the ropes on them.</summary>
        public static void Pose(Transform piece, List<KwSailLayout> sails, int i, float f)
        {
            Transform? cloth = piece.Find(Cloth(i));
            if (cloth != null)
                Hoisted(piece, cloth, sails, i, f);
        }

        /// <summary>Sail i's cloth (found already), its bolt rope, its gaff and the ropes on them, at hoist f.</summary>
        public static void Hoisted(Transform piece, Transform cloth, List<KwSailLayout> sails, int i, float f)
        {
            KwSailLayout s = sails[i];
            KwSailRigging.Bolt(cloth, s, f, KwSailCloth.Rope);
            KwSailMotion.Pose(cloth, s, f);
            Transform? gaff = piece.Find(Gaff(i));
            if (gaff != null)
            {
                // on its cloth, which a launched ship swings about the mast (KwBoomSwing); on the dock the cloth is unturned
                (Vector3 head, Vector3 peak) = KwSailCanvas.Top(s, f);
                gaff.localPosition = cloth.localPosition + cloth.localRotation * KwMesh.ToUnity(head);
                gaff.localRotation = cloth.localRotation * Quaternion.LookRotation(KwMesh.ToUnity(peak - head));
                gaff.localScale = new Vector3(1f, 1f, (peak - head).magnitude + 0.2f);
            }
            KwSailRigging.Follow(piece, sails, i, f, KwSailCloth.Rope);
        }

        // A part of its own mesh, in place of the old one.
        private static Transform Part(Transform piece, string name, Mesh mesh, Material? material, Vector3 kit)
        {
            Transform? old = piece.Find(name);
            if (old != null)
            {
                old.name = name + "_gone";
                Object.Destroy(old.gameObject);
            }
            GameObject made = new GameObject(name) { layer = piece.gameObject.layer };
            made.transform.SetParent(piece, false);
            made.transform.localPosition = KwMesh.ToUnity(kit);
            made.AddComponent<MeshFilter>().sharedMesh = mesh;
            made.AddComponent<MeshRenderer>().sharedMaterial = material;
            made.AddComponent<KwMeshOwner>();
            return made.transform;
        }
    }
}
