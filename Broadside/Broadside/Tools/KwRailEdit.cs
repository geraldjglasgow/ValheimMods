using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Editing railings (<see cref="KwEdit"/>): the railing tool's click on a railing, while no first click waits, opens its
    /// window on the posts' spacing and the material; a hull railing's also on its sides (both, port or starboard, read from
    /// its own left or right by the way it runs to the ship's bow, <see cref="KwRailBow"/>, the edit's "runs" value, never
    /// changed); a roof railing's (<see cref="KwRoofRail"/>) on each roof edge, on or off, and, when it has gaps for
    /// ladders, whether they stay. Shift edits every railing on the dock, hull and roof alike, each given only what the
    /// player changed (a hull railing has no edges to turn, a roof railing no sides; Port given to every hull railing puts
    /// each on the ship's port side, whichever way it runs). A railing built before railings had a spacing keeps its posts
    /// at its stations until the spacing is changed; one built before they had a material saves none (priced as its
    /// planking) until one is picked; every railing wears the plank wood, its material its cost only (<see cref="KwRailLooks"/>).
    /// A click on what a railing stands over edits it too (<see cref="Over"/>): a cabin's roof railed already, or the
    /// planking, a rib or a keel end under a hull railing's stretch on a side it has (<see cref="KwRailOver"/>).
    /// </summary>
    public sealed class KwRailEdit : KwEditKind
    {
        private const string Posts = KwRailField.Posts, Sides = KwRailField.HullSides, Runs = KwRailField.Runs;

        public KwRailEdit() : base(KwBiomeCost.Rail, "railing", "railings")
        {
        }

        public override bool Keeps(KwClick click) => KwTools.Pending(Tool) != null;

        /// <summary>The railing on a cabin's roof clicked, or the hull railing over the hull clicked; none while a first click waits.</summary>
        public override Piece? Over(KwClick click)
        {
            if (KwTools.Pending(Tool) != null)
                return null;
            KwRoofSite? roof = KwRoofSite.Of(click.Piece);
            return roof != null ? roof.Railed() : KwRailOver.At(click);
        }

        public override KwPartValues? Read(string saved, KwBiomeTier tier) => Read(saved, tier, true);

        /// <summary>A railing's values: its spacing ("" for posts at its stations), a roof railing's edges, its tier when it saves one.</summary>
        public override KwPartValues? Read(string saved, KwBiomeTier tier, bool tiered)
        {
            KwPartValues values = new KwPartValues();
            if (KwRoofRail.TryParse(saved, out KwRoofRail roof))
                Roof(values, roof);
            else if (KwSpan.TryParse(saved, out KwSpan span))
                values[Posts] = KwRailText.Spacing(span.Rail) > 0f ? KwPartValues.F(KwRailText.Spacing(span.Rail)) : "";
            else
                return null;
            if (tiered)
                values[KwBiomeField.Key] = tier.Id;
            return values;
        }

        /// <summary>A railing standing on the dock: a hull railing's sides also, as port and starboard from the way it runs.</summary>
        public override KwPartValues? Read(Piece piece, string saved, KwBiomeTier tier, bool tiered)
        {
            KwPartValues? values = Read(saved, tier, tiered);
            if (values == null || piece == null || KwRoofRail.TryParse(saved, out _) || !KwSpan.TryParse(saved, out KwSpan span))
                return values;
            float runs = KwRailBow.Runs(piece.transform.position, piece.transform.forward);
            values.Set(Runs, runs);
            values[Sides] = KwRailBow.Name(KwRailText.Sides(span.Rail), runs);
            return values;
        }

        // A roof railing's spacing, each of its edges on ("1") or off ("0"), its gaps kept.
        private static void Roof(KwPartValues values, KwRoofRail roof)
        {
            values.Set(Posts, roof.Spacing);
            foreach (string side in KwRoofRail.Sides)
                values[side] = roof.Has(side[0]) ? "1" : "0";
            values[KwRailField.Gaps] = "keep";
        }

        public override string? Write(KwPartValues values, string saved, out string why)
        {
            why = "";
            float spacing = values.Number(Posts);
            if (values[Posts].Length > 0 && !Within(spacing, KwRailText.Least, KwRailText.Most, "posts would stand {0} m apart; railings take {1} to {2} m", out why))
                return null;
            if (KwRoofRail.TryParse(saved, out KwRoofRail roof))
                return RoofText(values, roof, out why);
            if (KwSpan.TryParse(saved, out KwSpan span))
                return KwRailText.With(saved, values[Posts].Length > 0 ? spacing : 0f, HullText(values, span));
            why = "the railing's saved text does not read";
            return null;
        }

        // A hull railing's sides from its values: port, starboard or both turned into its own left or right as it runs; its
        // own sides for values without them.
        private static string HullText(KwPartValues values, KwSpan span) =>
            values.Has(Sides) ? KwRailBow.Letters(values[Sides], values.Number(Runs, 1f)) : KwRailText.Sides(span.Rail);

        // A roof railing's text from its values: its edges on or off, its spacing, its gaps kept or closed.
        private static string? RoofText(KwPartValues values, KwRoofRail roof, out string why)
        {
            string on = "";
            foreach (string side in KwRoofRail.Sides)
                on += values[side] == "1" ? side[0].ToString() : "";
            why = on.Length == 0 ? "a roof railing needs at least one edge on" : "";
            if (on.Length == 0)
                return null;
            KwRoofRail made = roof.With(values.Number(Posts), on);
            return (values[KwRailField.Gaps] == "close" ? new KwRoofRail(made.Spacing, made.On, made.Edges, new List<Vector2>()) : made).ToString();
        }

        public override List<KwField> Fields(KwPartValues values, string saved)
        {
            List<KwField> fields = new List<KwField>();
            if (KwRoofRail.TryParse(saved, out KwRoofRail roof))
                fields.AddRange(KwRailField.SideRows(roof.On));
            fields.Add(KwRailField.Spacing(values[Posts].Length > 0 ? values.Number(Posts) : KwRailText.Usual));
            if (roof == null && values.Has(Sides))
                fields.Add(KwRailField.SidesRow(KwRailBow.Index(values[Sides])));
            KwBiomeTier shown = values.Has(KwBiomeField.Key) ? KwBiomeTiers.Of(values[KwBiomeField.Key]) : KwBiomeTiers.Meadows;
            fields.Add(KwBiomeField.Field(KwPart.Deck, shown));
            if (roof != null && roof.Gaps.Count > 0)
                fields.Add(KwRailField.GapRow(roof.Gaps.Count));
            return fields;
        }

        public override KwPartValues Values(KwFormValues form, KwPartValues was)
        {
            KwPartValues values = was.Copy();
            values.Set(Posts, form.Number(Posts));
            foreach (string side in KwRoofRail.Sides)
            {
                if (form.Has(side))
                    values[side] = form.ChoiceIndex(side) == 0 ? "1" : "0";
            }
            if (form.Has(KwRailField.Gaps))
                values[KwRailField.Gaps] = form.ChoiceIndex(KwRailField.Gaps) == 0 ? "keep" : "close";
            if (form.Has(Sides))
                values[Sides] = KwRailBow.Names[Mathf.Clamp(form.ChoiceIndex(Sides), 0, KwRailBow.Names.Length - 1)];
            Material(values, form);
            return values;
        }

        public override string Note(List<string> changed, int count) =>
            (changed.Contains(KwRailField.Gaps) ? "Closed gaps stay closed: a ladder built there later cuts its own again. " : "")
            + (changed.Contains(Sides) ? "Port and starboard go by the bow (the rudder, else the masts; else the way you look). " : "")
            + "Shields hung on a railing changed drop off as it is built again.";
    }
}
