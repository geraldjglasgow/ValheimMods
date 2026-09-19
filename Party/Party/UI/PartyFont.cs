using TMPro;
using UnityEngine;

namespace Party.UI
{
    /// <summary>Valheim's own TextMeshPro font, found in memory rather than shipped as an asset (Party has none).</summary>
    public static class PartyFont
    {
        private static TMP_FontAsset font;

        public static TMP_FontAsset Get()
        {
            if (font != null)
                return font;
            TMP_FontAsset[] loaded = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            font = loaded.Length > 0 ? loaded[0] : TMP_Settings.defaultFontAsset;
            return font;
        }
    }
}
