using BepInEx.Configuration;
using UnityEngine;

namespace EliteCreaturesReborn.Recap
{
    /// <summary>
    /// The death recap's settings: all of them per player and never locked, since the recap records only this player's
    /// own screen and the hits this player took, and is seen only by them.
    /// </summary>
    public static class RecapSettings
    {
        public const string Section = "10 - Death Recap (per player)";

        public static ConfigEntry<bool> Record = null!;
        public static ConfigEntry<KeyboardShortcut> Key = null!;
        public static ConfigEntry<int> Seconds = null!;
        public static ConfigEntry<int> FramesPerSecond = null!;
        public static ConfigEntry<int> Height = null!;
        public static ConfigEntry<int> Kept = null!;
        public static ConfigEntry<bool> Notice = null!;

        public static void Bind(ConfigFile config)
        {
            Record = config.Bind(Section, "Record deaths", true,
                "Keep a small video of the last seconds on screen, in memory only, so every death can be watched again "
                + "in the death recap window. Off records nothing and costs nothing; the hits are still listed. Client "
                + "side; never locked.");
            Key = config.Bind(Section, "Recap key", new KeyboardShortcut(KeyCode.F10),
                "Opens or closes the death recap window (also /deaths in chat or deaths in the F5 console). Client side.");
            Notice = config.Bind(Section, "Death notice", true,
                "After a death, one line at the top left of the screen names the killer and the recap key. Respawning "
                + "is never held up. Client side; never locked.");
            Kept = config.Bind(Section, "Deaths kept", 5,
                new ConfigDescription("How many deaths the recap keeps, newest first. They last until the game closes. "
                    + "Client side.", new AcceptableValueRange<int>(1, 10)));
            BindVideo(config);
        }

        private static void BindVideo(ConfigFile config)
        {
            Seconds = config.Bind(Section, "Seconds before death", 15,
                new ConfigDescription("How much of the time before a death the video keeps. Client side.",
                    new AcceptableValueRange<int>(5, 30)));
            FramesPerSecond = config.Bind(Section, "Video frames per second", 15,
                new ConfigDescription("Frames the video records each second. More is smoother and uses more memory. "
                    + "Client side.", new AcceptableValueRange<int>(5, 30)));
            Height = config.Bind(Section, "Video height", 360,
                new ConfigDescription("Height of the recorded video in pixels; its width follows the screen's shape. "
                    + "Higher is sharper and uses more memory (360: about 10 MB per death). Client side.",
                    new AcceptableValueRange<int>(180, 720)));
        }
    }
}
