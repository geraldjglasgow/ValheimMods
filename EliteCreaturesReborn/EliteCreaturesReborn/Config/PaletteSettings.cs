using BepInEx.Configuration;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// The per-player star colour palette. These are local settings: they change only what this player sees, so the
    /// server never binds them. Colour is always redundant with the name, so editing it loses no information.
    /// </summary>
    public static class PaletteSettings
    {
        public const string Section = "9 - Palette (per player)";

        private static readonly ConfigEntry<string>[] Colors = new ConfigEntry<string>[MutationCatalog.InOrder.Length];

        public static void Bind(ConfigFile config)
        {
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                string key = $"{MutationCatalog.Word(mutation)} star colour";
                Colors[(int)mutation] = config.Bind(Section, key, MutationCatalog.DefaultColorHex(mutation),
                    $"Colour of a star carrying the {MutationCatalog.Word(mutation)} mutation. Client side.");
            }
        }

        public static Color Of(Mutation mutation)
        {
            ConfigEntry<string> entry = Colors[(int)mutation];
            Color fallback = ColorParse.Hex(MutationCatalog.DefaultColorHex(mutation), Color.white);
            return ColorParse.Hex(entry.Value, fallback);
        }
    }
}
