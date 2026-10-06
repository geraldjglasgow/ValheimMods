using System.Runtime.CompilerServices;

namespace GrindstoneSkills
{
    /// <summary>
    /// Which pickables are forage: one whose item the Forage file lists and which is not a crop
    /// (<see cref="Crops"/>). Everything else that can be picked (crops, surtling cores, treasure, tin, dragon
    /// eggs...) is left to the game and to the other modules. Nothing is forage while Foraging is off.
    /// </summary>
    public static class Forage
    {
        private sealed class Known
        {
            public bool Crop;
            public string Item;
        }

        private static readonly ConditionalWeakTable<Pickable, Known> known = new ConditionalWeakTable<Pickable, Known>();

        /// <summary>
        /// The pickable's forage entry, or null when it is not forage. Whether it is a crop and its item's prefab name are
        /// remembered per instance: the hover asks every frame, and both names would be a new string each time. The entry
        /// itself is looked up each time, so a reloaded Forage file applies at once.
        /// </summary>
        public static ForageEntry Of(Pickable pickable)
        {
            if (!ForagingSkill.Active || pickable == null || pickable.m_itemPrefab == null)
                return null;
            Known seen = known.GetValue(pickable, instance => new Known { Crop = Crops.IsCrop(instance), Item = instance.m_itemPrefab.name });
            return seen.Crop ? null : ForageFile.Find(seen.Item);
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
