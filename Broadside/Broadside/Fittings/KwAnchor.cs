using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The anchor windlass, placed on a deck (<see cref="KwAnchorPlace"/>), facing any way: a windlass with its cable wound on
    /// the drum and an iron stock anchor (<see cref="KwAnchorShapes"/>). Its anchor goes overboard at the nearest place on
    /// the ship's side, bow or stern its cable reaches clear of everything, straight out from the drum where it can
    /// (<see cref="KwAnchorPlan"/>): led from the drum along the deck through a fairlead (a chock on the sheer or railing, a
    /// hawse pipe from a cabin), the weighed anchor hanging just under it outside the hull. On the dock it is shown so
    /// (<see cref="KwAnchorDock"/>); with no clear place within reach the anchor stays stowed upright on the deck before it,
    /// as the piece is made, and the hover says why. On a launched ship E at it drops or weighs the ship's anchor
    /// (<see cref="KwShipWindlass"/>, <see cref="KwShipAnchor"/>): dropped, it lies on the sea floor under the fairlead and
    /// the ship rides to it there. The windlass wears the plank wood, the coil the game's rope (<see cref="KwAnchorCoil"/>),
    /// the anchor the iron wall's metal. It costs what a post cap or a hatch does (<see cref="KwBpCost"/>). Kit axes: x
    /// forward, y left, z up; the origin on the deck's top at the windlass's middle.
    /// </summary>
    public static class KwAnchor
    {
        public const string Prefab = "KW_anchor";
        public const string AnchorName = "anchor", CoilName = "coil";

        /// <summary>Where the stowed anchor's ring is, from the windlass (kit): ahead of it, its crown on the deck (with no clear place overboard).</summary>
        public static readonly Vector3 Stowed = new Vector3(0.6f, 0f, KwAnchorShapes.Length + 0.02f);

        public static bool Is(string prefab) => prefab == Prefab;

        public static IEnumerable<KwDef> All()
        {
            yield return new KwDef(Prefab, "Anchor windlass", "fitting") { Wood = true, Shape = KwAnchorShapes.Windlass, Setup = Setup };
        }

        private static void Setup(GameObject prefab)
        {
            Part(prefab, CoilName, Vector3.zero, KwAnchorCoil.ToMesh(), KwSailCloth.Rope);
            Part(prefab, AnchorName, Stowed, Built(prefab, AnchorName, KwAnchorShapes.Anchor), KwTierMaterials.Of(KwLook.Iron));
            prefab.AddComponent<KwAnchorDock>();
            prefab.GetComponent<Piece>().m_description =
                "Place on a deck: the anchor hangs over the nearest side, its cable led through a fairlead. On a launched ship, E drops or weighs the anchor: anchored, the ship holds its place and only swings round it.";
        }

        private static Mesh Built(GameObject prefab, string name, System.Action<MeshBuilder> shape)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            shape(b);
            return b.ToMesh(prefab.name + "_" + name);
        }

        // A drawn part of its own under the piece (no collider): the coil in rope, the anchor in iron.
        private static void Part(GameObject prefab, string name, Vector3 kit, Mesh mesh, Material? material)
        {
            GameObject part = new GameObject(name) { layer = prefab.layer };
            part.transform.SetParent(prefab.transform, false);
            part.transform.localPosition = KwMesh.ToUnity(kit);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            Material? worn = material != null ? material : KwPrefabs.Planks;
            if (worn != null)
                renderer.sharedMaterial = worn;
        }
    }
}
