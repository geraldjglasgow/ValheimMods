using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's count of who is aboard a launched ship kept true (a player's report, 2026-11-09: a ship left behind "cannot
    /// be dismantled anymore, the game says there is still someone on board (there is not) and the ship speedometer stays on
    /// screen even when going far away from it"). The game counts a player aboard on its onboard trigger's enter and lets
    /// him go on its exit (<c>Ship.m_players</c>, and for the local player <c>Ship.s_currentShips</c>, which the ship panel,
    /// the break-up rule, the save aboard and the brake with nobody aboard read); Unity sends no exit for a player who dies,
    /// logs out, teleports or is unloaded inside it, nor for a trigger destroyed round him, and the game tidies only on the
    /// ship's owner while he is not aboard himself. On every peer once a second (and before each break-up and save check) an
    /// entry that is no live player in the onboard box (<see cref="Margin"/> round it), or a second entry of one, goes, and
    /// the ship leaves the local list when the local player has; nobody inside is ever taken off, so the game's later exits
    /// stay balanced. Only once the ship is built, when its trigger has its size (<see cref="KwShipFittings"/>).
    /// </summary>
    public sealed class KwShipAboard : MonoBehaviour
    {
        // Round the box: the trigger meets a player's capsule, from his feet (his position) up to his head.
        private const float Margin = 2f, Every = 1f;

        private Ship? ship;
        private float next;

        private void Awake() => ship = GetComponent<Ship>();

        private void Update()
        {
            if (Time.time < next || ship == null)
                return;
            next = Time.time + Every;
            Guard.Run("Broadside ship aboard", Prune, ship);
        }

        /// <summary>The ship's onboard list (and the local player's ship list) left with the live players in its onboard box.</summary>
        public static void Prune(Ship ship)
        {
            Transform? onboard = ship.transform.Find("OnboardTrigger");
            BoxCollider? box = onboard != null ? onboard.GetComponent<BoxCollider>() : null;
            if (box == null || ship.GetComponent<KwSeaShip>() == null)
                return;
            List<Player> players = ship.m_players;
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (!Inside(players[i], box) || players.IndexOf(players[i]) < i)
                    players.RemoveAt(i);
            }
            if (Player.m_localPlayer == null || players.Contains(Player.m_localPlayer))
                return;
            while (Ship.s_currentShips.Remove(ship))
            {
            }
        }

        // A live player in this game whose feet are in the box, or no more than Margin outside it.
        private static bool Inside(Player? player, BoxCollider box)
        {
            if (player == null || player.m_nview == null || !player.m_nview.IsValid() || player.IsDead())
                return false;
            Transform frame = box.transform;
            Vector3 local = frame.InverseTransformPoint(player.transform.position) - box.center, scale = frame.lossyScale, half = box.size / 2f;
            return Out(local.x, half.x, scale.x) <= Margin && Out(local.y, half.y, scale.y) <= Margin && Out(local.z, half.z, scale.z) <= Margin;
        }

        // How far outside the box a point is along one of its axes, in metres (negative inside).
        private static float Out(float at, float half, float scale) => (Mathf.Abs(at) - half) * Mathf.Abs(scale);
    }
}
