using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The net winch's prefab: a copy of the game's wood floor (its Piece, WearNTear, network view, workbench and build
    /// sounds), stripped (<see cref="KwPrefabs.Strip"/>) and given the winch's own parts, made in code
    /// (<see cref="KwTrawlShapes"/>): <see cref="Model"/> (the frame in plank wood with its irons, its two boxes and the
    /// placing box, <see cref="KwPrefabs.Anchor"/>, and the lead block <see cref="Heel"/>), <see cref="Drum"/> (turning
    /// about the axle: barrel, crank and the roll of net <see cref="Roll"/>, in the net's hemp where it is loaded) and
    /// <see cref="Davit"/> (swinging about the post: arm and the turning <see cref="Sheave"/>). Its +Z is outboard: it is
    /// built facing over the stern or a side rail. Never breaks for lack of support or rain (a ship holds it up).
    /// </summary>
    public static class KwTrawlBuild
    {
        public const string Model = "model", Drum = "drum", Roll = "roll", Davit = "davit", Sheave = "sheave", Heel = "heel";

        public static GameObject Make(GameObject floor)
        {
            GameObject copy = PrefabBench.Copy(floor, KwTrawl.Prefab);
            KwPrefabs.Strip(copy);
            GameObject model = Frame(copy);
            Turning(copy.transform);
            Configure(copy, model);
            copy.AddComponent<KwTrawlWinch>();
            return copy;
        }

        private static GameObject Frame(GameObject piece)
        {
            GameObject model = Part(piece.transform, Model, Vector3.zero, KwTrawlShapes.Frame, KwPrefabs.Planks);
            Box(model, KwTrawlShapes.BodyMin, KwTrawlShapes.BodyMax);
            Box(model, KwTrawlShapes.PostMin, KwTrawlShapes.PostMax);
            KwPrefabs.Anchor(model);
            Part(model.transform, "irons", Vector3.zero, KwTrawlShapes.Irons, KwTierMaterials.Of(KwLook.Iron));
            Pivot(model.transform, Heel, KwTrawlShapes.HeelAt);
            return model;
        }

        // The parts that turn: the drum on its axle with its crank and the roll of net; the davit on its post with its sheave.
        private static void Turning(Transform piece)
        {
            Transform drum = Pivot(piece, Drum, KwTrawlShapes.AxleAt);
            Part(drum, "barrel", Vector3.zero, KwTrawlShapes.Barrel, KwPrefabs.Planks);
            Part(drum, "crank", Vector3.zero, KwTrawlShapes.Crank, KwTierMaterials.Of(KwLook.Iron));
            Transform roll = Part(drum, Roll, Vector3.zero, KwTrawlShapes.Roll, KwTrawlNet.Hemp ?? KwPrefabs.Planks).transform;
            roll.localScale = new Vector3(1f, KwTrawlTune.RollFull, KwTrawlTune.RollFull);
            Transform davit = Pivot(piece, Davit, KwTrawlShapes.PivotAt);
            Part(davit, "arm", Vector3.zero, KwTrawlShapes.Arm, KwPrefabs.Planks);
            Transform sheave = Pivot(davit, Sheave, KwTrawlShapes.SheaveAt);
            Part(sheave, "wheel", Vector3.zero, KwTrawlShapes.Wheel, KwPrefabs.Planks);
        }

        // An empty child at a kit point (a pivot or a point), on the piece's layer.
        private static Transform Pivot(Transform parent, string name, Vector3 kit)
        {
            GameObject pivot = new GameObject(name) { layer = parent.gameObject.layer };
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = KwMesh.ToUnity(kit);
            return pivot.transform;
        }

        // A drawn part: a mesh about its own pivot (kit axes), no collider.
        private static GameObject Part(Transform parent, string name, Vector3 kit, System.Action<MeshBuilder> shape, Material? material)
        {
            GameObject part = Pivot(parent, name, kit).gameObject;
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            shape(b);
            part.AddComponent<MeshFilter>().sharedMesh = b.ToMesh(KwTrawl.Prefab + "_" + name);
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            return part;
        }

        // A box collider on the model from kit corners (kit to Unity swaps the axes and turns y round).
        private static void Box(GameObject model, Vector3 min, Vector3 max)
        {
            BoxCollider box = model.AddComponent<BoxCollider>();
            box.center = KwMesh.ToUnity((min + max) / 2f);
            Vector3 size = KwMesh.ToUnity(max - min);
            box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        private static void Configure(GameObject copy, GameObject model)
        {
            Piece piece = copy.GetComponent<Piece>();
            (piece.m_name, piece.m_description) = (KwTrawl.Label, KwTrawl.Description);
            (piece.m_category, piece.m_enabled) = (Piece.PieceCategory.Misc, true);
            // the build menu's Transportation filter, as Broadside's pieces and the game's own ships carry
            piece.m_usage = Piece.UsageTagFlags.Transport;
            WearNTear wear = copy.GetComponent<WearNTear>();
            if (wear == null)
                return;
            (wear.m_noSupportWear, wear.m_noRoofWear) = (false, false);
            wear.m_new = wear.m_worn = wear.m_broken = model;
            (wear.m_wet, wear.m_fragmentRoots) = (null, null);
        }
    }
}
