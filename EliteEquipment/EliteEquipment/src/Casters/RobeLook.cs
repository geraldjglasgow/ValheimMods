using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;
using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The workshop robes (<c>ee_seer_meadows</c> v2, the long seer's robes; its BRIEF's runtime steps, 2026-10-11) on a copy
    /// of a game piece: the hood on the Eitr-weave Hood, the robe on the Eitr-weave Robe (the game's long robe, its skirt
    /// skinned to the legs the way ours is), the trousers on the leather trousers. The game's own parts go (its worn
    /// <c>attach_skin</c> and dropped look; a helmet's <c>attach</c>, which the game would take first); the bundle piece's
    /// <c>attach_skin</c> (inactive, as the game keeps it) and dropped <c>model</c> come in. Every mesh wears the Eitr-weave
    /// Hood's creature material with its own bake; the body paint under the robe and trousers is a copy of the game piece's
    /// armour material wearing the robe's <c>_ChestTex</c> or the trousers' <c>_LegsTex</c> and their normal maps. The head
    /// piece hides hair by its set's rule and leaves beards. Which robe mesh a body shows is <see cref="RobeBody"/>'s.
    /// </summary>
    public static class RobeLook
    {
        private const float Gloss = 0.1f;
        private const string Worn = "attach_skin";

        public static void Wear(GearPiece piece, GameObject item, GameObject game, GameObject helmet, GameObject model)
        {
            if (model == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: {item.name} keeps the {game.name} look (no workshop robes)");
                return;
            }
            Material creature = GameMaterials.Borrow(helmet, Worn) ?? GameMaterials.Borrow(game, Worn);
            Strip(item);
            foreach (Transform part in model.transform)
            {
                GameObject copy = Object.Instantiate(part.gameObject, item.transform, false);
                copy.name = part.name;
            }
            SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_armorMaterial = Armour(piece, shared.m_armorMaterial) ?? shared.m_armorMaterial;
            Dress(item.transform, creature);
            Collide(item);
            if (piece.Part == "hood")
                Head(shared, piece.Set);
        }

        private static void Strip(GameObject item)
        {
            for (int i = item.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(item.transform.GetChild(i).gameObject);
            foreach (Collider collider in item.GetComponents<Collider>())
                Object.DestroyImmediate(collider);
            foreach (Renderer renderer in item.GetComponents<Renderer>())
                Object.DestroyImmediate(renderer);
        }

        /// <summary>The game piece's body paint material wearing the robe's or the trousers' paint; null for the hood.</summary>
        private static Material Armour(GearPiece piece, Material game)
        {
            string part = piece.Part == "robe" ? "Chest" : piece.Part == "trousers" ? "Legs" : null;
            if (part == null || game == null)
                return null;
            string texture = piece.Asset + "_" + part.ToLowerInvariant();
            Texture2D paint = CasterBundles.Texture(piece.Bundle, texture);
            if (paint == null)
                return null;
            var material = new Material(game) { name = piece.Asset };
            material.SetTexture("_" + part + "Tex", paint);
            material.SetTexture("_" + part + "BumpMap", CasterBundles.Texture(piece.Bundle, texture + "_bump"));
            return material;
        }

        /// <summary>Every mesh, worn or dropped, in the creature material wearing its own bake (the placeholder's maps).</summary>
        private static void Dress(Transform item, Material creature)
        {
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
            {
                if (creature != null && renderer.sharedMaterial != null)
                    renderer.sharedMaterial = GameMaterials.Plain(GameMaterials.Dress(creature, renderer.sharedMaterial), Gloss);
                renderer.gameObject.layer = item.gameObject.layer;
            }
        }

        private static void Collide(GameObject item)
        {
            Transform dropped = item.transform.Find("model");
            if (dropped == null)
                return;
            Bounds bounds = ModelBounds.In(dropped.gameObject, item.transform);
            BoxCollider box = item.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = Vector3.Max(bounds.size, Vector3.one * 0.05f);
        }

        /// <summary>The head piece hides hair by the set's rule (a hood, a hat or a scarf-like crown, as its workshop asset was fitted) and leaves beards.</summary>
        private static void Head(SharedData shared, RobeSet set)
        {
            shared.m_helmetHideHair = set.Hair;
            shared.m_helmetHideBeard = ItemDrop.ItemData.HelmetHairType.Default;
        }
    }
}
