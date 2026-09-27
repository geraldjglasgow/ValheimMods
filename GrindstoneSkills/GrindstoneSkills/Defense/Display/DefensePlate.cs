using HarmonyLib;
using PlateColumn;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Defense on the inventory screen: a plate in the player panel's stat column (the PlateColumn library, shared with
    /// our other mods), just under the game's armour plate, showing the Defense icon and the damage reduction ("-10%"),
    /// with every Defense bonus at the player's level in its tooltip (<see cref="DefenseSummary"/>). The game writes the
    /// weight (InventoryGui.UpdateInventoryWeight) every frame the inventory is open and only then, so the postfix there
    /// runs exactly while the plate can be seen. The plate hides, closing its gap, while Defense is off or the player's
    /// "Show Plate" is. The tooltip is rewritten at most once a second, when it changed.
    /// </summary>
    public static class DefensePlate
    {
        private const string Id = "grindstone_defense";
        private const int Rank = Column.ArmorRank + 50;
        private const float TipInterval = 1f;

        private static Plate plate;
        private static InventoryGui failedOn;
        private static string shownText;
        private static string shownTip;
        private static float tipCheckedAt = float.NegativeInfinity;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventoryWeight))]
        private static class Weight
        {
            [HarmonyPostfix]
            private static void Postfix(InventoryGui __instance, Player player) =>
                HookGuard.Run("defense plate", () => Refresh(__instance, player));
        }

        private static void Refresh(InventoryGui gui, Player player)
        {
            bool show = DefenseSettings.ShowPlate.Value && DefenseSkill.Active && DefenseSkill.IsLocal(player);
            Plate current = Current(gui, show);
            if (current == null)
                return;
            GameObject go = current.Rect.gameObject;
            if (go.activeSelf != show)
            {
                go.SetActive(show);
                Column.Arrange(gui);
            }
            if (!show)
                return;
            WriteText(player);
            WriteTip(gui);
        }

        /// <summary>The plate made on this panel, else a new one when there is something to show; tried once per panel.</summary>
        private static Plate Current(InventoryGui gui, bool show)
        {
            if (plate != null && plate.Rect)
                return plate;
            if (!show || failedOn == gui)
                return null;
            shownText = null;
            shownTip = DefenseSummary.Tip();
            plate = Column.Add(gui, Spec(shownTip));
            if (plate?.Text == null)
            {
                failedOn = gui;
                plate = null;
                GrindstoneSkills.Log.LogWarning("the Defense plate could not be added: the inventory's armor or weight plate is missing");
            }
            return plate;
        }

        private static PlateSpec Spec(string tip) => new PlateSpec(Id, Rank, DefenseIcon.Find(), true, DefenseSummary.Topic(), tip);

        private static void WriteText(Player player)
        {
            int percent = Mathf.RoundToInt(Reductions.Toughness(player) * 100f);
            string text = percent > 0 ? $"-{percent}%" : "0%";
            if (text == shownText)
                return;
            plate.Text.text = text;
            shownText = text;
        }

        private static void WriteTip(InventoryGui gui)
        {
            if (Time.unscaledTime - tipCheckedAt < TipInterval)
                return;
            tipCheckedAt = Time.unscaledTime;
            string tip = DefenseSummary.Tip();
            if (tip == shownTip)
                return;
            shownTip = tip;
            Column.Add(gui, Spec(tip));
        }
    }
}
