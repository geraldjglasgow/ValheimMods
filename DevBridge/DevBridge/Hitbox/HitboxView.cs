using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Hitbox
{
    /// <summary>
    /// What /hitbox turned on, and what it saw: every melee swing's hit shape flashed in the world, and a line from the
    /// attacker to the local player for every hit that reaches them, each also logged with its distances. Drawn on this
    /// machine only, and only where the attack is worked out (the attacker's owner: single player or the host).
    /// </summary>
    internal static class HitboxView
    {
        private const int Keep = 40;

        internal static bool On;
        internal static bool Players;
        internal static float Seconds = 1.5f;

        private static readonly List<Dictionary<string, object>> events = new List<Dictionary<string, object>>();

        internal static void Record(string kind, Dictionary<string, object> entry)
        {
            entry["kind"] = kind;
            entry["time"] = Fmt.R(Time.time);
            events.Add(entry);
            if (events.Count > Keep) events.RemoveAt(0);
            Debug.Log($"[DevBridge] hitbox {kind}: " + string.Join(", ", entry.Where(e => e.Key != "kind").Select(e => $"{e.Key}={e.Value}")));
        }

        internal static List<Dictionary<string, object>> Recent(int count) => events.Skip(Mathf.Max(0, events.Count - count)).ToList();

        internal static void Clear() => events.Clear();

        internal static string Name(Character character) =>
            Localization.instance != null ? Localization.instance.Localize(character.m_name) : character.m_name;

        /// <summary>Metres between two characters on the ground plane: centre to centre, and body edge to body edge.</summary>
        internal static (float centres, float gap) Apart(Character a, Character b)
        {
            Vector3 between = b.transform.position - a.transform.position;
            float centres = new Vector2(between.x, between.z).magnitude;
            return (centres, centres - a.GetRadius() - b.GetRadius());
        }
    }
}
