using System.Collections.Generic;
using System.Linq;
using OpenKeep.Batch;
using OpenKeep.Core;
using OpenKeep.Recipes;
using UnityEngine;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// The character's tracked recipes, in the order they were tracked, saved with the character in its custom data
    /// (<c>OpenKeep.trackedRecipes</c>). <see cref="Version"/> grows with every change, and when another character
    /// loads, so the tracker knows to rebuild. A new entry starts at the batch stepper's amount when that recipe is the
    /// one selected on the Craft tab, else at 1.
    /// </summary>
    public static class TrackerList
    {
        private const string DataKey = "trackedRecipes";

        private static List<TrackedRecipe> entries;
        private static Player loadedFor;

        public static int Version { get; private set; }

        public static List<TrackedRecipe> Entries
        {
            get
            {
                Player player = Player.m_localPlayer;
                if (entries == null || loadedFor != player)
                    Load(player);
                return entries;
            }
        }

        public static bool IsTracked(Recipe recipe, int quality) => IndexOf(recipe, quality) >= 0;

        /// <summary>
        /// The entry of that recipe and quality as made where the player stands (an upgrader station's entry is its
        /// own). Asked every frame by the crafting panel's Track button, so a plain loop.
        /// </summary>
        public static int IndexOf(Recipe recipe, int quality)
        {
            if (recipe == null)
                return -1;
            string key = RecipeKeys.Of(recipe);
            bool upgrader = RecipeNeeds.AtUpgrader;
            List<TrackedRecipe> list = Entries;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Is(key, quality, upgrader))
                    return i;
            }
            return -1;
        }

        /// <summary>Tracks the recipe at that quality, or stops tracking it; says which.</summary>
        public static void Toggle(Recipe recipe, int quality)
        {
            if (!TrackerSettings.Enabled.Value || recipe == null || recipe.m_item == null || Player.m_localPlayer == null)
                return;
            int index = IndexOf(recipe, quality);
            if (index >= 0)
            {
                Remove(index);
                return;
            }
            if (Entries.Count >= TrackerSettings.MaxTracked.Value)
            {
                Messages.Center(TrackerWords.Format(TrackerWords.Full, TrackerSettings.MaxTracked.Value));
                return;
            }
            Entries.Add(new TrackedRecipe(RecipeKeys.Of(recipe), quality, StartAmount(recipe, quality), RecipeNeeds.AtUpgrader));
            Save();
            Messages.Center(TrackerWords.Format(TrackerWords.Tracking, DisplayName(recipe, quality)));
        }

        public static void Remove(int index)
        {
            if (index < 0 || index >= Entries.Count)
                return;
            TrackedRecipe entry = Entries[index];
            Entries.RemoveAt(index);
            Save();
            Recipe recipe = entry.Recipe;
            if (recipe != null)
                Messages.Center(TrackerWords.Format(TrackerWords.Untracked, DisplayName(recipe, entry.Quality)));
        }

        /// <summary>One step (-1 or 1) of an entry's amount; with Shift to the next multiple of ten.</summary>
        public static void Step(int index, int direction, bool tens)
        {
            if (index < 0 || index >= Entries.Count)
                return;
            int amount = Entries[index].Amount;
            int target = !tens ? amount + direction : direction > 0 ? (amount / 10 + 1) * 10 : (amount - 1) / 10 * 10;
            Entries[index].Amount = Mathf.Clamp(target, 1, TrackedRecipe.MaxAmount);
            Save();
        }

        /// <summary>After a craft of that recipe and quality: counted down, and off the tracker once all are made.</summary>
        public static void Crafted(Recipe recipe, int quality, int crafts)
        {
            int index = IndexOf(recipe, quality);
            if (index < 0 || !TrackerSettings.UntrackWhenCrafted.Value)
                return;
            TrackedRecipe entry = Entries[index];
            entry.Amount -= Mathf.Max(1, crafts);
            if (entry.Amount > 0)
            {
                Save();
                return;
            }
            Entries.RemoveAt(index);
            Save();
            Messages.TopLeft(TrackerWords.Format(TrackerWords.Made, DisplayName(recipe, quality)));
        }

        /// <summary>The item's name in the game's language, with the level for an upgrade.</summary>
        public static string DisplayName(Recipe recipe, int quality)
        {
            string name = Language.Localize(recipe.m_item.m_itemData.m_shared.m_name);
            return quality > 1 ? TrackerWords.Format(TrackerWords.Upgrade, name, quality) : name;
        }

        private static int StartAmount(Recipe recipe, int quality)
        {
            return quality == 1 && BatchAmount.Current == recipe ? Mathf.Max(1, BatchAmount.Value) : 1;
        }

        private static void Load(Player player)
        {
            loadedFor = player;
            entries = CharacterData.GetList(DataKey).Select(TrackedRecipe.Parse).Where(entry => entry != null).ToList();
            Version++;
        }

        private static void Save()
        {
            CharacterData.SetSet(DataKey, Entries.Select(entry => entry.ToString()));
            Version++;
        }
    }
}
