using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// The bolt a crossbowman fires: a copy of the game's bone bolt projectile (arbalest_projectile_bone) wearing the
    /// bundle's own bone bolt (the one in its groove and quiver, grimed like the skeleton) in place of the game's look.
    /// It flies straight, as the skeleton archer's arrow does, so it lands where the game aimed it: at the target's
    /// middle as it stood when the bolt left. Its hit, sound and trail are the game's; the damage comes from the shot
    /// (<see cref="XbowShot"/>).
    /// </summary>
    public static class XbowBolt
    {
        private const string GameLook = "default";   // the game projectile's own bolt model, flying along +Z
        private const float Length = 0.43f;          // AssetWorkshop assets/ecp_xbow_bolt: nock at its origin, head along +Z
        private const float Head = 0.05f;            // where the game projectile's own head is, ahead of its origin

        public static GameObject Build(GameObject gameBolt, GameObject look, Material skin)
        {
            GameObject bolt = PrefabBench.Copy(gameBolt, XbowPrefabs.Bolt);
            var projectile = bolt.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.m_gravity = 0f;
                projectile.m_ttl = 10f;
            }
            Transform? old = bolt.transform.Find(GameLook);
            GameObject ours = Object.Instantiate(look, bolt.transform, false);
            ours.name = "ecp_xbow_bolt_look";
            ours.transform.localPosition = new Vector3(0f, 0f, Head - Length);
            int layer = old != null ? old.gameObject.layer : bolt.layer;
            foreach (Transform part in ours.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = layer;
            }
            XbowKit.Dress(ours, skin);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }
            return bolt;
        }
    }
}
