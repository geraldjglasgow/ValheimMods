using System;
using System.Collections.Generic;
using UnityEngine;

using Requirement = Piece.Requirement;

namespace OpenKeep.Boots
{
    /// <summary>
    /// A recipe per boots, beside its leggings' (same station, station level, repair station and order), and the
    /// leggings' cost split with it: each material's amount and amount per upgrade level gives a fifth (rounded) to the
    /// boots and keeps the rest. Boots always cost something, and something per upgrade when the leggings do: when every
    /// share rounds to nothing, one of the leggings' largest material moves over (or, with only single items, the boots
    /// ask one more). Then the boots are priced up (user, 2026-10-08): the early sets' share costs three times as much to
    /// craft, Padded's twice (<see cref="CraftFactor"/>), and no material less than two, to craft or per upgrade level (the
    /// game multiplies the per-level amount by 1, 2 and 4 for the upgrades to quality 2, 3 and 4, so each costs more than
    /// the last). Upgrade kits (the game's upgrader resources, used only at an upgrader station) are not split: both
    /// pieces keep the leggings' own. The game's recipes are copied in, never edited: switching off gives the leggings
    /// their own cost back and the boots recipes are switched off.
    /// </summary>
    public static class BootsRecipes
    {
        private const int Least = 2;

        /// <summary>The boots' craft cost over their fifth, by set key (others 1).</summary>
        private static readonly Dictionary<string, int> CraftFactor = new Dictionary<string, int>
        {
            ["Rag"] = 3, ["Leather"] = 3, ["TrollLeather"] = 3, ["Bronze"] = 3, ["Bear"] = 3, ["Root"] = 3,
            ["Vilebone"] = 3, ["Lox"] = 3, ["Padded"] = 2,
        };

        private static readonly Dictionary<BootSet, Recipe> boots = new Dictionary<BootSet, Recipe>();
        private static readonly Dictionary<Recipe, Requirement[]> own = new Dictionary<Recipe, Requirement[]>();

        /// <summary>Adds the boots recipes to the database's list (shared with the database it was copied from).</summary>
        public static void Install(ObjectDB db)
        {
            foreach (BootSet set in BootSets.All)
            {
                Recipe legs = set.Item != null ? LegsRecipe(db, set) : null;
                if (legs == null)
                    continue;
                Recipe recipe = RecipeOf(set, legs);
                if (!db.m_recipes.Contains(recipe))
                    db.m_recipes.Add(recipe);
            }
        }

        public static void Apply(ObjectDB db, bool on)
        {
            if (db == null)
                return;
            foreach (BootSet set in BootSets.All)
            {
                Recipe legs = boots.ContainsKey(set) ? LegsRecipe(db, set) : null;
                if (legs != null)
                    Apply(set, legs, boots[set], on);
            }
        }

        private static void Apply(BootSet set, Recipe legs, Recipe recipe, bool on)
        {
            if (!own.TryGetValue(legs, out Requirement[] cost))
                own[legs] = cost = legs.m_resources;
            Split(cost, out Requirement[] kept, out Requirement[] given);
            legs.m_resources = on ? kept : cost;
            recipe.m_resources = Priced(set, given);
            recipe.m_enabled = on && legs.m_enabled;
            recipe.m_craftingStation = legs.m_craftingStation;
            recipe.m_repairStation = legs.m_repairStation;
            recipe.m_minStationLevel = legs.m_minStationLevel;
            recipe.m_requireOnlyOneIngredient = legs.m_requireOnlyOneIngredient;
            recipe.m_listSortWeight = legs.m_listSortWeight;
        }

        private static Recipe LegsRecipe(ObjectDB db, BootSet set) =>
            db.m_recipes.Find(r => r != null && r.m_item != null && r.m_item.name == set.Legs);

