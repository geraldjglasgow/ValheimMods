using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Editing decks (<see cref="KwEdit"/>). The deck tool's click on a deck within <see cref="EndReach"/> of either end
    /// (a quarter of a short deck), or while a first click waits, is the tool's as before (the deck stands for the rib at
    /// its end, carrying its height, <see cref="KwTools.Resolve"/>); a click elsewhere on a deck opens the deck window on its
    /// height over the keel line and its material: one height for a level deck, one for each end of a sloping one. A deck's
    /// height stays between a little above the keel and a little under its ribs' tops (<see cref="KwSpan.Deck"/>); past
    /// that it is refused. Hatches, ladders and masts on an edited deck stay where they are.
    /// </summary>
    public sealed class KwDeckEdit : KwEditKind
    {
        /// <summary>Metres from a deck's end within which the deck tool's click is the end's, not an edit.</summary>
        public const float EndReach = 0.75f;

        private const string A = "height_a", B = "height_b", Level = "height";
        private const float Lowest = 0.15f;

        public KwDeckEdit() : base("deck", "deck", "decks")
        {
        }

        public override bool Keeps(KwClick click)
        {
            if (KwTools.Pending(Tool) != null)
                return true;
            ZDO? zdo = click.Piece.m_nview != null && click.Piece.m_nview.IsValid() ? click.Piece.m_nview.GetZDO() : null;
            if (zdo == null || !KwSpan.TryParse(zdo.GetString(KwGenerated.Key), out KwSpan span))
                return true;
            Transform deck = click.Piece.transform;
            return NearEnd(Vector3.Dot(click.Point - deck.position, deck.forward), span.Length);
        }

        /// <summary>Whether a point that far along a deck of that length is near one of its ends (a quarter of a short one).</summary>
        public static bool NearEnd(float along, float length)
        {
            float reach = Mathf.Min(EndReach, length / 4f);
            return along < reach || along > length - reach;
        }

        /// <summary>A deck's heights as it saves them: a number at each end, or empty for one it saves none of (laid at the default).</summary>
        public override KwPartValues? Read(string saved, KwBiomeTier tier)
        {
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return null;
            KwPartValues values = new KwPartValues();
            values[A] = float.IsNaN(span.DeckA) ? "" : KwPartValues.F(span.DeckA);
            values[B] = float.IsNaN(span.DeckB) ? "" : KwPartValues.F(span.DeckB);
            values[KwBiomeField.Key] = tier.Id;
            return values;
        }

        public override string? Write(KwPartValues values, string saved, out string why)
        {
            why = "the deck's saved span does not read";
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return null;
            float a = Height(values[A]), b = Height(values[B]);
            if (!Fits(a, span.FrameA, span.DeckA, out why) || !Fits(b, span.FrameB, span.DeckB, out why))
                return null;
            return new KwSpan(span.Family, span.FrameA, span.FrameB, span.Length, span.End, a, b, span.Seed) { Paint = span.Paint }.ToString();
        }

        private static float Height(string text) =>
            float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value) ? value : float.NaN;

        // A clicked height within the rib's: from a little above the keel to a little under its top. None saved, and the
        // deck's own (unchanged), always fit.
        private static bool Fits(float height, KwFrame frame, float own, out string why)
        {
            why = "";
            return float.IsNaN(height) || Mathf.Abs(height - own) < 0.0005f
                || Within(height, Lowest, frame.Height - 0.1f, "a deck would stand {0} m over the keel line; its ribs take {1} to {2} m", out why);
        }

        public override List<KwField> Fields(KwPartValues values, string saved)
        {
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return new List<KwField> { Material(KwPart.Deck, values) };
            float a = KwSpan.Deck(span.DeckA, span.FrameA), b = KwSpan.Deck(span.DeckB, span.FrameB);
            List<KwField> fields = Mathf.Abs(a - b) < 0.005f
                ? new List<KwField> { Height(Level, "Height", a, Mathf.Min(span.FrameA.Height, span.FrameB.Height)) }
                : new List<KwField> { Height(A, "Height at one end", a, span.FrameA.Height), Height(B, "Height at the other end", b, span.FrameB.Height) };
            fields.Add(Material(KwPart.Deck, values));
            return fields;
        }

        private static KwField Height(string key, string label, float value, float rib) =>
            KwField.Number(key, label, value, Lowest, Mathf.Max(Lowest, rib - 0.1f), 0.01f);

        /// <summary>The window's heights: one for a level deck, set at both ends; one for each end of a sloping one.</summary>
        public override KwPartValues Values(KwFormValues form, KwPartValues was)
        {
            KwPartValues values = was.Copy();
            bool level = form.Has(Level);
            values.Set(A, form.Number(level ? Level : A));
            values.Set(B, form.Number(level ? Level : B));
            Material(values, form);
            return values;
        }

        public override string Note(List<string> changed, int count) =>
            changed.Contains(A) || changed.Contains(B)
                ? $"Hatches, ladders and masts on {(count == 1 ? "this deck" : "these decks")} stay where they are."
                : "";
    }
}
