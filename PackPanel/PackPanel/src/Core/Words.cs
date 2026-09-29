using PackPanel.Backpacks;
using PackPanel.Ring;
using PackPanel.Slots;

namespace PackPanel.Core
{
    /// <summary>The $packpanel_ words: the slot captions, the slot panel's tabs, the key ring's words and the messages.</summary>
    public static class Words
    {
        public static string Head { get; private set; }
        public static string Chest { get; private set; }
        public static string Legs { get; private set; }
        public static string Back { get; private set; }
        public static string Backpack { get; private set; }
        public static string Utility { get; private set; }
        public static string Trinket { get; private set; }
        public static string Food { get; private set; }
        public static string Mead { get; private set; }
        public static string Ammo { get; private set; }
        public static string Coins { get; private set; }
        public static string Keys { get; private set; }
        public static string KeyRing { get; private set; }
        public static string NoKeysHeld { get; private set; }
        public static string NotAKey { get; private set; }
        public static string KeyNew { get; private set; }
        public static string KeysNew { get; private set; }
        public static string WrongSlot { get; private set; }
        public static string Dropped { get; private set; }
        public static string GearTab { get; private set; }
        public static string ConsumablesTab { get; private set; }
        public static string StatResistances { get; private set; }
        public static string StatGear { get; private set; }
        public static string StatEpicLoot { get; private set; }
        public static string StatOffence { get; private set; }
        public static string StatDefence { get; private set; }
        public static string StatResources { get; private set; }
        public static string StatMovement { get; private set; }
        public static string StatSkills { get; private set; }
        public static string StatOther { get; private set; }

        public static void Register()
        {
            Head = Language.Add("packpanel_head", "Head");
            Chest = Language.Add("packpanel_chest", "Chest");
            Legs = Language.Add("packpanel_legs", "Legs");
            Back = Language.Add("packpanel_back", "Back");
            Backpack = Language.Add("packpanel_backpack", "Backpack");
            Utility = Language.Add("packpanel_utility", "Utility");
            Trinket = Language.Add("packpanel_trinket", "Trinket");
            Food = Language.Add("packpanel_food", "Food");
            Mead = Language.Add("packpanel_mead", "Mead");
            Ammo = Language.Add("packpanel_ammo", "Ammo");
            Coins = Language.Add("packpanel_coins", "Coins");
            Keys = Language.Add("packpanel_keys", "Keys");
            KeyRing = Language.Add("packpanel_keyring", "Key ring");
            NoKeysHeld = Language.Add("packpanel_nokeysheld", "You carry no keys");
            NotAKey = Language.Add("packpanel_notakey", "Only keys go on the key ring");
            KeyNew = Language.Add("packpanel_keynew", "{0} went onto your key ring");
            KeysNew = Language.Add("packpanel_keysnew", "{0} new keys went onto your key ring");
            WrongSlot = Language.Add("packpanel_wrongslot", "That does not go in that slot");
            Dropped = Language.Add("packpanel_dropped", "No room for {0} items: dropped at your feet");
            GearTab = Language.Add("packpanel_tab_gear", "Gear");
            ConsumablesTab = Language.Add("packpanel_tab_consumables", "Consumables");
            RegisterStats();
        }

        /// <summary>The stat sheet's headings.</summary>
        private static void RegisterStats()
        {
            StatResistances = Language.Add("packpanel_stat_resistances", "Resistances");
            StatGear = Language.Add("packpanel_stat_gear", "Gear bonuses");
            StatEpicLoot = Language.Add("packpanel_stat_epicloot", "Epic Loot");
            StatOffence = Language.Add("packpanel_stat_offence", "Offence");
            StatDefence = Language.Add("packpanel_stat_defence", "Defence");
            StatResources = Language.Add("packpanel_stat_resources", "Health and stamina");
            StatMovement = Language.Add("packpanel_stat_movement", "Movement");
            StatSkills = Language.Add("packpanel_stat_skills", "Skills");
            StatOther = Language.Add("packpanel_stat_other", "Other");
        }

        /// <summary>The caption of a slot kind.</summary>
        public static string Caption(SlotKind kind)
        {
            switch (kind)
            {
                case SlotKind.Head: return Head;
                case SlotKind.Chest: return Chest;
                case SlotKind.Legs: return Legs;
                case SlotKind.Back: return Back;
                case SlotKind.Backpack: return Backpack;
                case SlotKind.Utility: return Utility;
                case SlotKind.Trinket: return Trinket;
                case SlotKind.Food: return Food;
                case SlotKind.Mead: return Mead;
                case SlotKind.Ammo: return Ammo;
                case SlotKind.Key: return Keys;
                case SlotKind.Tacklebox: return Tackle.TackleboxWords.Tacklebox;
                case SlotKind.Tackle: return Tackle.TackleboxWords.Bait;
                default: return Coins;
            }
        }
    }
}
