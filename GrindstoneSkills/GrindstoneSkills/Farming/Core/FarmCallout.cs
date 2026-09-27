using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Farming's floating words ("Giant turnip!", a returned seed), shown only on the client of the player they are
    /// about, as the game shows its own "+1" for a bonus crop. The local player's own Show Callouts decides.
    /// </summary>
    public static class FarmCallout
    {
        public static void Show(Vector3 position, string text)
        {
            if (FarmingSettings.ShowCallouts.Value)
                FloatingText.Show(position + Vector3.up, text);
        }
    }
}
