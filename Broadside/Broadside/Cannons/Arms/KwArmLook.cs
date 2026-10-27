using LocalEffects;
using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What every peer that draws shows of a cannon besides its turn (the library turns the yaw part and tilts the pitch
    /// part to the aim), each frame after the turn and before the gunner's hands, all from the ZDO (<see cref="WeaponState"/>):
    /// a shot leaving partway into its motion (<see cref="KwArmLaunch"/>), the moving parts in the pose the shot and the
    /// reload call for (<see cref="KwArmMotion"/>), a ship cannon's carriage rolling back and the quoin and breeching laid
    /// after it (<see cref="KwShipRecoil"/>), its breeching hidden while it is hauled (<see cref="KwBreechingTie"/>), and the shot's game effects at the muzzle (sound, smoke, flash: local
    /// copies, <see cref="LocalEffect"/>, never networked). Nothing on a dedicated server.
    /// </summary>
    public static class KwArmLook
    {
        private const float EffectLife = 10f;

        public static void Draw(KwArm arm, in WeaponState s)
        {
            arm.Launch.Tick(arm, s);
            arm.Motion?.Pose(arm, s);
            arm.Recoil?.Draw(s);
            arm.Recoil?.Breeching.Show(KwBreechingTie.Shown(arm));
        }

        /// <summary>A shot leaving: the muzzle's effects.</summary>
        public static void Blast(KwArm arm, KwShot shot)
        {
            foreach (string name in shot.Effects)
                Muzzle(ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null, arm.Muzzle, shot.EffectScale);
        }

        // A local copy of a game effect at the muzzle, facing along the barrel, left standing there (smoke stays where it
        // was made); its own timer removes it, or ten seconds should it have none.
        private static void Muzzle(GameObject? prefab, Transform muzzle, float scale)
        {
            GameObject? copy = Mathf.Approximately(scale, 1f)
                ? LocalEffect.Attach(prefab, muzzle, muzzle.position, endless: false)
                : LocalEffect.AttachScaled(prefab, muzzle, muzzle.position, endless: false, scale: scale);
            if (copy == null)
                return;
            copy.transform.SetParent(null, true);
            copy.transform.rotation = muzzle.rotation;
            Object.Destroy(copy, EffectLife);
        }
    }
}
