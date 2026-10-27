using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a ship cannon's shot does to its carriage and its gunner's spot, from the ZDO alone (<see cref="WeaponState"/>:
    /// the age of its last shot), so every peer that draws it shows the same. The shot leaving the muzzle (its
    /// <see cref="KwArmDef.FireDelay"/> into the lanyard's pull) rolls the carriage (the yaw part: brackets, trucks, quoin
    /// and gun) back along the way it points by <see cref="KwShipSpec.Recoil"/>, fast and slowing as the breeching takes
    /// it, holds it there a moment with the breeching taut, and runs it out again over <see cref="Out"/> seconds, slow to
    /// start and slow to stop; its trucks roll as it goes (<see cref="KwWheels"/>). The piece itself and its colliders
    /// never move, so nothing is synced and nothing desyncs; how far back it is (<see cref="Recoiled"/>) lets the gunner's
    /// hands go while it recoils (<see cref="KwArmHands"/>). The gunner's stand point (the cannon's "attach", behind the
    /// breech, on the yaw part) turns round the carriage's pivot with it as it is levered round, so he stays behind the
    /// breech (the library walks him after it once it has moved far enough), but never rolls back with it, and slides
    /// aside out of the recoil's way as he yanks the lanyard and back once the gun has nearly run out, <see cref="Step"/>
    /// seconds each way (<see cref="Stand"/>, <see cref="KwShipSpec.Aside"/>): on his own game the game's movement walks
    /// him there, and every peer sees him by his own synced position. Whenever the carriage, the gun's tilt or the
    /// barrel's kick has moved since the last frame, the quoin is slid under the breech (<see cref="KwQuoin"/>) and the
    /// breeching laid again (<see cref="KwBreeching"/>). Drawn by the cannon's look (<see cref="KwArmLook"/>), nothing on a
    /// dedicated server.
    /// </summary>
    public sealed class KwShipRecoil
    {
        /// <summary>The roll back, the moment held there and the run out again, seconds; the gunner's step aside and back.</summary>
        public const float Back = 0.2f, Hold = 0.25f, Out = 2f, Step = 0.3f;

        private readonly KwShipSpec ship;
        private readonly float delay;
        private readonly Transform root, yaw, pitch, gun, stand;
        private readonly KwQuoin quoin;
        private readonly KwBreeching breeching;
        private readonly Vector3 rest, aside;
        private Vector3 lastAt, lastGun;
        private Quaternion lastTurn, lastTilt;
        private bool laid;

        private KwShipRecoil(KwShipSpec ship, KwArmDef def, Transform root, Transform yaw, Transform pitch, Transform gun, Transform stand, KwQuoin quoin, KwBreeching breeching)
        {
            (this.ship, delay, this.root, this.yaw, this.pitch, this.gun, this.stand) = (ship, def.FireDelay, root, yaw, pitch, gun, stand);
            (this.quoin, this.breeching, rest) = (quoin, breeching, yaw.localPosition);
            aside = KwMesh.ToUnity(ship.Aside(def.Stand)) - rest;   // in the yaw part's own space, as the stand point is
        }

        /// <summary>A ship cannon's recoil, its parts found under the piece; null when one is missing.</summary>
        public static KwShipRecoil? Of(Transform root, KwArmDef def, Transform yaw, Transform pitch, Transform stand)
        {
            KwShipSpec? ship = def.Ship;
            Transform? gun = KwShipRig.Gun(root);
            KwQuoin? quoin = ship != null ? KwQuoin.Of(root, ship) : null;
            KwBreeching? breeching = ship != null ? KwBreeching.Of(root, ship) : null;
            if (ship == null || gun == null || quoin == null || breeching == null)
                return null;
            return new KwShipRecoil(ship, def, root, yaw, pitch, gun, stand, quoin, breeching);
        }

        /// <summary>How far back the carriage is a while after the shot left, as a share of its recoil: out at once, held, then run out.</summary>
        public static float Share(float age)
        {
            if (age < 0f || age >= Back + Hold + Out)
                return 0f;
            if (age < Back)
                return 1f - (1f - age / Back) * (1f - age / Back);
            return age < Back + Hold ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (age - Back - Hold) / Out);
        }

        /// <summary>
        /// How far the gunner has stepped aside a while after he pulled (the shot leaving <paramref name="delay"/> seconds
        /// into it), 0 (behind the breech) to 1 (clear of the recoil): out as he pulls, back as the gun finishes running out.
        /// </summary>
        public static float Aside(float age, float delay)
        {
            if (age < 0f)
                return 0f;
            float back = Mathf.SmoothStep(0f, 1f, (age - delay - Back - Hold - Out * 0.85f) / Step);
            return Mathf.SmoothStep(0f, 1f, age / Step) * (1f - back);
        }

        /// <summary>Its breeching, which is hidden while the cannon is hauled (<see cref="KwBreechingTie"/>).</summary>
        public KwBreeching Breeching => breeching;

        /// <summary>How far back the carriage has rolled now, metres.</summary>
        public float Recoiled(in WeaponState s) => ship.Recoil * Share(s.ShotAge - delay);

        /// <summary>The gunner's spot now: behind the breech, turned with the carriage but not rolled back with it, and stepped aside at a shot.</summary>
        public Vector3 Stand(in WeaponState s) =>
            root.TransformPoint(rest + yaw.localRotation * Vector3.Lerp(stand.localPosition, aside, Aside(s.ShotAge, delay)));

        /// <summary>The carriage rolled back as far as the shot has it now, and the quoin and breeching after it.</summary>
        public void Draw(in WeaponState s)
        {
            Vector3 at = rest + yaw.localRotation * new Vector3(0f, 0f, -Recoiled(s));
            if (yaw.localPosition != at)
                yaw.localPosition = at;
            if (!Moved())
                return;
            quoin.Place();
            breeching.Lay();
        }

        // Whether the carriage, the gun's tilt or the barrel's kick moved since the last frame (always, the first time).
        private bool Moved()
        {
            bool moved = !laid || yaw.localPosition != lastAt || yaw.localRotation != lastTurn || pitch.localRotation != lastTilt || gun.localPosition != lastGun;
            (lastAt, lastTurn, lastTilt, lastGun, laid) = (yaw.localPosition, yaw.localRotation, pitch.localRotation, gun.localPosition, true);
            return moved;
        }
    }
}
