using System;
using HarmonyLib;
using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Maker's Mark line in an item's tooltip, on the client that shows it. The static ItemData.GetTooltip builds
    /// every item tooltip before localization: inventories and containers (the hovered slot, every frame) with crafting
    /// off, and the crafting panel's recipe (InventoryGui.UpdateRecipe, every frame, for the recipe's prefab item) with
    /// crafting on. A marked item gets its mark; a recipe whose item can be marked gets the mark the local player's craft
    /// would give, while the module is on and they stand at a station that trains Crafting. The line goes after the
    /// durability line, else at the end; the text stays the same from frame to frame, so the game's tooltip is not
    /// redrawn. The tooltip of a second item appended to this one (m_appendToolTip) is a call of its own with appending
    /// set and is skipped.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    public static class MarkTooltip
    {
        private const string DurabilityKey = "\n$item_durability:";

        private static float previewShare = -1f;
        private static int previewVersion = -1;
        private static string previewLine;

        [HarmonyPostfix]
        private static void Postfix(ItemDrop.ItemData item, bool crafting, bool appending, ref string __result)
        {
            if (appending || item == null || __result == null)
                return;
            string line = HookGuard.Run("Maker's Mark tooltip", static shown => LineFor(shown.item, shown.crafting), (item, crafting), (string)null);
            if (line != null)
                __result = WithLine(__result, line);
        }

        private static string LineFor(ItemDrop.ItemData item, bool crafting) => crafting ? Preview(item) : MarkReader.Line(item);

        /// <summary>The mark a craft of this recipe item would get now, kept per share and language so it is built once.</summary>
        private static string Preview(ItemDrop.ItemData item)
        {
            if (!CraftingPerks.Enabled || !MakersMark.Qualifies(item) || CraftingPerks.Station(Player.m_localPlayer) == null)
                return null;
            float share = MakersMark.Share(LocalSkill.Level(Skills.SkillType.Crafting));
            if (share <= 0f)
                return null;
            if (share != previewShare || previewVersion != Lang.Version)
            {
                previewShare = share;
                previewVersion = Lang.Version;
                previewLine = MakersMark.LineFor(share);
            }
            return previewLine;
        }

        private static string WithLine(string text, string line)
        {
            int durability = text.IndexOf(DurabilityKey, StringComparison.Ordinal);
            int end = durability < 0 ? -1 : text.IndexOf('\n', durability + 1);
            return end < 0 ? text + line : text.Insert(end, line);
        }
    }
}
