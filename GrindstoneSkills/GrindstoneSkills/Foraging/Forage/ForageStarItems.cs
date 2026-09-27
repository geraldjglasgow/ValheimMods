using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Makes the forage items that roll stars carry them the way dishes do: each joins <see cref="Kitchen"/>'s items,
    /// which every star feature keys on (stacking apart, icons, tooltips, recipe counting, the ingredient bonus,
    /// eating). Items are added from every Forage file applied this session and never removed while the game runs, so
    /// a server's file that arrives after the local one only adds. Names are resolved once the item database exists:
    /// when a file applies, and after ZNetScene.Awake and ObjectDB.Awake (last, like <see cref="KitchenDiscovery"/>).
    /// </summary>
    public static class ForageStarItems
    {
        private static readonly HashSet<string> names = new HashSet<string>();

        /// <summary>Notes the file's starred items and adds those the item database already knows.</summary>
        public static void Remember(ForageModel model)
        {
            foreach (ForageEntry entry in model.Items.Values)
            {
                if (entry.Stars)
                    names.Add(entry.Item);
            }
            AddKnown();
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => AddKnown();
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabaseAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => AddKnown();
        }

        private static void AddKnown()
        {
            if (ObjectDB.instance == null)
                return;
            foreach (string name in names)
                Kitchen.AddItem(Kitchen.ItemPrefab(name));
        }
    }
}
