using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's sail's hit box letting a projectile through (<c>IHitProjectile</c>: the game asks it before a
    /// projectile stops on what it struck): a sail never stops a shot. On the game that flies the projectile (the shooter's,
    /// where the game's projectiles hit), a heavy one (<see cref="KwShipHitEnergy.Heavy"/>) holes the sail where it passed:
    /// the place in the cloth's texture worked out here, from the sail set as this game sees it, and sent to the ship's owner
    /// (<see cref="KwRigDamage.ShotRpc"/>), who keeps it; so a ball that holes a sail and then misses the ship still holes it.
    /// A projectile is counted once a sail (its ray can meet the box two frames running).
    /// </summary>
    public sealed class KwSailDamagePierce : MonoBehaviour, IHitProjectile
    {
        private const float Again = 1f;
        private static readonly List<(int Projectile, int Box, float At)> seen = new List<(int, int, float)>();

        public int Piece, Sail;
        public KwSailLayout? Layout;

        public bool OnProjectileHit(Character owner, ItemDrop.ItemData weapon, Projectile projectile, Collider collider, Vector3 hitPoint, bool water, Vector3 normal)
        {
            if (projectile != null && KwShipHitEnergy.Heavy(projectile.m_damage) && First(projectile))
                Guard.Run("Broadside sail pierced", () => Send(projectile, hitPoint));
            return false;
        }

        // Whether this projectile meets this sail for the first time just now.
        private bool First(Projectile projectile)
        {
            (int shot, int box) = (projectile.GetInstanceID(), GetInstanceID());
            seen.RemoveAll(s => Time.time - s.At > Again);
            if (seen.Exists(s => s.Projectile == shot && s.Box == box))
                return false;
            seen.Add((shot, box, Time.time));
            return true;
        }

        // The hole's place in the cloth and its size, sent to the ship's owner.
        private void Send(Projectile projectile, Vector3 point)
        {
            KwRigDamage? damage = GetComponentInParent<KwRigDamage>();
            Transform? frame = GetComponent<KwRigHitFrame>()?.Frame;
            KwRigPiece? mast = damage?.Parts.Get(Piece);
            ZNetView? view = damage != null ? damage.GetComponent<ZNetView>() : null;
            if (damage == null || frame == null || mast == null || Layout == null || view == null || !view.IsValid())
                return;
            Vector3 kit = KwMesh.ToKit(KwRigDamageTakers.Into(damage.transform, damage.GetComponent<Rigidbody>(), frame, point, out _));
            float f = KwRigDamageHits.Set(damage.GetComponent<Ship>(), mast, Sail);
            if (!KwSailDamageHoles.Uv(Layout, kit, f, out Vector2 uv))
                return;
            ZPackage package = new ZPackage();
            package.Write(Piece);
            package.Write(Sail);
            package.Write(uv.x);
            package.Write(uv.y);
            package.Write(KwRigDamageCuts.HoleRadius(projectile.m_damage.GetTotalPhysicalDamage()));
            view.InvokeRPC(KwRigDamage.ShotRpc, package);
        }
    }
}
