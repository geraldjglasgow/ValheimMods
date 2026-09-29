using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Crafting's page in the info pane, from the game's code: shorter crafts and a chance of one more item at stations
    /// that train Crafting (InventoryGui.UpdateRecipe and DoCrafting, with the live InventoryGui's timings and chances),
    /// and cheaper building with a tool whose pieces train Crafting (Player.GetBuildStamina and GetPlaceDurability):
    /// the tool in hand when it is one, else the first such item in ObjectDB. GrindstoneSkills changes none of it.
    /// </summary>
    public static class CraftingPage
    {
        /// <summary>The game's build stamina saving at level 100.</summary>
        private const float BuildStaminaAt100 = 0.5f;

        public static void Write(SkillPage page)
        {
            ItemDrop.ItemData tool = BuildTool(page.Player);
            page.About = tool != null
                ? "Craft faster and build for less. Trained by crafting, repairing gear and building."
                : "Craft faster. Trained by crafting and repairing gear.";
            if (InventoryGui.instance != null)
                Stations(page, InventoryGui.instance);
            if (tool != null)
                Building(page, tool);
        }

        private static void Stations(SkillPage page, InventoryGui gui)
        {
            float time = 1f - page.Factor * gui.m_craftDurationSkillMaxDecrease;
            page.Line($"Craft time -{SkillPage.Percent(1f - time)}", "Craft time",
                $"At stations that train Crafting: {SkillPage.Duration(gui.m_craftDuration * time)} a craft instead of {SkillPage.Duration(gui.m_craftDuration)}, {SkillPage.Duration(gui.m_multiCraftDuration * time)} for {gui.m_multiCraftAmount} at once. -{SkillPage.Percent(gui.m_craftDurationSkillMaxDecrease)} at level 100.");
            page.Line($"Extra item {SkillPage.Percent(page.Factor * gui.m_craftBonusChance)}", "Extra item",
                $"Each craft of a stackable item, such as arrows, has that chance to give {gui.m_craftBonusAmount} more. {SkillPage.Percent(gui.m_craftBonusChance)} at level 100.");
        }

        /// <summary>The tool's stamina for building, removing and repairing, and its wear when its wear follows Crafting.</summary>
        private static void Building(SkillPage page, ItemDrop.ItemData tool)
        {
            string name = Localized(tool.m_shared.m_name);
            string stamina = SkillPage.Percent(BuildStaminaAt100 * page.Factor);
            if (tool.m_shared.m_placementDurabilitySkill != Skills.SkillType.Crafting)
            {
                page.Line($"{name}: stamina -{stamina}", name,
                    $"For building, removing and repairing pieces with it. -{SkillPage.Percent(BuildStaminaAt100)} at level 100.");
                return;
            }
            float wear = tool.m_shared.m_placementDurabilityMax;
            page.Line($"{name}: stamina -{stamina}, wear -{SkillPage.Percent(wear * page.Factor)}", name,
                $"Stamina for building, removing and repairing pieces with it, wear for building and removing. -{SkillPage.Percent(BuildStaminaAt100)} and -{SkillPage.Percent(wear)} at level 100.");
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
