using System.Collections.Generic;
using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// One tracked recipe on screen. A header: the item's icon and name (in the Ready colour once every material is
    /// there), - amount + and X; the mouse wheel over it changes the amount too. Under it the station the recipe needs,
    /// then one row per material: icon, name, have/need, in the Have or Missing colour. A recipe that takes any one
    /// of its materials says so and is ready once one row is.
    /// </summary>
    public sealed class TrackerEntry
    {
        private readonly TrackedRecipe tracked;
        private readonly Recipe recipe;
        private readonly List<Piece.Requirement> materials = new List<Piece.Requirement>();
        private readonly List<TMP_Text> labels = new List<TMP_Text>();
        private readonly List<TMP_Text> counts = new List<TMP_Text>();
        private TMP_Text name;

        private TrackerEntry(TrackedRecipe tracked, Recipe recipe)
        {
            this.tracked = tracked;
            this.recipe = recipe;
        }

        public static TrackerEntry Build(Transform parent, TrackedRecipe tracked, Recipe recipe, int index)
        {
            TrackerEntry entry = new TrackerEntry(tracked, recipe);
            RectTransform root = TrackerUi.Node("OpenKeep_TrackerEntry", parent);
            TrackerUi.Column(root.gameObject, 1f);
            entry.Header(root, index);
            entry.Notes(root);
            foreach (Piece.Requirement requirement in recipe.m_resources)
            {
                if (TrackerCounts.Shows(requirement, tracked))
                    entry.Material(root, requirement);
            }
            return entry;
        }

        private void Header(Transform root, int index)
        {
            float size = TrackerStyle.Size;
            HorizontalLayoutGroup row = TrackerUi.Row(root, "header", 3f);
            Image hit = row.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            row.gameObject.AddComponent<TrackerWheel>().Index = index;
            TrackerUi.Icon(row.transform, recipe.m_item.m_itemData.GetIcon(), size * 1.6f);
            name = TrackerUi.Text(row.transform, "name", size, TextAlignmentOptions.MidlineLeft);
            name.text = TrackerList.DisplayName(recipe, tracked.Quality);
            TrackerUi.Flexible(name, size * 3f);
            TrackerUi.TextButton(row.transform, "less", "-", size, () => TrackerList.Step(index, -1, Shift));
            TMP_Text amount = TrackerUi.Text(row.transform, "amount", size, TextAlignmentOptions.Center);
            amount.text = "x" + tracked.Amount;
            TrackerUi.Fixed(amount.gameObject, size * 2.6f, size * 1.2f);
            TrackerUi.TextButton(row.transform, "more", "+", size, () => TrackerList.Step(index, 1, Shift));
            TrackerUi.TextButton(row.transform, "remove", "X", size * 0.85f, () => TrackerList.Remove(index));
        }

        /// <summary>The station and level the recipe needs, and the any-one-material hint, small and grey.</summary>
        private void Notes(Transform root)
        {
            CraftingStation station = recipe.GetRequiredStation(tracked.Quality);
            if (station != null)
                Note(root, TrackerWords.Format(TrackerWords.Station, Language.Localize(station.m_name), recipe.GetRequiredStationLevel(tracked.Quality)));
            if (recipe.m_requireOnlyOneIngredient)
                Note(root, Language.Localize(TrackerWords.AnyOne));
        }

        private static void Note(Transform root, string text)
        {
            TMP_Text note = TrackerUi.Text(root, "note", TrackerStyle.Size * 0.8f, TextAlignmentOptions.MidlineLeft);
            note.text = text;
            note.color = TrackerStyle.Dim;
            note.margin = new Vector4(TrackerStyle.Size * 1.6f + 3f, 0f, 0f, 0f);
        }

        private void Material(Transform root, Piece.Requirement requirement)
        {
            float size = TrackerStyle.Size;
            HorizontalLayoutGroup row = TrackerUi.Row(root, "material", 4f);
            row.padding = new RectOffset(Mathf.RoundToInt(size * 0.4f), 0, 0, 0);
            TrackerUi.Icon(row.transform, requirement.m_resItem.m_itemData.GetIcon(), size * 1.25f);
            TMP_Text label = TrackerUi.Text(row.transform, "name", size * 0.9f, TextAlignmentOptions.MidlineLeft);
            label.text = Language.Localize(requirement.m_resItem.m_itemData.m_shared.m_name);
            TrackerUi.Flexible(label, size * 3f);
            TMP_Text count = TrackerUi.Text(row.transform, "count", size * 0.9f, TextAlignmentOptions.MidlineRight);
            TrackerUi.Fixed(count.gameObject, size * 4.5f, size * 1.25f);
            materials.Add(requirement);
            labels.Add(label);
            counts.Add(count);
        }

        /// <summary>Every half second while shown: the counts, their colours and the name's ready colour.</summary>
        public void Count(Player player)
        {
            bool all = true;
            bool any = false;
            for (int i = 0; i < materials.Count; i++)
            {
                int have = TrackerCounts.Have(player, materials[i], tracked.Quality);
                int need = TrackerCounts.Need(materials[i], tracked.Quality, tracked.Amount);
                bool enough = have >= need;
                Paint(i, have + "/" + need, enough ? TrackerStyle.Have : TrackerStyle.Missing);
                all &= enough;
                any |= enough;
            }
            bool ready = recipe.m_requireOnlyOneIngredient ? any : all;
            name.color = ready ? TrackerStyle.Ready : TrackerStyle.Have;
        }

        private void Paint(int row, string text, Color colour)
        {
            if (counts[row].text != text)
                counts[row].text = text;
            counts[row].color = colour;
            labels[row].color = colour;
        }

        private static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }
}
