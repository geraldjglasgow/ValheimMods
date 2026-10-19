using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Crafting's page in the info pane, from the game's code: shorter crafts and a chance of one more item at stations
    /// that train Crafting (InventoryGui.UpdateRecipe and DoCrafting, with the live InventoryGui's timings and chances),
    /// and cheaper building with a tool whose pieces train Crafting (Player.GetBuildStamina and GetPlaceDurability):
    /// the tool in hand when it is one, else the first such item in ObjectDB. What GrindstoneSkills adds follows in
    /// <see cref="CraftingPerksPage"/>.
    /// </summary>
    public static class CraftingPage
    {
        /// <summary>The game's build stamina saving at level 100.</summary>
        private const float BuildStaminaAt100 = 0.5f;

        public static void Write(SkillPage page)
        {
            ItemDrop.ItemData tool = BuildTool(page.Player);
            page.About = Lang.Get(tool != null ? "gs_page_crafting_about_build" : "gs_page_crafting_about");
            if (InventoryGui.instance != null)
                Stations(page, InventoryGui.instance);
            if (tool != null)
                Building(page, tool);
        }

        private static void Stations(SkillPage page, InventoryGui gui)
        {
            float time = 1f - page.Factor * gui.m_craftDurationSkillMaxDecrease;
            string craftTime = Lang.Get("gs_page_crafting_time");
            page.Line(Lang.Format("gs_page_crafting_time_line", craftTime, SkillPage.Percent(1f - time)), craftTime,
                Lang.Format("gs_page_crafting_time_tip", SkillPage.Duration(gui.m_craftDuration * time), SkillPage.Duration(gui.m_craftDuration),
                    SkillPage.Duration(gui.m_multiCraftDuration * time), gui.m_multiCraftAmount, SkillPage.Percent(gui.m_craftDurationSkillMaxDecrease)));
            string extra = Lang.Get("gs_page_crafting_extra");
            page.Line(Lang.Format("gs_page_crafting_extra_line", extra, SkillPage.Percent(page.Factor * gui.m_craftBonusChance)), extra,
                Lang.Format("gs_page_crafting_extra_tip", gui.m_craftBonusAmount, SkillPage.Percent(gui.m_craftBonusChance)));
        }

        /// <summary>The tool's stamina for building, removing and repairing, and its wear when its wear follows Crafting.</summary>
        private static void Building(SkillPage page, ItemDrop.ItemData tool)
        {
            string name = Localized(tool.m_shared.m_name);
            string stamina = SkillPage.Percent(BuildStaminaAt100 * page.Factor);
            if (tool.m_shared.m_placementDurabilitySkill != Skills.SkillType.Crafting)
            {
                page.Line(Lang.Format("gs_page_crafting_tool_line", name, stamina), name,
                    Lang.Format("gs_page_crafting_tool_tip", SkillPage.Percent(BuildStaminaAt100)));
                return;
            }
            float wear = tool.m_shared.m_placementDurabilityMax;
            page.Line(Lang.Format("gs_page_crafting_tool_wear_line", name, stamina, SkillPage.Percent(wear * page.Factor)), name,
                Lang.Format("gs_page_crafting_tool_wear_tip", SkillPage.Percent(BuildStaminaAt100), SkillPage.Percent(wear)));
        }

        /// <summary>The build tool whose pieces train Crafting: the one in hand when it is one, else the first in ObjectDB.</summary>
        private static ItemDrop.ItemData BuildTool(Player player)
        {
            ItemDrop.ItemData held = player.GetRightItem();
            if (TrainsCrafting(held))
                return held;
            if (ObjectDB.instance == null)
                return null;
            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop != null && TrainsCrafting(drop.m_itemData))
                    return drop.m_itemData;
            }
            return null;
        }

        private static bool TrainsCrafting(ItemDrop.ItemData item) =>
            item != null && item.m_shared.m_buildPieces != null && item.m_shared.m_buildPieces.m_skill == Skills.SkillType.Crafting;

        private static string Localized(string token) =>
            Localization.instance != null ? Localization.instance.Localize(token) : token ?? "";
    }
}
