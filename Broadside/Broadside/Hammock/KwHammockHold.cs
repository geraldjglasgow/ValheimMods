using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hammock's hooks are held by what they are fixed into (the user, 2026-11-03, a picture falling when its wall goes):
    /// on the hammock's owner (a player's game or a dedicated server), once a second, each hook's point on its surface is
    /// looked at for a solid thing that is not the hammock (a piece, a rock, a tree, the ground, a ship's hull, beam, wall or
    /// mast); with one gone on two looks running, the hammock comes down the game's way (<c>WearNTear.ApplyDamage</c> past its
    /// health: the game's destroy, its effect and its recipe dropped as a broken piece's is). Posts stand on their own (the
    /// game's support holds the whole piece up by its colliders, as any piece's). Never in its first
    /// <see cref="Settle"/> seconds (nor that long after it boards or leaves a ship), nor while the area round it is still
    /// loading; aboard only on its ship's own pose (the colliders a step ahead of where the ship is drawn) once the ship is
    /// built; a rider parked away from its ship never comes down. Nothing is sent but the game's own destroy.
    /// </summary>
    public sealed class KwHammockHold
    {
        private const float Settle = 10f, Reach = 0.06f, ReachAboard = 0.12f;
        private const int Strikes = 2;
        private static readonly Collider[] touching = new Collider[16];
        private float since = float.NaN;
        private bool wasAboard;
        private int strikes;

        public void Tick(KwHammock hammock, ZDO zdo)
        {
            KwShipRiders? ship = hammock.Rider != null ? hammock.Rider.On : null;
            bool aboard = ship != null;
            if (float.IsNaN(since) || aboard != wasAboard)
                (since, wasAboard, strikes) = (Time.time, aboard, 0);
            if (!hammock.Spec.Hooks || !zdo.IsOwner() || Time.time - since < Settle || !Ready(hammock, ship))
            {
                strikes = 0;
                return;
            }
            strikes = Held(hammock, ship) ? 0 : strikes + 1;
            if (strikes >= Strikes)
                Fall(hammock, zdo);
        }

        // The area round it loaded and, aboard, its ship built; a rider whose ship is not here is never looked at.
        private static bool Ready(KwHammock hammock, KwShipRiders? ship)
        {
            if (hammock.Rider != null && ship == null)
                return false;
            KwShipBody? body = ship != null ? ship.GetComponent<KwShipBody>() : null;
            return (body == null || body.Built) && ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(hammock.transform.position);
        }

        private static bool Held(KwHammock hammock, KwShipRiders? ship)
        {
            KwHammockSpec spec = hammock.Spec;
            return Fixed(hammock.transform, spec.A, ship) && Fixed(hammock.transform, spec.B, ship);
        }

        // A post, or a hook with something solid at its point that is not the hammock itself.
        private static bool Fixed(Transform hammock, KwHammockMount end, KwShipRiders? ship)
        {
            if (end.Post)
                return true;
            int count = Physics.OverlapSphereNonAlloc(Where(hammock, end, ship), ship != null ? ReachAboard : Reach, touching, KwHammockAim.Solid, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (touching[i] != null && !touching[i].transform.IsChildOf(hammock))
                    return true;
            }
            return false;
        }

        // A hook's point on its surface in the world; aboard carried to the ship's body pose, where its colliders are.
        private static Vector3 Where(Transform hammock, KwHammockMount end, KwShipRiders? ship)
        {
            Vector3 at = hammock.TransformPoint(KwMesh.ToUnity(end.Base));
            if (ship == null)
                return at;
            (Vector3 bodyAt, Quaternion bodyTurn) = ship.Pose;
            return KwShipRiderPose.Carry(ship.transform.position, ship.transform.rotation, bodyAt, bodyTurn, at);
        }

        // Down the game's way: damage past its health, so the game destroys it and drops its recipe as for a broken piece.
        private static void Fall(KwHammock hammock, ZDO zdo)
        {
            Log.Info("Broadside: a hammock came down; what held one of its hooks is gone.");
            WearNTear? wear = hammock.GetComponent<WearNTear>();
            if (wear == null)
            {
                ZNetScene.instance.Destroy(hammock.gameObject);
                return;
            }
            float health = zdo.GetFloat(ZDOVars.s_health, wear.m_health);
            wear.ApplyDamage(Mathf.Max(health, 1f) + 1f);
        }
    }
}
