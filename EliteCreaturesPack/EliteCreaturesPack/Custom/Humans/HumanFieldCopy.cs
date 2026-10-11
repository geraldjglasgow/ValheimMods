using System;
using System.Reflection;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// Copies the fields Unity serializes that one type declares, from one component to another that has them: the
    /// player's Character and Humanoid values onto a human's new Humanoid, the Draugr's BaseAI and MonsterAI values onto
    /// its MonsterAI. Only serialized fields, since only those reach the creatures instantiated from the prefab; the game
    /// sets up the rest in Awake. Reference values (effect lists, item arrays) end up shared with the source, so the
    /// source is always a private copy made for the purpose, never one of the game's prefabs.
    /// </summary>
    internal static class HumanFieldCopy
    {
        private const BindingFlags Declared =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>Copies `declaring`'s serialized fields from `from` to `to`; both must be (or derive from) that type.</summary>
        public static void Copy(Type declaring, object from, object to)
        {
            foreach (FieldInfo field in declaring.GetFields(Declared))
            {
                if (IsSerialized(field))
                {
                    field.SetValue(to, field.GetValue(from));
                }
            }
        }

        /// <summary>Unity's rule: public or [SerializeField], neither readonly, const nor [NonSerialized], and no delegate.</summary>
        private static bool IsSerialized(FieldInfo field)
        {
            if (field.IsInitOnly || field.IsLiteral || field.IsNotSerialized || typeof(Delegate).IsAssignableFrom(field.FieldType))
            {
                return false;
            }
            return field.IsPublic || field.IsDefined(typeof(SerializeField), false);
        }
    }
}
