using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// What lies round the arena beyond its colliders, clients only, at the arena's origin: the cave's roof and Hel's
    /// river. The cave (<c>ecr_helarena_cave</c>, a rock roof about 400 m up coming down behind the mountains, its
    /// stalactites and roots, glow <c>glow_crystal</c> and <c>glow_crack</c>, no colliders) is dressed in the hall's stone
    /// like the land, and casts no shadow: it stands between the sun and the arena. The river (<c>ecr_helarena_river</c>,
    /// a flat ring at y -8 from r 70 to 470) keeps its own Standard material, neither casts nor takes shadows, and flows
    /// (<see cref="HallRiver"/>). Its bubbles and mist are <see cref="HallBubbles"/> and <see cref="HallMist"/>.
    /// </summary>
    internal static class HallOutside
    {
        public static void Cave(Transform hall, int layer)
        {
            GameObject? cave = HallBuilder.Piece(hall, HallBundles.Cave, Vector3.zero, layer, visuals: true);
            if (cave != null)
            {
                Shadowless(cave, receive: true);
            }
        }

        public static void River(Transform hall)
        {
            GameObject? prefab = HallBundles.Prefab(HallBundles.River, HallBundles.River);
            if (prefab == null)
            {
                return;
            }
            GameObject river = Object.Instantiate(prefab, hall, false);
            river.name = HallBundles.River;
            river.transform.localPosition = Vector3.zero;
            river.transform.localRotation = Quaternion.identity;
            Shadowless(river, receive: false);
            river.AddComponent<HallRiver>();
        }

        private static void Shadowless(GameObject root, bool receive)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = receive && renderer.receiveShadows;
            }
        }
    }
}
