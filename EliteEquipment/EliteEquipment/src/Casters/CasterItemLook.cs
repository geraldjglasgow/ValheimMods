using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>A caster item's own name, description and icon (<c>&lt;asset&gt;_icon</c> in its bundle; the game item's when missing).</summary>
    public static class CasterItemLook
    {
        public static void Describe(SharedData shared, GearPiece piece)
        {
            shared.m_name = piece.Token;
            shared.m_description = piece.DescriptionToken;
            shared.m_variants = 0;
            Icon(shared, CasterBundles.Sprite(piece.Bundle, piece.Asset + "_icon"));
        }

        public static void Icon(SharedData shared, Sprite icon)
        {
            if (icon != null)
                shared.m_icons = new[] { icon };
        }
    }
}
