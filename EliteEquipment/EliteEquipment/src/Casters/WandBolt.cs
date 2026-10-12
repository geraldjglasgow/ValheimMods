using BundlePrefabs;
using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The wand's bolt: a copy of the game's Staff of Frost shard (<c>staff_iceshard_projectile</c>: a plain networked
    /// projectile with no area burst), registered in the scene on every peer, its ice looks and hit effects gone. Its
    /// damage is the wand's (the game's projectile attack hands it over). The workshop's mote (<c>ee_fx_bolt</c>) is
    /// dressed onto it once the scene has woken (the game's particle shaders are loaded then), on machines that draw; the
    /// hit burst is <see cref="CasterFx"/>'s, sent from the owner where it lands (<see cref="BoltHit"/>). Its size is shared
    /// with every machine (the game's initial scale sync), for a charged bolt is bigger (<see cref="BoltSize"/>).
    /// </summary>
    public static class WandBolt
    {
        public const string Prefab = "EE_WandBolt";
        private const string Game = "staff_iceshard_projectile";
        // Straight to the crosshair (user, 2026-10-11: "the spells need to go right to where the person is aiming").
        private const float Gravity = 0f;
        private const float Seconds = 3f;

        private static bool dressed;

        public static GameObject Build(GameObject shard)
        {
            GameObject bolt = PrefabBench.Copy(shard, Prefab);
            for (int i = bolt.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bolt.transform.GetChild(i).gameObject);
            Projectile projectile = bolt.GetComponent<Projectile>();
            projectile.m_gravity = Gravity;
            projectile.m_ttl = Seconds;
            projectile.m_hitEffects = new EffectList();
            projectile.m_hitWaterEffects = new EffectList();
            projectile.m_spawnOnHit = null;
            bolt.GetComponent<ZNetView>().m_syncInitialScale = true;
            return bolt;
        }

        public static string GameName => Game;

        /// <summary>The mote on the bolt, once, after the scene woke; nothing on a machine that draws nothing.</summary>
        public static void Dress(GameObject bolt)
        {
            if (dressed || bolt == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            dressed = true;
            GameObject mote = CasterFx.Prepared(CasterFx.Bolt);
            if (mote == null)
                return;
            GameObject copy = Object.Instantiate(mote, bolt.transform, false);
            copy.name = "visual";
            foreach (TimedDestruction timer in copy.GetComponentsInChildren<TimedDestruction>(true))
                Object.DestroyImmediate(timer);
            ChargeOrb.ScaleWithParent(copy);
            copy.SetActive(true);
        }
    }
}
