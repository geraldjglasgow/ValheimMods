using System.Collections.Generic;
using HarmonyLib;
using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The tool's click (DECORATE.md, "How it plays"; <c>Player.TryPlacePiece</c> prefix: the tool itself is never placed):
    /// with a layout showing and the aim inside its square, its decorations are placed (all when the player can pay for
    /// all; with Shift, those he can pay for); anywhere else the aimed area is locked and read. Local to this player.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class KwDecorClick
    {
        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (!KwDecorTool.Is(piece))
                return true;
            __result = false;
            Guard.Run("Broadside auto-decorate click", Click, __instance);
            return false;
        }

        private static void Click(Player player)
        {
            if (player != Player.m_localPlayer)
                return;
            KwDecorDriver.Ensure();
            Vector3? aim = KwDecorDriver.Aim;
            if (KwDecorPlacer.Busy)
                Say(player, KwDecorWords.Placing);
            else if (KwDecorSession.Ready && aim != null && KwDecorSession.Read!.Grid.Contains(aim.Value))
                Place(player);
            else if (aim == null)
                Say(player, KwDecorWords.NoAim);
            else if (KwDecorKind.All().Count == 0)
                Say(player, KwDecorWords.NoKinds);
            else
            {
                float yaw = KwDecorAim.Full(aim.Value, KwDecorDriver.AimPiece, KwDecorSession.Half, out Vector3 anchor);
                KwDecorSession.Lock(aim.Value, yaw, anchor);
            }
        }

        private static void Place(Player player)
        {
            List<KwDecorPlan> shown = KwDecorSession.Shown();
            string? station = KwDecorCost.MissingStation(player, shown);
            if (station != null)
            {
                Say(player, $"Needs a {KwDecorWords.Localize(station)} in range");
                return;
            }
            bool some = ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
            List<KwDecorPlan> paid = KwDecorCost.CanPay(player, KwDecorCost.Sum(shown)) ? shown : some ? KwDecorCost.Affordable(player, shown) : new List<KwDecorPlan>();
            if (paid.Count == 0)
            {
                Say(player, some ? "Not enough to pay for any of them" : $"Not enough for all {shown.Count}: Shift + click places those you can pay for");
                return;
            }
            KwDecorPlacer.Start(player, paid, KwDecorSession.Read!.Grid);
            KwDecorSession.Cancel();
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }

    /// <summary>
    /// The tool's driver made the first time the tool is taken in hand (<c>Player.UpdatePlacementGhost</c> prefix), so its
    /// lines show while aiming before any click; never on a dedicated server. Local.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwDecorHold
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance)
        {
            if (__instance == Player.m_localPlayer && KwDecorTool.Held(__instance))
                KwDecorDriver.Ensure();
        }
    }

    /// <summary>
    /// Esc with an area locked cancels it instead of opening the game menu (<c>Menu.Update</c> prefix, the WindowInput
    /// library's way), that frame only. Local.
    /// </summary>
    [HarmonyPatch(typeof(Menu), "Update")]
    public static class KwDecorEscape
    {
        [HarmonyPrefix]
        private static bool Prefix(Menu __instance)
        {
            if (!KwDecorSession.Locked || __instance.m_root == null || __instance.m_root.gameObject.activeSelf || !ZInput.GetKeyDown(KeyCode.Escape))
                return true;
            KwDecorSession.Cancel();
            return false;
        }
    }
}
