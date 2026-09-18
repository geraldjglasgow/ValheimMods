using UnityEngine;
using Party.Chat;

namespace Party.Client
{
    /// <summary>A party member died: a chat line plus a temporary death pin at the spot.</summary>
    public static class PartyDeathNotice
    {
        private const float PinLifetimeSeconds = 120f;

        public static void OnDeliver(string name, Vector3 position)
        {
            PartyAnnounce.Print($"{name} died.");
            TempPartyPins.Add(position, $"{name} died", Minimap.PinType.Death, PinLifetimeSeconds);
        }
    }
}
