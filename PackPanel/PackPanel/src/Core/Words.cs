using PackPanel.Slots;
using Translations;

namespace PackPanel.Core
{
    /// <summary>
    /// The slot captions, in the player's language: each slot kind's $packpanel_ word (<c>translations/English.yml</c>).
    /// The mod's other words are read where they are shown, with <see cref="Lang"/>.
    /// </summary>
    public static class Words
    {
        /// <summary>The caption of a slot kind.</summary>
        public static string Caption(SlotKind kind) => Lang.Get(CaptionKey(kind));

        private static string CaptionKey(SlotKind kind)
        {
            switch (kind)
            {
                case SlotKind.Head: return "packpanel_head";
                case SlotKind.Chest: return "packpanel_chest";
                case SlotKind.Legs: return "packpanel_legs";
                case SlotKind.Feet: return "packpanel_feet";
                case SlotKind.Back: return "packpanel_back";
                case SlotKind.Backpack: return "packpanel_backpack";
                case SlotKind.Utility: return "packpanel_utility";
                case SlotKind.Trinket: return "packpanel_trinket";
                case SlotKind.Food: return "packpanel_food";
                case SlotKind.Mead: return "packpanel_mead";
                case SlotKind.Ammo: return "packpanel_ammo";
                case SlotKind.Key: return "packpanel_keys";
                case SlotKind.Tacklebox: return "packpanel_tackle";
                case SlotKind.Tackle: return "packpanel_bait";
                default: return "packpanel_coins";
            }
        }
    }
}
