using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The captain's wheel, placed on a deck or a cabin's roof (<see cref="KwAnchorPlace"/>): a ship's wheel on a stand (a foot plate, two
    /// frames fore and aft under a cap, the axle through both and a pedestal between them, where the steering gear's short
    /// shaft comes up through the deck to the axle, <see cref="KwWheelShapes"/>) with eight spokes running out past the rim
    /// as handles. The wheel is the "wheel" child, turned about the axle. On the dock E takes its helm and A and D turn it
    /// (<see cref="KwWheelHelm"/>); on a launched ship it turns with the ship's helm and the helmsman stands at it
    /// (<see cref="KwShipWheel"/>). With a rudder on the same ship it steers the rudder through a steering gear under its
    /// deck (<see cref="KwRudderGear"/>), and the rudder has no deck tiller. It costs what a post cap does
    /// (<see cref="KwBpCost"/>). Kit axes: x forward (the bow), y left, z up; the origin on the deck under the stand's
    /// middle.
    /// </summary>
    public static class KwWheel
    {
        public const string Prefab = "KW_helm", WheelName = "wheel", AttachName = "attach";

        /// <summary>The axle's height over the deck; how far the wheel turns each way at full helm (the steering gear's ratio times the rudder's turn).</summary>
        public const float AxleZ = 1.1f, Turn = KwGearTrain.WheelTurn;

        /// <summary>The wheel's hub on the axle, aft of the stand.</summary>
        public static readonly Vector3 Hub = new Vector3(-0.36f, 0f, AxleZ);

        /// <summary>Where the helmsman stands: behind the wheel, facing the bow.</summary>
        public static readonly Vector3 Stand = new Vector3(-0.95f, 0f, 0f);

        public static bool Is(string prefab) => prefab == Prefab;

        public static IEnumerable<KwDef> All()
        {
            yield return new KwDef(Prefab, "Captain's wheel", "fitting") { Shape = KwWheelShapes.Stand, Wood = true, Setup = Setup };
        }

        // The turning wheel on the axle, the helmsman's place behind it, and the dock's helm.
        private static void Setup(GameObject prefab)
        {
            Transform wheel = Part(prefab.transform, WheelName, KwMesh.ToUnity(Hub), KwWheelShapes.Wheel, KwPrefabs.Planks);
            float across = 2f * KwWheelShapes.Handle;
            wheel.gameObject.AddComponent<BoxCollider>().size = new Vector3(across, across, 0.12f);
            Transform attach = new GameObject(AttachName) { layer = prefab.layer }.transform;
            attach.SetParent(prefab.transform, false);
            attach.localPosition = KwMesh.ToUnity(Stand);
            prefab.AddComponent<KwWheelHelm>();
            prefab.GetComponent<Piece>().m_description = "Place on a deck or a cabin's roof, facing the bow. It steers the ship's rudder through a steering gear under the deck "
                + "(a shaft with gears to a quadrant on the rudder's stock; up the cabin's back wall and under its roof to a wheel on the roof) in place of a deck tiller. "
                + "E takes the helm; A and D turn the wheel.";
        }

        // A drawn part of its own under a parent (no collider), in the plank wood.
        private static Transform Part(Transform parent, string name, Vector3 at, System.Action<MeshBuilder> shape, Material? material)
        {
            GameObject part = new GameObject(name) { layer = parent.gameObject.layer };
            part.transform.SetParent(parent, false);
            part.transform.localPosition = at;
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            shape(b);
            part.AddComponent<MeshFilter>().sharedMesh = b.ToMesh(Prefab + "_" + name);
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            Material? worn = material != null ? material : KwPrefabs.Planks;
            if (worn != null)
                renderer.sharedMaterial = worn;
            return part.transform;
        }
    }
}
