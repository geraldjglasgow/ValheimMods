using System.Collections.Generic;
using System.Globalization;
using EarthWright.Brush;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// Fills a custom entry's command template from the local brush: {x} {y} {z} (the brush centre, or the ghost's
    /// position when the brush is not active), {radius} {radius2} {height} (the brush amount) {target} (the target
    /// height) {rotation} {shape} and {player} (the player's name). Numbers use a dot and at most three decimals.
    /// </summary>
    public static class CommandTemplate
    {
        public static string Fill(string template, Player player, Vector3 ghostPosition)
        {
            string text = template;
            foreach (KeyValuePair<string, string> value in Values(player, ghostPosition))
                text = text.Replace("{" + value.Key + "}", value.Value);
            return text.Trim();
        }

        private static Dictionary<string, string> Values(Player player, Vector3 ghostPosition)
        {
            Vector3 center = BrushState.Active && BrushState.HasAim ? BrushState.Center : ghostPosition;
            return new Dictionary<string, string>
            {
                { "x", Number(center.x) },
                { "y", Number(center.y) },
                { "z", Number(center.z) },
                { "radius", Number(BrushState.Radius) },
                { "radius2", Number(BrushState.Radius2) },
                { "height", Number(BrushState.Amount) },
                { "target", Number(BrushState.TargetHeight) },
                { "rotation", Number(BrushState.Rotation) },
                { "shape", BrushState.Shape.ToString().ToLowerInvariant() },
                { "player", player != null ? player.GetPlayerName() : "" },
            };
        }

        private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
