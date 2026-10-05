using System.Collections.Generic;
using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// One tracked recipe on screen, as wide as its words. A header: the item's icon and name (in the Ready colour once
    /// every material is there) and its amount when more than one; while the cursor is free (the inventory open) also
    /// an X at its left that removes it and - and + round the amount. The mouse wheel over the header changes the
    /// amount too. Under it the station the recipe needs, then one row per material: icon, name, have/need, in the
    /// Have or Missing colour. A recipe that takes any one of its materials says so and is ready once one row is.
    /// </summary>
    public sealed class TrackerEntry
    {
        private const float Gap = 3f;

        private readonly TrackedRecipe tracked;
        private readonly Recipe recipe;
        private readonly List<Piece.Requirement> materials = new List<Piece.Requirement>();
        private readonly List<TMP_Text> labels = new List<TMP_Text>();
        private readonly List<TMP_Text> counts = new List<TMP_Text>();
        private readonly List<TMP_Text> notes = new List<TMP_Text>();
        private readonly List<GameObject> controls = new List<GameObject>();
        private TMP_Text name;
        private TMP_Text amount;

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
            HorizontalLayoutGroup row = TrackerUi.Row(root, "header", Gap);
            Image hit = row.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            row.gameObject.AddComponent<TrackerWheel>().Index = index;
            Control(TrackerUi.TextButton(row.transform, "remove", "X", size * 0.85f, () => TrackerList.Remove(index)));
            TrackerUi.Icon(row.transform, recipe.m_item.m_itemData.GetIcon(), size * 1.6f);
            name = TrackerUi.Text(row.transform, "name", size, TextAlignmentOptions.MidlineLeft);
            name.text = TrackerList.DisplayName(recipe, tracked.Quality);
            TrackerUi.Flexible(name, size * 11f);
            Amount(row.transform, index);
        }

        /// <summary>- xN + at the header's end.</summary>
        private void Amount(Transform row, int index)
        {
            float size = TrackerStyle.Size;
            Control(TrackerUi.TextButton(row, "less", "-", size, () => TrackerList.Step(index, -1, Shift)));
            amount = TrackerUi.Text(row, "amount", size, TextAlignmentOptions.Center);
            amount.text = "x" + tracked.Amount;
            TrackerUi.Short(amount, size * 1.6f);
            Control(TrackerUi.TextButton(row, "more", "+", size, () => TrackerList.Step(index, 1, Shift)));
        }

        private void Control(Button button) => controls.Add(button.gameObject);

        /// <summary>The station and level the recipe needs, and the any-one-material hint, small and grey.</summary>
        private void Notes(Transform root)
        {
            CraftingStation station = recipe.GetRequiredStation(tracked.Quality);
            if (station != null)
                Note(root, TrackerWords.Format(TrackerWords.Station, Language.Localize(station.m_name), recipe.GetRequiredStationLevel(tracked.Quality)));
            if (recipe.m_requireOnlyOneIngredient)
                Note(root, Language.Localize(TrackerWords.AnyOne));
        }

        private void Note(Transform root, string text)
        {
            TMP_Text note = TrackerUi.Text(root, "note", TrackerStyle.Size * 0.8f, TextAlignmentOptions.MidlineLeft);
            note.text = text;
            note.color = TrackerStyle.Dim;
            notes.Add(note);
        }

        private void Material(Transform root, Piece.Requirement requirement)
        {
            float size = TrackerStyle.Size;
            HorizontalLayoutGroup row = TrackerUi.Row(root, "material", 6f);
            row.padding = new RectOffset(Mathf.RoundToInt(size * 0.4f), 0, 0, 0);
            TrackerUi.Icon(row.transform, requirement.m_resItem.m_itemData.GetIcon(), size * 1.25f);
            TMP_Text label = TrackerUi.Text(row.transform, "name", size * 0.9f, TextAlignmentOptions.MidlineLeft);
            label.text = Language.Localize(requirement.m_resItem.m_itemData.m_shared.m_name);
            TrackerUi.Flexible(label, size * 9f);
            TMP_Text count = TrackerUi.Text(row.transform, "count", size * 0.9f, TextAlignmentOptions.MidlineRight);
            TrackerUi.Short(count, size * 3f);
            materials.Add(requirement);
            labels.Add(label);
            counts.Add(count);
        }

        /// <summary>
        /// The X, - and + only while the cursor is free, the amount then too, else only when more than one; the notes
        /// stay under the name, past the X while it shows.
        /// </summary>
        public void ShowControls(bool open)
        {
            foreach (GameObject control in controls)
                control.SetActive(open);
            amount.gameObject.SetActive(open || tracked.Amount > 1);
            float size = TrackerStyle.Size;
            float indent = size * 1.6f + Gap + (open ? size * 0.85f + Gap : 0f);
            foreach (TMP_Text note in notes)
                note.margin = new Vector4(indent, 0f, 0f, 0f);
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
