using BundlePrefabs;
using EliteCreaturesReborn.Arena.Trial;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Hel's Anvils (the bundle's <c>ecr_helarena_anvil</c>: an anvil on a stone block and plinth with a soul-brazier,
    /// 2.3 x 1.2 m, its <c>use</c> point in front, <c>fire</c> on its embers), where players spend souls to repair their
    /// gear between waves (the trial's <see cref="HelAnvil"/>). Two, one each side of Hel's dais, at (+-11.5, 0, -56) in
    /// arena coordinates, each turned to face the arena's centre so a player coming from the floor reaches its
    /// <c>use</c> side. Chosen against CONTRACT.md's clear zones: 5.2 m clear of the dais's side (x +-5) and beside its
    /// front step, outside the 6 m before it; 10.8 m from the 157.5 and 202.5 degree archway lanes' lines (6 m lanes);
    /// 17 m from the throne path's pillars (+-8.3, -39.1); 8 m inside the wall (r 57.2 of 66). Built with the arena on
    /// every peer for their four colliders (static_solid); on clients dressed in the arena's stone with the iron and
    /// Bloodgold shining through the bundle's metal mask at 0.72, <c>glow_souls</c> kept, a small blue flame (the
    /// sconces' brazier fire at 0.4, no light) on the embers, and the trial's anvil logic on the root.
    /// </summary>
    internal static class HallAnvils
    {
        private static readonly Vector3[] Spots = { new Vector3(11.5f, 0f, -56f), new Vector3(-11.5f, 0f, -56f) };
        private const string Fire = "fire";
        private const float FireSize = 0.4f;
        private const float MetalGloss = 0.72f;

        public static void Place(Transform hall, int layer, bool visuals)
        {
            GameObject? prefab = HallBundles.Prefab(HallBundles.Anvil, HallBundles.Anvil);
            if (prefab == null)
            {
                return;
            }
            foreach (Vector3 spot in Spots)
            {
                Place(hall, prefab, spot, layer, visuals);
            }
        }

        private static void Place(Transform hall, GameObject prefab, Vector3 spot, int layer, bool visuals)
        {
            GameObject anvil = Object.Instantiate(prefab, hall, false);
            anvil.name = HallBundles.Anvil;
            anvil.transform.localPosition = spot;
            anvil.transform.localRotation = Quaternion.LookRotation(new Vector3(-spot.x, 0f, -spot.z));
            HallBuilder.SetLayer(anvil.transform, layer);
            if (!visuals)
            {
                return;
            }
            HallMaterials.DressStoneMetal(anvil, MetalGloss);
            Transform? fire = GameMaterials.Find(anvil.transform, Fire);
            if (fire != null)
            {
                HallFires.Flame(fire, FireSize);
            }
            anvil.AddComponent<HelAnvil>();
        }
    }
}
