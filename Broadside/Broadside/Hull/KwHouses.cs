using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Cabins as generated parts (<see cref="KwGenerated"/>), the Cabin tool's part: what its saved text builds
    /// (<see cref="KwCabinSpec"/>), in three looks (<see cref="KwHouseLayers"/>), and the part built in the world from its
    /// ZDO on every peer: its model wearing every look as a submesh and walked on as one mesh. A cabin's part is
    /// <c>KW_gen_cabin</c>; a castle built before the castle tool became the Cabin tool is still a <c>KW_gen_castle</c> piece,
    /// which builds the same way from its old text (<see cref="Castle"/>). Every text that reads builds a cabin fitted to its
    /// hull (<see cref="KwCabinFit"/>), a plain frame where none fits; a part whose text does not read at all wears the
    /// Cabin tool's mark, so no cabin part is ever invisible.
    /// </summary>
    public static class KwHouses
    {
        /// <summary>The Cabin tool's name, and the old castle tool's, whose built parts still load as cabins.</summary>
        public const string Cabin = "cabin", Castle = "castle";

        public static bool Owns(string tool) => tool == Cabin || tool == Castle;

        /// <summary>Whether a piece's prefab is a built cabin (or an old castle).</summary>
        public static bool IsPart(string prefab) => prefab == KwToolPieces.PartPrefix + Cabin || prefab == KwToolPieces.PartPrefix + Castle;

        /// <summary>A saved cabin in its looks (one: every look in one builder); null when its text does not read.</summary>
        public static KwHouseLayers? Layers(string tool, string saved, KwBiomeTier tier, bool one = false) =>
            Owns(tool) && KwCabinSpec.TryParse(saved, out KwCabinSpec spec) ? KwCabinShape.Layers(spec, tier, one) : null;

        /// <summary>A saved cabin as one mesh (its looks together); null when it does not read.</summary>
        public static Mesh? Whole(string tool, string saved, KwBiomeTier tier) => Layers(tool, saved, tier, true)?.Whole("KW_gen_" + tool);

        /// <summary>Each look of a saved cabin as its own mesh with its material (a launched ship's and a blueprint's look).</summary>
        public static List<(Material Material, Mesh Mesh)> Looks(string tool, string saved, KwBiomeTier tier) =>
            Layers(tool, saved, tier)?.Looks(tier) ?? new List<(Material Material, Mesh Mesh)>();

        /// <summary>Builds a cabin part in the world from its ZDO: false for any other tool's part, left to its own build.</summary>
        public static bool Build(Transform part, string tool, ZDO zdo)
        {
            if (!Owns(tool))
                return false;
            KwBiomeTier tier = KwBiomeTiers.Of(zdo);
            KwHouseLayers? layers = Layers(tool, zdo.GetString(KwGenerated.Key), tier) ?? Unread();
            Transform? model = part.Find("model");
            if (model == null)
                return true;
            (Mesh look, Material[] materials) = layers.Layered(tier, "KW_gen_" + tool);
            Meshes(model).Wear(look, materials, layers.Hard.ToMesh("KW_gen_" + tool + "_solid"));
            KwShipBody.Step("Broadside openings", () => KwOpeningPart.Fit(part.gameObject, tool, zdo.GetString(KwGenerated.Key), tier));
            return true;
        }

        // A part whose text does not read at all: the Cabin tool's mark in the plank wood, so it is seen, and the hammer can take it away.
        private static KwHouseLayers Unread()
        {
            KwHouseLayers layers = new KwHouseLayers();
            layers.Draw(layers.Wood, KwCabinTool.Mark);
            return layers;
        }

        // The model's meshes holder, made the first time.
        private static KwHouseMeshes Meshes(Transform model)
        {
            KwHouseMeshes? made = model.GetComponent<KwHouseMeshes>();
            return made != null ? made : model.gameObject.AddComponent<KwHouseMeshes>();
        }
    }

    /// <summary>A cabin's model: its look (a submesh a look, their materials) and what is walked on, let go when new ones come or it goes.</summary>
    public sealed class KwHouseMeshes : MonoBehaviour
    {
        private Mesh? look, hard;

        public void Wear(Mesh newLook, Material[] materials, Mesh newHard)
        {
            GetComponent<MeshFilter>().sharedMesh = newLook;
            GetComponent<MeshRenderer>().sharedMaterials = materials;
            GetComponent<MeshCollider>().sharedMesh = newHard;
            Let();
            (look, hard) = (newLook, newHard);
        }

        private void OnDestroy() => Let();

        private void Let()
        {
            if (look != null)
                Destroy(look);
            if (hard != null)
                Destroy(hard);
        }
    }
}
