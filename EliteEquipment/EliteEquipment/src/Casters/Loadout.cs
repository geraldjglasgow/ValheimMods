namespace EliteEquipment.Casters
{
    /// <summary>
    /// The two spells a wand holds ready (user, 2026-10-11: "2 extra spells only"), saved on the wand itself (item custom
    /// data <see cref="SlotsKey"/>: the spell keys separated by commas, an empty one for an empty slot), so two wands are two
    /// loadouts. Slot 1 casts with right mouse, slot 2 with right mouse held and left mouse (<see cref="CasterControls"/>).
    /// A new wand holds Mend in slot 1. A slot holding a spell the wielder does not know (a wand from a friend) counts as
    /// empty for them. A wand saved with three slots, from before, keeps its first two.
    /// </summary>
    public static class Loadout
    {
        public const int Slots = 2;
        public const string SlotsKey = "EliteEquipment.loadout";

        public static string[] Read(ItemDrop.ItemData wand)
        {
            var slots = new string[Slots];
            string text = wand.m_customData.TryGetValue(SlotsKey, out string saved) ? saved : Spells.MendKey;
            string[] keys = text.Split(',');
            for (int i = 0; i < Slots; i++)
                slots[i] = i < keys.Length ? keys[i] : "";
            return slots;
        }

        /// <summary>Puts a spell in a slot (null empties it); the same spell in the other slot leaves it.</summary>
        public static void Set(ItemDrop.ItemData wand, int slot, Spell spell)
        {
            string[] slots = Read(wand);
            for (int i = 0; i < Slots; i++)
            {
                if (spell != null && slots[i] == spell.Key)
                    slots[i] = "";
            }
            slots[slot] = spell != null ? spell.Key : "";
            wand.m_customData[SlotsKey] = string.Join(",", slots);
        }

        /// <summary>The spell in a slot the player can cast, or null.</summary>
        public static Spell In(Player player, ItemDrop.ItemData wand, int slot)
        {
            if (slot < 0 || slot >= Slots)
                return null;
            Spell spell = Spells.Find(Read(wand)[slot]);
            return KnownSpells.Knows(player, spell) ? spell : null;
        }

        /// <summary>The wand holds a castable spell of this kind in either slot.</summary>
        public static bool Holds(Player player, ItemDrop.ItemData wand, SpellKind kind)
        {
            for (int i = 0; i < Slots; i++)
            {
                Spell spell = In(player, wand, i);
                if (spell != null && spell.Kind == kind)
                    return true;
            }
            return false;
        }
    }
}
