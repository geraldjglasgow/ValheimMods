using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Crowd
{
    /// <summary>
    /// The other players this PC has loaded, taken from the game's own list (Player.GetAllPlayers), each with its
    /// <see cref="FarPlayer"/> record found by the character's instance id. Only another peer's character counts: never
    /// the local player and never a character this PC owns (its owner drives it, root motion and foot steps included).
    /// A record the game no longer lists (the player left, died, went out of range) or that this PC took over is put
    /// back in full first, then dropped, so nothing outlives its player. The list and the lookup are reused; nothing is
    /// allocated per frame.
    /// </summary>
    internal static class CrowdList
    {
        private static readonly List<FarPlayer> Tracked = new List<FarPlayer>();
        private static readonly Dictionary<int, FarPlayer> ById = new Dictionary<int, FarPlayer>();
        private static int _round;

        public static int Count => Tracked.Count;

        public static FarPlayer At(int index) => Tracked[index];

        /// <summary>Every other player listed now, with its distance to the nearer of the local player and the camera.</summary>
        public static void Refresh(Vector3 body, Vector3 eye, float now)
        {
            _round++;
            List<Player> players = Player.GetAllPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (!IsOther(player))
                {
                    continue;
                }
                FarPlayer far = Track(player, now);
                Vector3 at = player.transform.position;
                far.SqrDistance = Mathf.Min((at - body).sqrMagnitude, (at - eye).sqrMagnitude);
                far.Seen = _round;
            }
            Sweep();
        }

        private static bool IsOther(Player player) => player != null && !ReferenceEquals(player, Player.m_localPlayer) &&
            player.m_nview != null && player.m_nview.IsValid() && !player.m_nview.IsOwner();

        private static FarPlayer Track(Player player, float now)
        {
            int id = player.GetInstanceID();
            if (!ById.TryGetValue(id, out FarPlayer far))
            {
                far = new FarPlayer(player, now);
                ById.Add(id, far);
                Tracked.Add(far);
            }
            return far;
        }

        private static void Sweep()
        {
            for (int i = Tracked.Count - 1; i >= 0; i--)
            {
                FarPlayer far = Tracked[i];
                if (far.Seen != _round)
                {
                    far.Restore(0f);
                    ById.Remove(far.Id);
                    Tracked[i] = Tracked[Tracked.Count - 1];
                    Tracked.RemoveAt(Tracked.Count - 1);
                }
            }
        }

        /// <summary>Every frame: the stepped animators owed a step are updated (<see cref="SteppedAnimator"/>).</summary>
        public static void StepAll(float dt)
        {
            for (int i = 0; i < Tracked.Count; i++)
            {
                if (Tracked[i].Stepping)
                {
                    SteppedAnimator.Step(Tracked[i], dt);
                }
            }
        }

        /// <summary>The record of the player wearing this equipment, or null (a creature, an armour stand, the local player).</summary>
        public static FarPlayer ByVis(VisEquipment vis)
        {
            for (int i = 0; i < Tracked.Count; i++)
            {
                if (ReferenceEquals(Tracked[i].Vis, vis))
                {
                    return Tracked[i];
                }
            }
            return null;
        }

        /// <summary>Everyone back in full and forgotten: the setting turned off, or the game left.</summary>
        public static void RestoreAll()
        {
            if (Tracked.Count == 0)
            {
                return;
            }
            for (int i = 0; i < Tracked.Count; i++)
            {
                Tracked[i].Restore(0f);
            }
            Tracked.Clear();
            ById.Clear();
        }
    }
}
