namespace GrindstoneSkills
{
    /// <summary>
    /// Which pickables are forage: one whose item the Forage file lists and which is not a crop
    /// (<see cref="Crops"/>). Everything else that can be picked (crops, surtling cores, treasure, tin, dragon
    /// eggs...) is left to the game and to the other modules. Nothing is forage while Foraging is off.
    /// </summary>
    public static class Forage
    {
        /// <summary>The pickable's forage entry, or null when it is not forage.</summary>
        public static ForageEntry Of(Pickable pickable)
        {
            if (!ForagingSkill.Active || pickable == null || pickable.m_itemPrefab == null || Crops.IsCrop(pickable))
                return null;
            return ForageFile.Find(pickable.m_itemPrefab.name);
        }

        /// <summary>Whether a pick of it now would be taken: loaded, enabled, not picked yet.</summary>
        public static bool CanPick(Pickable pickable) =>
            pickable != null && pickable.m_nview != null && pickable.m_nview.IsValid() && pickable.m_enabled != 0 && !pickable.m_picked;

        /// <summary>The item's display name, localized ("Raspberries"); the prefab name when it has none.</summary>
        public static string ItemName(Pickable pickable)
        {
            ItemDrop drop = pickable?.m_itemPrefab != null ? pickable.m_itemPrefab.GetComponent<ItemDrop>() : null;
            string token = drop?.m_itemData?.m_shared?.m_name;
            if (string.IsNullOrEmpty(token))
                return pickable?.m_itemPrefab != null ? pickable.m_itemPrefab.name : "";
            return Localization.instance != null ? Localization.instance.Localize(token) : token;
        }
    }
}
