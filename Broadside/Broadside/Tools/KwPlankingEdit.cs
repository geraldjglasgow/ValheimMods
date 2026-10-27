using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Editing planking (<see cref="KwEdit"/>): the planking tool's click on a planking part, while no first click waits
    /// (one that waits takes the next click, as before), opens the planking window on that part's material and painted
    /// bands (<see cref="KwBandField"/>): each band's strake, width and colour; a band at strake 0 is none. Its shape is
    /// the ribs' it was built between and stays.
    /// </summary>
    public sealed class KwPlankingEdit : KwEditKind
    {
        private const string At = "at", Wide = "wide", Colour = "colour";

        public KwPlankingEdit() : base("planking", "planking part", "planking parts")
        {
        }

        public override bool Keeps(KwClick click) => KwTools.Pending(Tool) != null;

        public override KwPartValues? Read(string saved, KwBiomeTier tier)
        {
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return null;
            KwPartValues values = new KwPartValues();
            List<KwBand> bands = KwPaint.Read(span.Paint);
            for (int i = 0; i < KwBandField.Bands; i++)
            {
                KwBand band = i < bands.Count ? bands[i] : new KwBand(-1, -1, i);
                values.Set(KwBandField.Key(i, At), i < bands.Count ? band.From + 1 : 0);
                values.Set(KwBandField.Key(i, Wide), i < bands.Count ? band.To - band.From + 1 : 1);
                values[KwBandField.Key(i, Colour)] = KwPaint.Names[Mathf.Clamp(band.Colour, 0, KwPaint.Names.Length - 1)];
            }
            values[KwBiomeField.Key] = tier.Id;
            return values;
        }

        /// <summary>The part's own span with the values' bands painted on it (none at strake 0).</summary>
        public override string? Write(KwPartValues values, string saved, out string why)
        {
            why = "the planking's saved span does not read";
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return null;
            List<KwBand> bands = new List<KwBand>();
            for (int i = 0; i < KwBandField.Bands; i++)
            {
                int at = values.Whole(KwBandField.Key(i, At)), wide = Mathf.Max(1, values.Whole(KwBandField.Key(i, Wide), 1));
                int colour = Math.Max(0, Array.IndexOf(KwPaint.Names, values[KwBandField.Key(i, Colour)]));
                if (at > 0)
                    bands.Add(new KwBand(at - 1, at - 2 + wide, colour));
            }
            span.Paint = KwPaint.Write(bands);
            return span.ToString();
        }

        public override List<KwField> Fields(KwPartValues values, string saved)
        {
            List<(int Strake, int Wide, int Colour)> bands = new List<(int Strake, int Wide, int Colour)>();
            for (int i = 0; i < KwBandField.Bands; i++)
            {
                int colour = Math.Max(0, Array.IndexOf(KwPaint.Names, values[KwBandField.Key(i, Colour)]));
                bands.Add((values.Whole(KwBandField.Key(i, At)), values.Whole(KwBandField.Key(i, Wide), 1), colour));
            }
            List<KwField> fields = new List<KwField> { Material(KwPart.Hull, values) };
            fields.AddRange(KwBandField.Rows(KwSpan.TryParse(saved, out KwSpan span) ? KwBandField.Strakes(span) : 1, bands));
            return fields;
        }

        public override KwPartValues Values(KwFormValues form, KwPartValues was)
        {
            KwPartValues values = was.Copy();
            for (int i = 0; i < KwBandField.Bands; i++)
            {
                values.Set(KwBandField.Key(i, At), form.Number(KwBandField.Key(i, At)));
                values.Set(KwBandField.Key(i, Wide), form.Number(KwBandField.Key(i, Wide)));
                values[KwBandField.Key(i, Colour)] = KwPaint.Names[Mathf.Clamp(form.ChoiceIndex(KwBandField.Key(i, Colour)), 0, KwPaint.Names.Length - 1)];
            }
            Material(values, form);
            return values;
        }
    }
}
