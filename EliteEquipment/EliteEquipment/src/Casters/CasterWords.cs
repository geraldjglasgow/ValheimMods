using EliteEquipment.Core;

namespace EliteEquipment.Casters
{
    /// <summary>The casters' English words: gear, spells, spell items, the set, the Spellbook and the messages.</summary>
    public static class CasterWords
    {
        public static string Spellbook { get; private set; }
        public static string Prepared { get; private set; }
        public static string Known { get; private set; }
        public static string Hint { get; private set; }
        public static string Learned { get; private set; }
        public static string AlreadyKnown { get; private set; }
        public static string HealPower { get; private set; }
        public static string EitrBonus { get; private set; }
        public static string EitrCost { get; private set; }
        public static string Empty { get; private set; }
        public static string SpellItemDescription { get; private set; }
        public static string NothingReady { get; private set; }
        public static string FirstKey { get; private set; }
        public static string SecondKey { get; private set; }

        /// <summary>The button a slot casts with, as the spell bar and the Spellbook label it.</summary>
        public static string KeyOf(int slot) => Core.Language.Localize(slot == 0 ? FirstKey : SecondKey);

        public static void Register()
        {
            foreach (GearPiece piece in CasterGear.All)
            {
                Language.Add(piece.Word, piece.Name);
                Language.Add(piece.Word + "_desc", piece.Description);
            }
            foreach (Spell spell in Spells.All)
                Spell(spell);
            foreach (RobeSet set in RobeSets.All)
            {
                Language.Add(set.SetWord, set.Name);
                Language.Add(set.SetWord + "_tip", set.BonusText);
            }
            foreach (FoodPiece food in CasterFood.All)
            {
                Language.Add(food.Word, food.Name);
                Language.Add(food.Word + "_desc", food.Description);
            }
            Interface();
        }

        private static void Spell(Spell spell)
        {
            Language.Add("ee_spell_" + spell.Key, spell.Name);
            Language.Add("ee_spell_" + spell.Key + "_desc", spell.Description);
            if (!spell.KnownByAll)
                Language.Add("ee_spellitem_" + spell.Key, "Spell: " + spell.Name);
        }

        private static void Interface()
        {
            Spellbook = Language.Add("ee_spellbook", "Spellbook");
            Prepared = Language.Add("ee_spellbook_prepared", "Held ready");
            Known = Language.Add("ee_spellbook_known", "Known spells");
            Hint = Language.Add("ee_spellbook_hint", "Click a slot, then a spell to put it there; click the picked slot again to empty it. Right mouse casts slot 1; hold right mouse and click left mouse for slot 2.");
            FirstKey = Language.Add("ee_spell_key_first", "RMB");
            SecondKey = Language.Add("ee_spell_key_second", "RMB+LMB");
            Learned = Language.Add("ee_spell_learned", "You learned {0}");
            AlreadyKnown = Language.Add("ee_spell_known", "You already know {0}");
            HealPower = Language.Add("ee_heal_power", "Heal power");
            EitrBonus = Language.Add("ee_eitr_bonus", "Eitr");
            EitrCost = Language.Add("ee_eitr_cost", "Eitr cost");
            Empty = Language.Add("ee_spell_empty", "Empty");
            NothingReady = Language.Add("ee_spell_none_ready", "No spell in that slot: open the Spellbook in the inventory");
            SpellItemDescription = Language.Add("ee_spellitem_desc",
                "Burnt runes on a strip of hide. Use it to learn the spell for good; a wand can then hold it ready.");
        }
    }
}