        private static Recipe RecipeOf(BootSet set, Recipe legs)
        {
            if (boots.TryGetValue(set, out Recipe recipe))
                return recipe;
            recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = "Recipe_" + set.Prefab;
            recipe.m_item = set.Item.GetComponent<ItemDrop>();
            recipe.m_amount = 1;
            recipe.m_enabled = false;
            recipe.m_resources = new Requirement[0];
            boots[set] = recipe;
            return recipe;
        }

        /// <summary>The leggings' cost in two: what the leggings keep and what the boots take.</summary>
        public static void Split(Requirement[] cost, out Requirement[] kept, out Requirement[] given)
        {
            var keep = new List<Requirement>();
            var give = new List<Requirement>();
            var kits = new List<Requirement>();
            foreach (Requirement part in cost ?? new Requirement[0])
            {
                if (part == null || part.m_resItem == null)
                    continue;
                if (part.m_upgraderResource)
                {
                    kits.Add(part);
                    continue;
                }
                keep.Add(Copy(part, part.m_amount - Fifth(part.m_amount), part.m_amountPerLevel - Fifth(part.m_amountPerLevel)));
                give.Add(Copy(part, Fifth(part.m_amount), Fifth(part.m_amountPerLevel)));
            }
            AtLeastOne(keep, give, r => r.m_amount, (r, n) => r.m_amount = n);
            AtLeastOne(keep, give, r => r.m_amountPerLevel, (r, n) => r.m_amountPerLevel = n);
            kept = Present(keep, kits);
            given = Present(give, kits);
        }

        /// <summary>The parts that cost anything, then a copy of each upgrade kit as the game has it.</summary>
        private static Requirement[] Present(List<Requirement> parts, List<Requirement> kits)
        {
            List<Requirement> present = parts.FindAll(r => r.m_amount > 0 || r.m_amountPerLevel > 0);
            foreach (Requirement kit in kits)
                present.Add(Copy(kit, kit.m_amount, kit.m_amountPerLevel));
            return present.ToArray();
        }

        /// <summary>The boots' share priced up: the set's craft factor, then no material under two (kits as they are).</summary>
        private static Requirement[] Priced(BootSet set, Requirement[] given)
        {
            int factor = CraftFactor.TryGetValue(set.Key, out int f) ? f : 1;
            foreach (Requirement part in given)
            {
                if (part.m_upgraderResource)
                    continue;
                part.m_amount = AtLeastTwo(part.m_amount * factor);
                part.m_amountPerLevel = AtLeastTwo(part.m_amountPerLevel);
            }
            return given;
        }

        private static int AtLeastTwo(int amount) => amount > 0 ? Math.Max(Least, amount) : 0;

        /// <summary>When the boots get none of an amount the leggings have, one of the leggings' largest moves over.</summary>
        private static void AtLeastOne(List<Requirement> keep, List<Requirement> give, Func<Requirement, int> amount, Action<Requirement, int> set)
        {
            int largest = -1;
            for (int i = 0; i < keep.Count; i++)
            {
                if (amount(give[i]) > 0)
                    return;
                if (largest < 0 || amount(keep[i]) > amount(keep[largest]))
                    largest = i;
            }
            if (largest < 0 || amount(keep[largest]) <= 0)
                return;
            set(give[largest], 1);
            if (amount(keep[largest]) > 1)
                set(keep[largest], amount(keep[largest]) - 1);
        }

        /// <summary>A fifth, rounded half up.</summary>
        private static int Fifth(int amount) => (int)Math.Floor(amount * BootsStats.BootsShare + 0.5f);

        private static Requirement Copy(Requirement part, int amount, int perLevel) => new Requirement
        {
            m_resItem = part.m_resItem,
            m_amount = Math.Max(0, amount),
            m_amountPerLevel = Math.Max(0, perLevel),
            m_extraAmountOnlyOneIngredient = part.m_extraAmountOnlyOneIngredient,
            m_upgraderResource = part.m_upgraderResource,
            m_recover = part.m_recover,
        };
    }
}
