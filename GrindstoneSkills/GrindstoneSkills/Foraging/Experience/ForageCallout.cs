using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Foraging's words in the world ("Discovered Thistle!"), for the picker only: drawn on the picker's own client with
    /// the game's floating text (<see cref="FloatingText"/>), a little above the plant, if their Show Callouts is on.
    /// </summary>
    public static class ForageCallout
    {
        private const float Height = 1.2f;

        public static void Show(Vector3 spot, string text)
        {
            if (ForagingSettings.ShowCallouts.Value)
                FloatingText.Show(spot + Vector3.up * Height, text);
        }
    }
}
