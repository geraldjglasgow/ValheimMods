using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// The small moves every part of the character step makes: a value the definition sets replaces the field, a value
    /// it leaves out (null) keeps what is there (the base's, or an earlier pass's), and a component is added or taken off
    /// the shell. Only ever used on the shell, never on the base prefab.
    /// </summary>
    internal static class Assign
    {
        public static void Set(ref float field, float? value)
        {
            if (value != null)
            {
                field = value.Value;
            }
        }

        public static void Set(ref bool field, bool? value)
        {
            if (value != null)
            {
                field = value.Value;
            }
        }

        /// <summary>The shell's component of that type, added when it has none.</summary>
        public static T Ensure<T>(GameObject shell) where T : Component
        {
            T component = shell.GetComponent<T>();
            return component != null ? component : shell.AddComponent<T>();
        }

        /// <summary>Takes every component of that type off the shell itself (the shell is inactive, so nothing has woken).</summary>
        public static void Remove<T>(GameObject shell) where T : Component
        {
            foreach (T component in shell.GetComponents<T>())
            {
                Object.DestroyImmediate(component);
            }
        }
    }
}
