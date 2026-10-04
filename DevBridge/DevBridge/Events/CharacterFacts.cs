using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Events
{
    /// <summary>What the events say about a character, read on the main thread: its name as players see it, prefab,
    /// ZDO id (the one /zdo?id= takes), level and place.</summary>
    internal static class CharacterFacts
    {
        internal static Dictionary<string, object> About(Character character, string nameKey = "name") =>
            Into(new Dictionary<string, object>(), character, nameKey);

        internal static Dictionary<string, object> Into(Dictionary<string, object> data, Character character, string nameKey = "name")
        {
            data[nameKey] = Name(character);
            data["prefab"] = Prefab(character);
            data["id"] = character.GetZDOID().ToString();
            data["level"] = character.GetLevel();
            return data;
        }

        internal static string Name(Character character)
        {
            if (character is Player player) return player.GetPlayerName();
            return Localization.instance != null ? Localization.instance.Localize(character.m_name) : character.m_name;
        }

        internal static string Prefab(Character character) => Utils.GetPrefabName(character.gameObject);

        internal static float[] Position(Character character) => Fmt.V3(character.transform.position);

        /// <summary>Marks a boss or a tamed creature; left out when false, as most characters are neither.</summary>
        internal static void Flags(Dictionary<string, object> data, Character character)
        {
            if (character.IsBoss()) data["boss"] = true;
            if (character.IsTamed()) data["tamed"] = true;
        }

        internal static float Distance(Character a, Character b) =>
            Fmt.R(Vector3.Distance(a.transform.position, b.transform.position));
    }
}
