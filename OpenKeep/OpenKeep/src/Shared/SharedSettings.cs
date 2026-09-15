using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace OpenKeep.Shared
{
    /// <summary>
    /// Section "9. Shared". The timeout and the touch duration change what every player experiences of a shared
    /// chest and are synced; whether touches are shown and in which colour is per player. Values are read at use
    /// time.
    /// </summary>
    public static class SharedSettings
    {
        public const string Section = "9. Shared";
        public const string DefaultColour = "#ffb347";

        public static ConfigEntry<float> RequestTimeout { get; private set; }
        public static ConfigEntry<float> TouchSeconds { get; private set; }
        public static ConfigEntry<bool> ShowTouches { get; private set; }
        public static ConfigEntry<string> TouchColour { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            RequestTimeout = synced.Bind(Section, "Request Timeout", 2f,
                "Seconds a request to the client that owns a shared chest may take; without a reply the request counts as denied and the centre message says 'The chest did not answer'.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 30f));
            TouchSeconds = synced.Bind(Section, "Touch Seconds", 5f,
                "Seconds a slot stays marked after another player pressed the mouse on it, unless the player lets go earlier.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 60f));
            ShowTouches = synced.Bind(Section, "Show Touches", true,
                "Slots another player is moving are tinted and the tooltip says who.", synced: false);
            TouchColour = synced.Bind(Section, "Touch Colour", DefaultColour,
                "Colour (hex, #rrggbb) of a slot another player is moving.", synced: false);
        }

        /// <summary>The touch colour, or the default when the setting does not parse.</summary>
        public static Color Colour()
        {
            string value = (TouchColour.Value ?? "").Trim();
            if (ColorUtility.TryParseHtmlString(value, out Color colour))
                return colour;
            ColorUtility.TryParseHtmlString(DefaultColour, out colour);
            return colour;
        }
    }
}
