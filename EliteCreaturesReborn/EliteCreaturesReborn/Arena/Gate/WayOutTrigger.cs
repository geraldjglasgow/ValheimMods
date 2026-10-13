using EliteCreaturesReborn.Arena.Travel;
using EliteCreaturesReborn.Patches;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// The trigger at the hall's way out (<see cref="GateWayOut"/>), on the walker's own machine: the local player who walks
    /// into it is jumped back through <see cref="ArenaTravel.Leave"/> to the point kept as they came in - in front of the
    /// Helgrind they used, facing away from it, or where they stood for `elite arena` - with the game's loading, or to their
    /// bed or the world's spawn when nothing was kept. Never refused: nobody is kept in the hall.
    /// </summary>
    internal sealed class WayOutTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            Player? player = other.GetComponent<Player>();
            if (player == null || player != Player.m_localPlayer || player.IsTeleporting())
            {
                return;
            }
            SafeCall.Run("Helgrind's way out", static walker => ArenaTravel.Leave(walker, out _), player);
        }
    }
}
