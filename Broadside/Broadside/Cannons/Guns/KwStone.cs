using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's own stone for the cannons' stone parts, which no look (<see cref="KwLook"/>) has a word for: the stone
    /// item's material (<c>stone</c> on the game's <c>Stone</c>, Custom/StaticRock; its texture a few pixels of grey, so
    /// the kit's planar UVs serve), borrowed, shared and never changed; should the game lack it the part keeps the look it
    /// was drawn in, <see cref="Stand"/> (iron, logged once). A stone part is drawn in that look and given the stone as
    /// its last touch: the stone cannonball's model (<see cref="KwItemDef.Finish"/>, which its shot wears in flight) and
    /// the ball the bronze ship cannon lays in at its breech (<see cref="KwArmDef.Finish"/>).
    /// </summary>
    public static class KwStone
    {
        /// <summary>The look a stone part is drawn in until it is given the stone.</summary>
        public const KwLook Stand = KwLook.Iron;

        private const string Prefab = "Stone", Name = "stone";
        private static Material? found;
        private static bool warned;

        /// <summary>Gives the stone to every mesh under <paramref name="root"/> named <paramref name="node"/> and the stand-in look ("mv_ball" finds "mv_ball_iron").</summary>
        public static void Wear(Transform root, string node)
        {
            Material? stone = Of();
            if (stone == null)
                return;
            string name = node + "_" + Stand.ToString().ToLowerInvariant();
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.name == name)
                    renderer.sharedMaterial = stone;
            }
        }

        // The stone item's material, found once the game's prefabs are loaded, then kept.
        private static Material? Of()
        {
            if (found != null)
                return found;
            GameObject? item = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Prefab) : null;
            found = item != null ? Named(item) : null;
            if (found == null && ZNetScene.instance != null && !warned)
            {
                warned = true;
                Log.Warn($"Broadside cannons: the game's {Prefab} has no {Name}; stone parts stay iron.");
            }
            return found;
        }

        private static Material? Named(GameObject item)
        {
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer)
                    continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.name == Name)
                        return material;
                }
            }
            return null;
        }
    }
}
