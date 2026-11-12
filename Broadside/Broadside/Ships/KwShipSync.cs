using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship kept where its owner has it, on the games that do not own it, through its body (a player's report,
    /// 2026-11-08 to 11-11: frame drops in multiplayer only, on one player at a time, passed round "like a hot potato").
    /// The game moves an object another player owns by writing its transform's place and turn each frame it differs
    /// (<c>ZSyncTransform.ClientSync</c>), and when it becomes this game's own (<c>OwnerSync</c>). Written that way, Unity
    /// works out every collider of the body again against all the others: on a game ship's dozen nothing, on a launched
    /// ship's thousands of boxes 2.6 ms a frame at 2300 colliders, 9 ms at 4200, 46 ms at 8500, and twice that when it
    /// turns as well (measured in Unity 6000.0.75f1, 2026-11-11); the owner's game, which moves the ship by its physics,
    /// pays nothing, so a player alone never saw it. Every frame a ship rode a wave, sailed or was shoved by a creature,
    /// every other player near paid it, and whoever was handed the ship stopped paying.
    /// Here the same easing is done on the body (<c>Rigidbody.MovePosition</c>, <c>MoveRotation</c>: 0.3 ms at 8500
    /// colliders), just before the game's own step, which then finds this frame done; the speeds, the sleep and the far
    /// jump are the game's as they were, and the ship's becoming this game's own is taken the same way. Nothing is sent
    /// or saved; a ship whose sync is set another way (kinematic, scaled) is left to the game.
    /// </summary>
    public sealed class KwShipSync : MonoBehaviour
    {
        private static readonly List<KwShipSync> Ships = new List<KwShipSync>();

        private ZNetView? view;
        private ZSyncTransform? sync;
        private Rigidbody? body;

        /// <summary>Whether any launched ship is loaded (one test a step when none is).</summary>
        public static bool Any => Ships.Count != 0;

        private void Awake() => (view, sync, body) = (GetComponent<ZNetView>(), GetComponent<ZSyncTransform>(), GetComponent<Rigidbody>());

        private void OnEnable() => Ships.Add(this);

        private void OnDisable() => Ships.Remove(this);

        /// <summary>Before the game's physics step: every loaded ship another player owns eased to its owner's place.</summary>
        public static void Follow()
        {
            for (int i = 0; i < Ships.Count; i++)
                Ships[i].Ease(Time.fixedDeltaTime);
        }

        /// <summary>Before the game's late update: every loaded ship that has just become this game's own put where its owner left it.</summary>
        public static void Hand()
        {
            for (int i = 0; i < Ships.Count; i++)
                Ships[i].Take();
        }

        // The ship's network data when the game would sync it the plain way (place and turn, a body of its own forces).
        private bool Plain(out ZDO zdo)
        {
            zdo = null!;
            if (view == null || sync == null || body == null || !view.IsValid() || !sync.enabled || body.isKinematic)
                return false;
            zdo = view.GetZDO();
            return sync.m_syncPosition && sync.m_syncRotation && !sync.m_syncScale && !sync.m_characterParentSync && !sync.m_isKinematicBody;
        }

        // Once a frame, as the game's own step, which finds the frame taken and does nothing.
        private void Ease(float dt)
        {
            if (!Plain(out ZDO zdo) || zdo.IsOwner() || sync!.m_lastUpdateFrame == Time.frameCount)
                return;
            sync.m_lastUpdateFrame = Time.frameCount;
            Place(zdo, dt);
            Quaternion turn = zdo.GetRotation(), now = body!.rotation;
            if (Quaternion.Angle(now, turn) > 0.001f)
                body.MoveRotation(Quaternion.Slerp(now, turn, 0.5f));
            Speed(zdo);
        }

        // The owner's place, run on by its speed since it was last heard from: eased to, or jumped to from 5 m off.
        private void Place(ZDO zdo, float dt)
        {
            Vector3 to = zdo.GetPosition(), at = body!.position;
            if (zdo.DataRevision != sync!.m_posRevision)
                (sync.m_posRevision, sync.m_targetPosTimer) = (zdo.DataRevision, 0f);
            if (zdo.HasOwner())
            {
                sync.m_targetPosTimer = Mathf.Min(sync.m_targetPosTimer + dt, 2f);
                to += zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero) * sync.m_targetPosTimer;
            }
            float away = Vector3.Distance(at, to);
            if (away >= 5f)
                body.position = to;
            else if (away > 0.001f)
                body.MovePosition(Vector3.Lerp(at, to, 0.2f));
        }

        // The body carried at its owner's speeds with no weight of its own, or asleep when its owner's lies still.
        private void Speed(ZDO zdo)
        {
            body!.useGravity = false;
            bool owned = sync!.m_syncBodyVelocity && zdo.HasOwner();
            Vector3 speed = zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero), spin = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
            if (owned && (speed.magnitude > 0.01f || spin.magnitude > 0.01f))
                (body.linearVelocity, body.angularVelocity) = (speed, spin);
            else if (owned)
                body.Sleep();
            else if (!body.IsSleeping())
            {
                (body.linearVelocity, body.angularVelocity) = (Vector3.zero, Vector3.zero);
                body.Sleep();
            }
        }

        // The ship just handed to this game: the game would write its transform; the body is put there instead, and the
        // game finds the hand-over done.
        private void Take()
        {
            if (!Plain(out ZDO zdo) || !zdo.IsOwner() || sync!.m_wasOwner)
                return;
            sync.m_wasOwner = true;
            (body!.position, body.rotation) = (zdo.GetPosition(), zdo.GetRotation());
            if (sync.m_syncBodyVelocity)
                (body.linearVelocity, body.angularVelocity) = (zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero), zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero));
        }
    }

    /// <summary>
    /// The game's own update loops (<c>MonoUpdaters</c>, once a physics step and once a frame, not once an object) given a
    /// first step for launched ships (<see cref="KwShipSync"/>); with none loaded, one test each.
    /// </summary>
    [HarmonyPatch(typeof(MonoUpdaters))]
    public static class KwShipSyncPatches
    {
        private static readonly Action Follow = KwShipSync.Follow, Hand = KwShipSync.Hand;

        [HarmonyPatch("FixedUpdate"), HarmonyPrefix]
        private static void Fixed()
        {
            if (KwShipSync.Any)
                Guard.Run("Broadside ship sync", Follow);
        }

        [HarmonyPatch("LateUpdate"), HarmonyPrefix]
        private static void Late()
        {
            if (KwShipSync.Any)
                Guard.Run("Broadside ship hand-over", Hand);
        }
    }
}
