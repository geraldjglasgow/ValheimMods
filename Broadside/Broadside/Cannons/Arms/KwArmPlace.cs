using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a ship cannon may be built: only on a launched Broadside ship's deck (the user's words, 2026-10-26: "build
    /// things like cannons and the weapon mounts on the ship after it is built, and not while it is being built on the
    /// keel guide with keel blocks"; and on graduating into Broadside: "you should be allowed to build cannons on your
    /// ship"), where it then rides the ship (<see cref="KwShipRider"/>). The ghost stands on the ship when the local
    /// player's placement ray hits a launched ship this frame (<see cref="KwShipRiderAim.Ship"/>); anywhere else it is
    /// refused, and trying to place it says why (<see cref="KwArmPlaceMessage"/>): on a Broadside dock's design parts the
    /// ship is not launched yet (<see cref="KwArmWords.Dock"/>), elsewhere (the ground, the game's floors)
    /// <see cref="KwArmWords.Afloat"/>. The game's own ships never take a ghost (the game drops placement hits on a body).
    /// Runs after the game has placed the ghost and found it valid; only the cannons' own ghosts are looked at.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwArmPlace
    {
        private const float Above = 0.5f, Below = 3f;
        private static readonly RaycastHit[] hits = new RaycastHit[16];
        private static readonly IComparer<RaycastHit> Nearer = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
        private static int mask;

        /// <summary>Why the cannon ghost was refused this frame, for the message when the player tries to place it; null when it was not.</summary>
        public static string? Refusal { get; private set; }

        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside cannon placement", Fit, __instance);

        private static void Fit(Player player)
        {
            if (player != Player.m_localPlayer)
                return;
            Refusal = null;
            GameObject? ghost = player.m_placementGhost;
            Piece? selected = ghost != null && ghost.activeSelf ? player.GetSelectedPiece() : null;
            if (selected == null || player.m_placementStatus != Player.PlacementStatus.Valid || KwArmory.OfPrefab(selected.name) == null)
                return;
            if (KwShipRiderAim.Ship != null)
                return;
            Refusal = OnDock(ghost!) ? KwArmWords.Dock : KwArmWords.Afloat;
            player.m_placementStatus = Player.PlacementStatus.Invalid;
            player.SetPlacementGhostValid(false);
        }

        // Whether the first solid thing under the ghost is one of a Broadside dock's pieces (the ship not launched yet).
        private static bool OnDock(GameObject ghost)
        {
            mask = mask != 0 ? mask : LayerMask.GetMask("piece", "Default", "static_solid", "terrain");
            Vector3 from = ghost.transform.position + Vector3.up * Above;
            int count = Physics.RaycastNonAlloc(from, Vector3.down, hits, Above + Below, mask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, 0, count, Nearer);
            for (int i = 0; i < count; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(ghost.transform))
                    continue;
                Piece? piece = collider.GetComponentInParent<Piece>();
                return piece != null && Utils.GetPrefabName(piece.gameObject).StartsWith("KW_", StringComparison.Ordinal);
            }
            return false;
        }
    }

    /// <summary>The reason a cannon ghost was refused, said when the player tries to place it (after the game's own "invalid placement").</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class KwArmPlaceMessage
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool __result)
        {
            if (!__result && KwArmPlace.Refusal != null && __instance == Player.m_localPlayer)
                __instance.Message(MessageHud.MessageType.Center, KwArmPlace.Refusal);
        }
    }
}
