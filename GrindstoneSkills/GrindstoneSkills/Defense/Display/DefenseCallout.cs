using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Short words floating where Defense did something ("Riposte!", "Reflex!", "Bash!", "First block: Troll"), drawn
    /// with the game's floating damage text (<see cref="FloatingText"/>) on the defending player's own client only, if
    /// their Defense "Show Callouts" is on. Every Defense event happens on that client, so nothing is sent and the words
    /// are in that client's language.
    /// </summary>
    public static class DefenseCallout
    {
        public static void Show(Vector3 position, string text)
        {
            if (DefenseSettings.ShowCallouts.Value)
                FloatingText.Show(position, text);
        }

        /// <summary>Above the local player's head.</summary>
        public static void OverPlayer(string text)
        {
            Player player = Player.m_localPlayer;
            if (player != null)
                Show(player.GetTopPoint() + Vector3.up * 0.3f, text);
        }

        /// <summary>A perk called out by its name ("Riposte!"); <paramref name="perkKey"/> is the perk name's word.</summary>
        public static string Perk(string perkKey) => Lang.Format("gs_defense_perk_callout", Lang.Get(perkKey));
    }
}
