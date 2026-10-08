using EliteCrafting.Text;
using UnityEngine;

namespace EliteCrafting.Tables
{
    /// <summary>The Rune Table's localization keys (translations/English.table.yml) and its small text helpers.</summary>
    internal static class TableWords
    {
        public const string Name = "$ecf_table_name";
        public const string Description = "$ecf_table_desc";

        /// <summary>A text in a colour, as TextMeshPro rich text.</summary>
        public static string Colored(string text, Color color) => "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";

        /// <summary>An essence's localized name in its colour.</summary>
        public static string EssenceName(Essence essence) => Colored(Words.Localize(essence.NameKey), essence.Color);

        /// <summary>
        /// The item class's own name, as the tooltip shows it: a class another mod registered names itself (PackPanel's
        /// backpacks: <c>$packpanel_class_backpack</c>), so <c>$ecf_class_&lt;id&gt;</c> alone showed as a raw key.
        /// </summary>
        public static string ClassName(Items.ClassInfo info) =>
            Display.DisplayWords.Name(info.Class?.Name, info.ClassId ?? "");

        /// <summary>The biome an item level stands for (1 Meadows ... 8 Deep North).</summary>
        public static string Biome(int level) => Words.Localize("$ecf_table_level_" + Mathf.Clamp(level, 1, 8));
    }
}
