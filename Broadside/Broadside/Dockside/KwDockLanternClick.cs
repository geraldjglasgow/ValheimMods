using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Placing a dock lantern with the hammer (DOCKS.md, "Dock lanterns"), on the placing player's game. While it is in hand
    /// (<see cref="Hover"/>, from <see cref="KwDockPlace"/>) its ghost stands where the aim's site puts it
    /// (<see cref="KwDockLanternAim"/>: just off a dock's side, pointing in), its post shown at its length in the dock's wood,
    /// and the piece's requirements are that site's cost, so the build panel shows it and the game takes it at the click as
    /// any piece's; a site refused turns the ghost red and says why in the middle of the screen when that changes. The click
    /// (a <c>Player.TryPlacePiece</c> prefix) says why when refused instead of the game's "invalid placement"; otherwise the
    /// game places the piece where the ghost stands, and this postfix writes its post (length, material, seed, what it
    /// stands beside) to the new piece's ZDO and builds it in the same frame (<see cref="KwDockLanternPiece.Take"/>). Every
    /// peer builds the rest from that.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class KwDockLanternClick
    {
        private static KwDockLanternSite? last;
        private static (string Key, Piece.Requirement[] Cost) shown = ("", new Piece.Requirement[0]);
        private static int seed = NewSeed();
        private static bool dressed;

        /// <summary>The ghost at the aim's site (or red, and why), its post's look, the piece's requirements its cost.</summary>
        public static void Hover(Player player, GameObject ghost, Piece piece)
        {
            KwDockLanternSite site = last = KwDockLanternAim.Of(player, false);
            KwLadderTool.Told(site.Why ?? "");
            if (site.Why != null)
            {
                Rest();
                player.m_placementStatus = Player.PlacementStatus.Invalid;
                player.SetPlacementGhostValid(false);
                return;
            }
            ghost.transform.SetPositionAndRotation(site.Origin, site.Turn);
            KwDockLanternLook.Ghost(ghost.transform, site.Kind, site.Length, seed);
            piece.m_resources = Cost(site);
            dressed = true;
            player.m_placementStatus = Player.PlacementStatus.Valid;
            player.SetPlacementGhostValid(true);
        }

        /// <summary>The piece's requirements back to its resting cost (a plain wood post's) once another piece, or none, is in hand.</summary>
        public static void Rest()
        {
            if (!dressed)
                return;
            dressed = false;
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(KwDockLanternPiece.Prefab) : null;
            if (prefab != null && ObjectDB.instance != null)
                prefab.GetComponent<Piece>().m_resources = new KwDockLanternSpec(0f, KwPierKinds.Wood, 0, true).Requirements(true);
        }

        // One array per site's cost, kept while the cost stays the same.
        private static Piece.Requirement[] Cost(KwDockLanternSite site)
        {
            KwDockLanternSpec spec = site.Spec(seed);
            string key = $"{spec.Kind.Id}|{spec.Wood}";
            if (shown.Key != key)
                shown = (key, spec.Requirements(true));
            return shown.Cost;
        }

        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (piece == null || piece.name != KwDockLanternPiece.Prefab)
                return true;
            string? why = Guard.Run<Player, string?>("Broadside dock lantern", static p => KwDockLanternAim.Of(p, true).Why, __instance);
            if (why == null)
                return true;
            KwLadderTool.Quiet();
            __instance.Message(MessageHud.MessageType.Center, why);
            __result = false;
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance, Piece piece, bool __result)
        {
            if (!__result || piece == null || piece.name != KwDockLanternPiece.Prefab)
                return;
            Guard.Run("Broadside dock lantern", Placed, __instance);
        }

        // The piece the game just placed given its post; the next lantern a new seed.
        private static void Placed(Player player)
        {
            KwDockLanternSite? site = last;
            Piece? placed = KwPierBuild.Newest(KwDockLanternPiece.Prefab, player.GetPlayerID());
            if (site == null || site.Why != null || placed == null)
            {
                Log.Warn("Broadside: a dock lantern was placed without its site; it keeps a short post.");
                return;
            }
            KwDockLanternPiece.Take(placed.gameObject, site.Spec(seed));
            seed = NewSeed();
        }

        private static int NewSeed() => (int)((uint)System.Environment.TickCount * 2654435761u >> 1) | 1;
    }
}
