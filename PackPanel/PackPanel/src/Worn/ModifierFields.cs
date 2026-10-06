using System.Reflection;
using HarmonyLib;

namespace PackPanel.Worn
{
    /// <summary>
    /// The game's equipment modifier source fields (<c>Player.s_equipmentModifierSourceFields</c>: movement, heat
    /// resistance, attack stamina and the rest) as typed getters, made once per field list. The game reads them with
    /// <c>FieldInfo.GetValue</c>, which boxes every float; the extra utilities' sum runs in every physics step
    /// (<c>Player.UpdateModifiers</c>), so it reads the same fields through these instead and makes no garbage.
    /// </summary>
    public static class ModifierFields
    {
        private static FieldInfo[] madeFrom;
        private static AccessTools.FieldRef<ItemDrop.ItemData.SharedData, float>[] getters =
            new AccessTools.FieldRef<ItemDrop.ItemData.SharedData, float>[0];

        /// <summary>A getter per source field, in the game's order; empty before the game has made its list.</summary>
        public static AccessTools.FieldRef<ItemDrop.ItemData.SharedData, float>[] Get()
        {
            FieldInfo[] fields = Player.s_equipmentModifierSourceFields;
            if (fields == null || ReferenceEquals(fields, madeFrom))
                return getters;
            var made = new AccessTools.FieldRef<ItemDrop.ItemData.SharedData, float>[fields.Length];
            for (int i = 0; i < fields.Length; i++)
                made[i] = AccessTools.FieldRefAccess<ItemDrop.ItemData.SharedData, float>(fields[i]);
            getters = made;
            madeFrom = fields;
            return getters;
        }
    }
}
