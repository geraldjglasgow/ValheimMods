using HarmonyLib;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Using a spell item (from the inventory or the hotbar: the game consumes a Consumable through
    /// <c>Player.ConsumeItem</c>) teaches its spell and uses one up, instead of eating it; a spell the character already
    /// knows leaves the item, so it can go to a friend; with the switch off a spell item does nothing. Answering false
    /// keeps the game's eating animation and sound away.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
    public static class SpellLearning
    {
        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Inventory inventory, ItemDrop.ItemData item, ref bool __result)
        {
            Spell spell = item?.m_dropPrefab != null ? Spells.ByItem(item.m_dropPrefab.name) : null;
            if (spell == null)
                return true;
            __result = false;
            if (!CastersSettings.On)
                return false;
            string name = Core.Language.Localize(spell.Token);
            if (KnownSpells.Learn(__instance, spell))
            {
                inventory.RemoveOneItem(item);
                __instance.Message(MessageHud.MessageType.Center, string.Format(Core.Language.Localize(CasterWords.Learned), name));
            }
            else
            {
                __instance.Message(MessageHud.MessageType.Center, string.Format(Core.Language.Localize(CasterWords.AlreadyKnown), name));
            }
            return false;
        }
    }
}
