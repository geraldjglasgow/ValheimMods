using StatusTemplates;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The "Ringing ears" status effect a shriek leaves on a player, made at runtime as a plain game StatusEffect so the
    /// HUD shows it as it shows any other: an icon (the game's own bell), its name, a countdown, and a line as it
    /// starts. It is handed to the player's status effects as an object rather than by name, so it needs no place in
    /// the game's object database, and the game never writes status effects into the character file, so a character
    /// saved mid-ringing loads cleanly with or without the mod. It is the deafness's one clock: a second shriek while it
    /// lasts only extends it, to whichever end is later, and never stacks a second copy.
    /// </summary>
    internal static class ShriekStatus
    {
        /// <summary>Item icons to borrow, in order: a bell for ringing ears, then items every game has.</summary>
        private static readonly string[] IconItems = { "Bell", "BellFragment", "Wishbone" };

        // Word tokens: the game words them in the player's language.
        private static readonly StatusTemplate<StatusEffect> Ringing = new StatusTemplate<StatusEffect>("ecr_ringing_ears",
            se =>
            {
                se.m_name = Lang.Token("ecr_se_ringing_ears");
                se.m_tooltip = Lang.Token("ecr_se_ringing_ears_tooltip");
                se.m_startMessage = Lang.Token("ecr_se_ringing_ears_start");
            },
            () => GameIcons.Item(IconItems));

        /// <summary>The ringing on <paramref name="player"/> now, or null when they hear as normal.</summary>
        public static StatusEffect? On(Player player) => Ringing.On(player);

        /// <summary>
        /// Rings <paramref name="player"/>'s ears for <paramref name="seconds"/> from now, or keeps them ringing that long
        /// when it outlasts the ringing already there. Null only when the game refused the effect.
        /// </summary>
        public static StatusEffect? Give(Player player, float seconds)
        {
            StatusEffect? current = On(player);
            if (current != null)
            {
                current.m_ttl = Mathf.Max(current.m_ttl, current.GetDuration() + seconds);
                return current;
            }
            Ringing.Get().m_ttl = seconds; // the game adds a copy, which keeps this
            return Ringing.Add(player);
        }
    }
}
