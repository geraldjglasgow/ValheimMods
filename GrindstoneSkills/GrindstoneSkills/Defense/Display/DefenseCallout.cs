using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Short words floating where Defense did something ("Riposte!", "Reflex!", "Bash!", "First block: Troll"), drawn
    /// with the game's floating damage text (<see cref="FloatingText"/>) on the defending player's own client only, if
    /// their Defense "Show Callouts" is on. Every Defense event happens on that client, so nothing is sent.
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
    }
}
