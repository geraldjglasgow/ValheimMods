using System;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A fish the trawl landed (<see cref="KwTrawlDrop"/>), riding its ship until it is picked up, on every peer: the
    /// game's items slide off a moving deck and Broadside's riders are pieces, so the fish's ZDO records its ship's own id
    /// (<see cref="ShipKey"/>, the ship's <c>kw_ship_uid</c>), its place and turn in the ship's frame (<see cref="PosKey"/>,
    /// <see cref="RotKey"/>) and its fall out of the bag (<see cref="FallKey"/> metres from <see cref="AtKey"/>, the network
    /// clock), written once by the game that made it. Each peer holds the fish still (its body kinematic and out of the
    /// physics, which still finds it for the hover and the game's pickup; its synced transform off) and puts it where the
    /// ship is drawn every frame, falling at first; moved by the game (drawn to a player picking it up) it keeps where it
    /// was moved to. Its owner keeps its ZDO's place with the ship once a second, so it loads and unloads with the ship;
    /// with the ship not in its scene it follows the ship's ZDO found near; with the ship gone for good (destroyed, or
    /// nowhere near for a minute, <see cref="KwShipRiderRules.Released"/>) it is let go as an ordinary item where it is.
    /// Picked up, it is the game's own item, as any fish.
    /// </summary>
    public sealed class KwTrawlDeck : MonoBehaviour
    {
        public const string ShipKey = "bs_catch_ship", PosKey = "bs_catch_pos", RotKey = "bs_catch_rot", FallKey = "bs_catch_fall", AtKey = "bs_catch_at";
        public static readonly int ShipHash = ShipKey.GetStableHashCode(), PosHash = PosKey.GetStableHashCode(), RotHash = RotKey.GetStableHashCode();
        private static readonly int FallHash = FallKey.GetStableHashCode(), AtHash = AtKey.GetStableHashCode();
        private const float LookEvery = 0.5f, KeepEvery = 1f, Metres = 0.5f, Gravity = 9.81f;

        private ZNetView? view;
        private Rigidbody? body;
        private ZSyncTransform? sync;
        private RigidbodyInterpolation smoothing;
        private KwShipRiders? ship;
        private Vector3 local, put;
        private Quaternion turn = Quaternion.identity;
        private long id, at;
        private float fall, nextLook, nextKeep, missing;
        private bool placed, synced;
        private Action? follow;

        /// <summary>On the game that made it: its ZDO marked as riding the ship from where it lands.</summary>
        public static void Mark(ZDO zdo, long ship, Vector3 local, Quaternion turn, float fall, long at)
        {
            zdo.Set(PosHash, local);
            zdo.Set(RotHash, turn);
            zdo.Set(FallHash, fall);
            zdo.Set(AtHash, at);
            zdo.Set(ShipHash, ship);
        }

        private void Awake()
        {
            (view, body, sync) = (GetComponent<ZNetView>(), GetComponent<Rigidbody>(), GetComponent<ZSyncTransform>());
            ZDO? zdo = view != null ? view.GetZDO() : null;
            id = zdo != null ? zdo.GetLong(ShipHash, 0L) : 0L;
            if (zdo == null || id == 0L)
            {
                Destroy(this);
                return;
            }
            (local, turn) = (zdo.GetVec3(PosHash, Vector3.zero), KwShipRiderPose.Unit(zdo.GetQuaternion(RotHash, Quaternion.identity)));
            (fall, at, missing) = (zdo.GetFloat(FallHash), zdo.GetLong(AtHash, 0L), Time.time);
            Hold();
        }

        // Held still: no physics of its own (nor touching the ship's), no synced transform; drawn where the ship has it.
        private void Hold()
        {
            if (body != null)
            {
                smoothing = body.interpolation;
                body.interpolation = RigidbodyInterpolation.None;
                (body.isKinematic, body.detectCollisions) = (true, false);
            }
            if (sync != null && sync.enabled)
                (synced, sync.enabled) = (true, false);
        }

        private void LateUpdate() => KwShipBody.Step("Broadside catch aboard", follow ??= Follow);

        private void Follow()
        {
            if (Time.time >= nextLook)
                Look();
            if (ship == null || ship.Zdo == null)
                return;
            Transform s = ship.transform;
            // moved by the game since this frame's place (drawn toward a player picking it up): kept, in the ship's frame
            if (placed && (transform.position - put).sqrMagnitude > 1e-6f)
                local += KwShipRiderPose.Inverse(KwShipRiderPose.Unit(s.rotation)) * (transform.position - put);
            (Vector3 world, Quaternion rot) = KwShipRiderPose.World(s.position, s.rotation, local + Vector3.up * Height(), turn);
            transform.SetPositionAndRotation(world, rot);
            (put, placed) = (world, true);
        }

        // How high over its place it still is, falling out of the bag.
        private float Height()
        {
            float t = at == 0L || ZNet.instance == null ? 99f : (float)((ZNet.instance.GetTime().Ticks - at) / 1e7);
            return Mathf.Max(0f, fall - 0.5f * Gravity * Mathf.Max(t, 0f) * Mathf.Max(t, 0f));
        }

        // Twice a second: let go by its owner, its ship found, and on its owner its place kept.
        private void Look()
        {
            nextLook = Time.time + LookEvery;
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null)
                return;
            if (zdo.GetLong(ShipHash, 0L) != id)
            {
                Release();
                return;
            }
            if (ship == null || ship.Zdo == null)
                ship = KwShipRiders.Find(id);
            if (ship != null)
                missing = Time.time;
            if (zdo.IsOwner() && Time.time >= nextKeep)
                Keep(zdo);
        }

        // On the owner: its ZDO's place kept with the ship; parked, moved with the ship's ZDO; its ship gone for good, let go.
        private void Keep(ZDO zdo)
        {
            nextKeep = Time.time + KeepEvery;
            if (ship == null && !Parked())
            {
                if (KwShipRiderRules.Released(KwShipRiders.Gone(id), Time.time - missing, AreaReady()))
                {
                    zdo.Set(ShipHash, 0L);
                    Release();
                }
                return;
            }
            if ((transform.position - zdo.GetPosition()).sqrMagnitude > Metres * Metres)
            {
                zdo.SetPosition(transform.position);
                zdo.SetRotation(transform.rotation);
            }
        }

        // With its ship not in this scene: where it would be on the ship as the ship's ZDO near has it; false with none near.
        private bool Parked()
        {
            ZDO? near = KwShipRiderFind.Near(id, transform.position);
            if (near == null)
                return false;
            missing = Time.time;
            (Vector3 world, Quaternion rot) = KwShipRiderPose.World(near.GetPosition(), near.GetRotation(), local, turn);
            transform.SetPositionAndRotation(world, rot);
            put = world;
            return true;
        }

        private bool AreaReady() => ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(transform.position);

        // An ordinary item again where it is: its body and synced transform back.
        private void Release()
        {
            if (body != null)
            {
                (body.isKinematic, body.detectCollisions) = (false, true);
                body.interpolation = smoothing;
            }
            if (sync != null && synced)
                sync.enabled = true;
            Destroy(this);
        }
    }
}
