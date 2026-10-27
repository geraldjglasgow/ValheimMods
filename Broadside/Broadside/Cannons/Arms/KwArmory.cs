using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Broadside
{
    /// <summary>
    /// Every ship cannon and cannon item, found by reflection in this assembly: each class with a parameterless
    /// constructor implementing <see cref="IKwArmSet"/> gives weapons, each implementing <see cref="IKwItemSet"/> items (a
    /// class may be both), so a weapon or an item is added in a file of its own and nothing shared is edited. Read once, in
    /// the sets' name order; a set that throws is logged and left out, a second weapon or item of a name is left out.
    /// The hammer gets every cannon (<see cref="Pieces"/>, made by <see cref="KwCannonPrefabs"/>).
    /// </summary>
    public static class KwArmory
    {
        private static List<KwArmDef>? arms;
        private static List<KwItemDef>? items;
        private static Dictionary<string, KwArmDef>? byId;

        public static IReadOnlyList<KwArmDef> Arms => arms ??= Unique(Find<IKwArmSet, KwArmDef>(set => set.Arms()), a => a.Id, "weapon");
        public static IReadOnlyList<KwItemDef> Items => items ??= Unique(Find<IKwItemSet, KwItemDef>(set => set.Items()), i => i.Name, "item");

        /// <summary>A weapon by its id; null for none.</summary>
        public static KwArmDef? Arm(string id)
        {
            byId ??= Arms.ToDictionary(a => a.Id);
            return byId.TryGetValue(id, out KwArmDef arm) ? arm : null;
        }

        /// <summary>A weapon by its prefab's name (<c>BS_</c> and its id); null for any other prefab.</summary>
        public static KwArmDef? OfPrefab(string prefab) =>
            prefab.StartsWith(KwArmDef.Prefix, StringComparison.Ordinal) ? Arm(prefab.Substring(KwArmDef.Prefix.Length)) : null;

        /// <summary>The hammer's weapon pieces: every weapon.</summary>
        public static IEnumerable<KwDef> Pieces() => Arms.Select(KwArmBuild.Def);

        /// <summary>Every shot a weapon fires, each weapon's shot and its other one.</summary>
        public static IEnumerable<KwShot> Shots() => Arms.SelectMany(a => a.Alternate != null ? new[] { a.Primary, a.Alternate } : new[] { a.Primary });

        /// <summary>An item prefab's name for a description: a cannon item's label, the prefab name for a game item.</summary>
        public static string Label(string prefab) => Items.FirstOrDefault(i => i.Name == prefab)?.Label ?? prefab;

        private static List<TDef> Find<TSet, TDef>(Func<TSet, IEnumerable<TDef>> take)
        {
            List<TDef> found = new List<TDef>();
            foreach (Type type in Types().Where(t => typeof(TSet).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null).OrderBy(t => t.FullName))
            {
                try
                {
                    found.AddRange(take((TSet)Activator.CreateInstance(type)));
                }
                catch (Exception error)
                {
                    Log.Warn($"Broadside cannons: the set {type.Name} threw and is left out: {error}");
                }
            }
            return found;
        }

        private static IEnumerable<Type> Types()
        {
            try
            {
                return typeof(KwArmory).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException partly)
            {
                return partly.Types.Where(t => t != null);
            }
        }

        private static List<T> Unique<T>(List<T> all, Func<T, string> name, string what)
        {
            HashSet<string> seen = new HashSet<string>();
            List<T> kept = new List<T>();
            foreach (T one in all)
            {
                if (seen.Add(name(one)))
                    kept.Add(one);
                else
                    Log.Warn($"Broadside cannons: a second {what} named {name(one)} is left out.");
            }
            return kept;
        }
    }
}
