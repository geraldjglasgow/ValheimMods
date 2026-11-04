using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A Broadside piece's placement ghost is placed by its origin, like the game's pieces on flat ground: the game moves a
    /// ghost so the point of its colliders nearest the aim sits on the aim, and a piece's body box (a pile's, a crate's, the
    /// blueprint table's legs) put that point at a corner, half the piece away, jumping from corner to corner as the ground
    /// or a rocking deck tilted. Once the game has made the ghost, every collider on it but the placing box
    /// (<see cref="KwPrefabs.Anchor"/>) is switched off. Only the ghost: a built piece keeps all its colliders. Local to the
    /// placing player; a ghost without a placing box is left alone.
    /// </summary>
    [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
    public static class KwGhostPlace
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside ghost placing box", Keep, __instance);

        private static void Keep(Player player)
        {
            GameObject? ghost = player.m_placementGhost;
            if (ghost == null)
                return;
            Collider[] colliders = ghost.GetComponentsInChildren<Collider>(true);
            if (!System.Array.Exists(colliders, IsPlace))
                return;
            foreach (Collider collider in colliders)
            {
                if (!IsPlace(collider))
                    collider.enabled = false;
            }
        }

        private static bool IsPlace(Collider collider) => collider.gameObject.name == KwPrefabs.PlaceName;
    }
}
