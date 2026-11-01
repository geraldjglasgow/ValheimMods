using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Where the players are this frame, for <see cref="AnimRule"/>: every player character this machine has loaded (the
    /// server loads every player's area, so all of them; on a hosted game the host's own too) and, on a hosted game, the
    /// host's camera, round which the game plays a creature's footsteps on its owner. Gathered once a frame into a reused
    /// list; asked by squared distance. A dedicated server has no camera of its own to count.
    /// </summary>
    internal static class AnimPlayers
    {
        private static readonly List<Vector3> Spots = new List<Vector3>();
        private static Vector3 _camera;
        private static bool _haveCamera;
        private static int _frame = -1;

        /// <summary>Gathers this frame's positions (once a frame, however often asked).</summary>
        public static void Refresh()
        {
            if (_frame == Time.frameCount)
            {
                return;
            }
            _frame = Time.frameCount;
            Spots.Clear();
            List<Player> players = Player.GetAllPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                {
                    Spots.Add(players[i].transform.position);
                }
            }
            Camera camera = ZNet.instance.IsDedicated() ? null : Utils.GetMainCamera();
            _haveCamera = camera != null;
            _camera = _haveCamera ? camera.transform.position : Vector3.zero;
        }

        /// <summary>True when a player is within <paramref name="range"/> metres of <paramref name="point"/>.</summary>
        public static bool Near(Vector3 point, float range)
        {
            float squared = range * range;
            for (int i = 0; i < Spots.Count; i++)
            {
                if ((Spots[i] - point).sqrMagnitude <= squared)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>True on a hosted game when the host's camera is within <paramref name="range"/> metres of the point.</summary>
        public static bool CameraNear(Vector3 point, float range) => _haveCamera && (_camera - point).sqrMagnitude <= range * range;
    }
}
