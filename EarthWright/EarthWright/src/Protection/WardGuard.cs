using System.Collections.Generic;
using EarthWright.Terrain;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// Wards over the whole footprint ("Respect Wards"). An active ward blocks an edit when any part of the edit reaches
    /// into it and the sending player is neither its creator nor on its permitted list; both come from the ward's ZDO,
    /// so every machine gives the same answer for the same player.
    /// <list type="bullet">
    /// <item>Sender (the player's machine): tests the local player; when the check comes from the player's own click, the
    /// blocking wards flash as they do when the game refuses a placement.</item>
    /// <item>Owner (the machine that owns the terrain compiler): tests <see cref="TerrainEdit.SenderPlayer"/> against the
    /// wards loaded there.</item>
    /// </list>
    /// </summary>
    public static class WardGuard
    {
        public static string Sender(GuardContext ctx)
        {
            Player player = Player.m_localPlayer;
            if (!ProtectionSettings.RespectWards.Value || player == null)
                return null;
            List<PrivateArea> blocking = Blocking(Footprint.Of(ctx.Edit), player.GetPlayerID());
            if (blocking.Count == 0)
                return null;
            if (PlaceWindow.Now)
            {
                foreach (PrivateArea area in blocking)
                    area.FlashShield(false);
            }
            return ProtectionWords.Ward;
        }

        public static string Owner(GuardContext ctx)
        {
            if (!ProtectionSettings.RespectWards.Value)
                return null;
            return Blocking(Footprint.Of(ctx.Edit), ctx.Edit.SenderPlayer).Count > 0 ? ProtectionWords.Ward : null;
        }

        /// <summary>The active wards this player has no access to that any disc reaches into.</summary>
        public static List<PrivateArea> Blocking(List<Disc> discs, long playerId)
        {
            List<PrivateArea> result = new List<PrivateArea>();
            if (discs.Count == 0)
                return result;
            foreach (PrivateArea area in PrivateArea.m_allAreas)
            {
                if (IsActive(area) && !HasAccess(area, playerId) && Overlaps(area, discs))
                    result.Add(area);
            }
            return result;
        }

        private static bool IsActive(PrivateArea area)
        {
            return area != null && area.m_nview != null && area.m_nview.IsValid() && area.IsEnabled();
        }

        /// <summary>The same rule as the game's own access check, for any player: the creator or a permitted player.</summary>
        private static bool HasAccess(PrivateArea area, long playerId)
        {
            if (playerId == 0L)
                return false;
            if (area.m_piece != null && area.m_piece.GetCreator() == playerId)
                return true;
            return area.IsPermitted(playerId);
        }

        private static bool Overlaps(PrivateArea area, List<Disc> discs)
        {
            foreach (Disc disc in discs)
            {
                if (area.IsInside(disc.Center, disc.Radius))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Marks the frame in which the local player tries to place a piece, so the ward check knows the refusal answers a
    /// real click (flash the wards) rather than a preview check several times a second (stay quiet).
    /// </summary>
    public static class PlaceWindow
    {
        internal static int Frame = -1;

        /// <summary>The local player is placing (clicking) in this frame.</summary>
        public static bool Now => Frame == Time.frameCount;
    }

    /// <summary>Runs before every other TryPlacePiece prefix (EarthWright's placement hook included).</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class PlaceWindowPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(Player __instance)
        {
            if (__instance != null && __instance == Player.m_localPlayer)
                PlaceWindow.Frame = Time.frameCount;
        }
    }
}
