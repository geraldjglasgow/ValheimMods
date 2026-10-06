using HarmonyLib;
using OpenKeep.Core;

namespace OpenKeep.Reach
{
    /// <summary>
    /// Before a craft or a placement pays from storage: what the inventory lacks must be in containers this client may
    /// change at once (it owns them, or nobody owns them). A container another client owns pays only once that client
    /// has handed it over (<see cref="HandOver"/>), so it is asked for here, and the craft or placement waits: the
    /// game's craft is refused with a centre message and the player presses again. Pressing Craft asks at once, so the
    /// hand-over is usually in before the craft's progress bar ends. When the materials are gone altogether (taken by
    /// another player since the counts), the game's "missing requirement" is said instead. The check runs in the same
    /// frame as the payment, so what it found is what the payment takes; a craft never completes unpaid.
    /// </summary>
    public static class ReachReady
    {
        public const string Wait = "$ok_reach_wait";

        /// <summary>The last check asked another client for a container (the wait message), rather than finding nothing.</summary>
        private static bool asked;

        /// <summary>The craft the gui is about to make can be paid now; when not, says so (unless quiet) and asks for the chests.</summary>
        public static bool CraftReady(InventoryGui gui, Player player, bool quiet)
        {
            Recipe recipe = gui.m_craftRecipe;
            if (recipe == null || player != Player.m_localPlayer || player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
                return true;
            int quality = gui.m_craftUpgradeItem == null ? 1 : gui.m_craftUpgradeItem.m_quality + 1;
            int multiplier = gui.m_multiCrafting ? gui.m_multiCraftAmount : 1;
            if (!ReachRules.Active(ReachRules.FromQuality(quality)))
                return true;
            asked = false;
            bool ready;
            if (recipe.m_requireOnlyOneIngredient)
            {
                recipe.GetAmount(quality, out int need, out ItemDrop.ItemData single, multiplier);
                ready = single == null || Ready(player.GetInventory(), single.m_shared.m_name, need, single.m_quality);
            }
            else
                ready = Ready(player, recipe.m_resources, quality, multiplier);
            return Said(ready, quiet);
        }

        /// <summary>The piece the player places can be paid now; when not, says so and asks for the chests.</summary>
        public static bool PlaceReady(Player player, Piece piece)
        {
            if (piece == null || player != Player.m_localPlayer || player.PlacementCostDisabled || !ReachRules.Active(ReachRules.FromQuality(0)))
                return true;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
                return true;
            asked = false;
            return Said(Ready(player, piece.m_resources, 0, 1), false);
        }

        private static bool Said(bool ready, bool quiet)
        {
            if (!ready && !quiet)
                Messages.Center(asked ? Wait : "$msg_missingrequirement");
            return ready;
        }

        /// <summary>Every requirement the game's consume path would take (its own filter) can be paid now.</summary>
        private static bool Ready(Player player, Piece.Requirement[] requirements, int qualityLevel, int multiplier)
        {
            CraftingStation station = player.GetCurrentCraftingStation();
            bool ready = true;
            foreach (Piece.Requirement requirement in requirements)
            {
                if (Requirements.Skipped(station, requirement))
                    continue;
                int amount = requirement.GetAmount(qualityLevel) * multiplier;
                if (amount > 0 && !Ready(player.GetInventory(), Requirements.Name(requirement), amount, -1))
                    ready = false;
            }
            return ready;
        }

        /// <summary>
        /// The inventory plus the containers that can be changed now hold the amount (the payment's world level rule);
        /// when they do not, every other reachable container holding the item is asked for.
        /// </summary>
        private static bool Ready(Inventory inventory, string name, int amount, int quality)
        {
            int shortfall = amount - inventory.CountItems(name, quality, true);
            if (shortfall <= 0)
                return true;
            int covered = 0;
            foreach (Container container in ReachCount.Containers())
            {
                if (ContainerScan.CanClaimNow(container))
                    covered += ReachCount.CountIn(container, name, quality, true);
                if (covered >= shortfall)
                    return true;
            }
            foreach (Container container in ReachCount.Containers())
            {
                if (ContainerScan.CanClaimNow(container) || ReachCount.CountIn(container, name, quality, true) <= 0)
                    continue;
                HandOver.Ask(container, HandOver.ActionSeconds);
                asked = true;
            }
            return false;
        }
    }

    /// <summary>Craft pressed: the chests the craft will pay from are asked for now, while its progress bar runs.</summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
    public static class CraftPressedAskPatch
    {
        [HarmonyPostfix]
        public static void Postfix(InventoryGui __instance)
        {
            if (__instance.m_craftTimer >= 0f && Player.m_localPlayer != null)
                ReachReady.CraftReady(__instance, Player.m_localPlayer, quiet: true);
        }
    }

    /// <summary>A placement whose materials sit in chests another client still owns waits (the game places nothing).</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class PlaceReadyPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        public static bool Prefix(Player __instance, Piece piece, ref bool __result, bool __runOriginal)
        {
            if (!__runOriginal || ReachReady.PlaceReady(__instance, piece))
                return __runOriginal;
            __result = false;
            return false;
        }
    }
}
