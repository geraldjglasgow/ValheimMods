using System.Collections.Generic;
using System.Linq;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The local character's beds in the current world: <c>Player.m_customData</c> under
    /// <c>OpenKeep.beds.&lt;world uid&gt;</c>, through <see cref="CharacterData"/>. The world uid is the key the game's
    /// profile keeps its own per-world spawn point under (<c>ZNet.GetWorldUID</c>, sent by the server to every
    /// client). The game saves the custom data with the character, so the list follows the character from server to
    /// server and each world keeps its own. <see cref="Scope"/> names the character and world a change belongs to;
    /// it is null where nobody plays (a dedicated server, no world loaded).
    /// </summary>
    public static class BedStore
    {
        public static string Scope()
        {
            if (ZNet.instance == null || ZNet.instance.IsDedicated() || ZNet.m_world == null || Game.instance == null)
                return null;
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            return profile == null ? null : profile.GetPlayerID() + "@" + ZNet.instance.GetWorldUID();
        }

        /// <summary>The custom data can be read and written: the scope exists and so does the local player.</summary>
        public static bool Writable(string scope) => scope != null && Player.m_localPlayer != null;

        public static List<Vector3> Read()
        {
            List<Vector3> points = new List<Vector3>();
            foreach (string text in CharacterData.GetSet(Key()))
            {
                if (BedPoints.TryParse(text, out Vector3 point))
                    points.Add(point);
            }
            return points;
        }

        public static void Write(List<Vector3> points) => CharacterData.SetSet(Key(), points.Select(BedPoints.Format));

        private static string Key() => "beds." + ZNet.instance.GetWorldUID();
    }
}
