using TMPro;
using UnityEngine;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// The tracker's look from the settings, in the game's own materials: its fonts (found by name among the loaded
    /// font assets; the inventory's text font stands in) and colours. <see cref="Signature"/> changes whenever something
    /// that needs a rebuild changes.
    /// </summary>
    public static class TrackerStyle
    {
        private static readonly string[] FontNames = { "Valheim-AveriaSansLibre", "Valheim-AveriaSerifLibre", "Valheim-Norsebold" };
        private static readonly TMP_FontAsset[] fonts = new TMP_FontAsset[FontNames.Length];

        public static readonly Color Dim = new Color(0.72f, 0.72f, 0.72f, 1f);

        public static float Size => TrackerSettings.FontSize.Value;

        public static float Width => Size * 16f;

        public static Color Have => TrackerSettings.Colour(TrackerSettings.HaveColour, Color.white);

        public static Color Missing => TrackerSettings.Colour(TrackerSettings.MissingColour, new Color(1f, 0.42f, 0.35f));

        public static Color Ready => TrackerSettings.Colour(TrackerSettings.ReadyColour, new Color(1f, 0.713f, 0.361f));

        public static string Signature => $"{TrackerSettings.Font.Value}|{TrackerSettings.FontSize.Value}|{TrackerSettings.Scale.Value}|{TrackerSettings.BackgroundOpacity.Value}|{(ObjectDB.instance != null ? ObjectDB.instance.m_recipes.Count : 0)}";

        public static TMP_FontAsset Font
        {
            get
            {
                int index = Mathf.Clamp((int)TrackerSettings.Font.Value, 0, FontNames.Length - 1);
                if (fonts[index] == null)
                    fonts[index] = Find(FontNames[index]);
                return fonts[index] != null ? fonts[index] : Fallback();
            }
        }

        private static TMP_FontAsset Find(string name)
        {
            foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (font != null && font.name == name)
                    return font;
            }
            return null;
        }

        private static TMP_FontAsset Fallback()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui != null && gui.m_recipeDecription != null && gui.m_recipeDecription.font != null)
                return gui.m_recipeDecription.font;
            return TMP_Settings.defaultFontAsset;
        }
    }
}
