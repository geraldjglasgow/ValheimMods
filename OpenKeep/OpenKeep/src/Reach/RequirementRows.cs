using System;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace OpenKeep.Reach
{
    /// <summary>
    /// The requirement rows of the crafting panel and the build panel both go through the static
    /// <c>InventoryGui.SetupRequirement</c>, which writes the required amount and blinks it red when the inventory
    /// is short. The postfix shows the amount per Requirement Display, colours the container part, never leaves a
    /// row red when inventory plus storage covers it, and paints the flash after a pull.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    public static class RequirementRows
    {
        [HarmonyPostfix]
        public static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, int quality, int craftMultiplier, bool __result)
        {
            if (!__result || req == null || req.m_resItem == null || player == null || !ReachRules.Active(ReachRules.FromQuality(quality)))
                return;
            Transform amountRoot = elementRoot.Find("res_amount");
            TMP_Text amountText = amountRoot != null ? amountRoot.GetComponent<TMP_Text>() : null;
            if (amountText == null)
                return;
            string name = Requirements.Name(req);
            int need = req.GetAmount(quality) * craftMultiplier;
            int have = player.GetInventory().CountItems(name);
            int stored = ReachCount.InContainers(name, -1, true);
            Show(amountText, name, need, have, stored);
        }

        private static void Show(TMP_Text text, string name, int need, int have, int stored)
        {
            float flash = ReachFlash.Weight(name);
            if (flash > 0f)
            {
                text.text = need.ToString();
                text.color = Color.Lerp(Color.white, ReachSettings.Colour(), flash);
                return;
            }
            int fromInventory = Math.Min(have, need);
            int fromStorage = Math.Min(stored, need - fromInventory);
            if (fromStorage <= 0 || have + stored < need)
                return;
            text.color = Color.white;
            text.text = Format(need, fromInventory, fromStorage);
        }

        private static string Format(int need, int fromInventory, int fromStorage)
        {
            string hex = ReachSettings.ColourHex();
            switch (ReachSettings.Display.Value)
            {
                case RequirementDisplay.Split: return $"{fromInventory} + <color={hex}>{fromStorage}</color>";
                case RequirementDisplay.Total: return $"<color={hex}>{need}</color>";
                default: return need.ToString();
            }
        }
    }
}
