namespace EliteEquipment.Casters
{
    /// <summary>What caster gear a character has on: the wand in hand, the spellbook, the robes; by the item's own word.</summary>
    public static class CasterGearWorn
    {
        public static bool IsWand(ItemDrop.ItemData item) => WandPiece(item) != null;

        /// <summary>Which of the wands an item is, or null.</summary>
        public static GearPiece WandPiece(ItemDrop.ItemData item)
        {
            foreach (GearPiece wand in CasterGear.Wands)
            {
                if (Is(item, wand))
                    return wand;
            }
            return null;
        }

        /// <summary>Which of the spellbooks an item is, or null.</summary>
        public static GearPiece BookPiece(ItemDrop.ItemData item)
        {
            foreach (GearPiece book in CasterGear.Books)
            {
                if (Is(item, book))
                    return book;
            }
            return null;
        }

        public static bool IsBook(ItemDrop.ItemData item) => BookPiece(item) != null;

        /// <summary>Which of the spellbooks is in the left hand, or null.</summary>
        public static GearPiece HeldBook(Humanoid humanoid) => BookPiece(humanoid != null ? humanoid.m_leftItem : null);

        /// <summary>The wand in the right hand, or null.</summary>
        public static ItemDrop.ItemData Wand(Humanoid humanoid)
        {
            ItemDrop.ItemData right = humanoid != null ? humanoid.m_rightItem : null;
            return IsWand(right) ? right : null;
        }

        /// <summary>The spellbook in the left hand, or null.</summary>
        public static ItemDrop.ItemData Book(Humanoid humanoid)
        {
            ItemDrop.ItemData left = humanoid != null ? humanoid.m_leftItem : null;
            return IsBook(left) ? left : null;
        }

        /// <summary>The eitr the worn robes and the wielded wand give (none while the switch is off).</summary>
        public static float Eitr(Humanoid humanoid)
        {
            if (humanoid == null || !CastersSettings.On)
                return 0f;
            GearPiece wand = WandPiece(Wand(humanoid));
            float eitr = wand != null ? wand.Eitr : 0f;
            eitr += RobeEitr(humanoid.m_helmetItem) + RobeEitr(humanoid.m_chestItem);
            return eitr + RobeEitr(humanoid.m_legItem) * CasterBoots.EitrShare + CasterBoots.BootsEitr(humanoid);
        }

        private static float RobeEitr(ItemDrop.ItemData worn)
        {
            foreach (GearPiece piece in CasterGear.Robes)
            {
                if (Is(worn, piece))
                    return piece.Eitr;
            }
            return 0f;
        }

        private static bool Is(ItemDrop.ItemData item, GearPiece piece) => item != null && item.m_shared.m_name == piece.Token;
    }
}
