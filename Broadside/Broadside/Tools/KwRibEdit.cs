using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Editing ribs (<see cref="KwEdit"/>): the rib tool's click on a rib opens the rib window on its hull shape, width,
    /// height and material (no "How many": the rib is changed where it stands). It stays on the keel width it was built
    /// on. Planking, decks and railings saved the frames of the ribs they were built between, so they keep their old shape
    /// after a rib is edited, and the note line says so.
    /// </summary>
    public sealed class KwRibEdit : KwEditKind
    {
        private static readonly string[] Shaping = { "shape", "width", "height" };

        public KwRibEdit() : base(KwShapeTool.Rib, "rib", "ribs")
        {
        }

        public override KwPartValues? Read(string saved, KwBiomeTier tier)
        {
            if (!KwRibs.TryRead(saved, out string family, out KwFrame frame))
                return null;
            KwPartValues values = new KwPartValues();
            values["shape"] = family;
            values.Set("width", frame.Beam);
            values.Set("height", frame.Height);
            values[KwBiomeField.Key] = tier.Id;
            return values;
        }

        /// <summary>A rib's text; its frame kept as it was (a preset's number too) while its width and height are its own.</summary>
        public override string? Write(KwPartValues values, string saved, out string why)
        {
            why = "the rib's saved shape does not read";
            string family = values["shape"];
            if (!KwRibs.TryRead(saved, out _, out KwFrame was) || Array.IndexOf(KwRibs.Families, family) < 0)
                return null;
            float beam = values.Number("width"), height = values.Number("height");
            if (!Within(beam, KwFrame.MinBeam, KwFrame.MaxBeam, "a rib would be {0} m wide; ribs are {1} to {2} m", out why)
                || !Within(height, KwFrame.MinHeight, KwFrame.MaxHeight, "a rib would be {0} m tall; ribs are {1} to {2} m", out why))
                return null;
            bool same = KwPartValues.F(was.Beam) == values["width"] && KwPartValues.F(was.Height) == values["height"];
            return KwRibs.Save(family, same ? was : new KwFrame(beam, height, 0, was.KeelHalf));
        }

        public override List<KwField> Fields(KwPartValues values, string saved)
        {
            float width = values.Number("width"), height = values.Number("height");
            return new List<KwField>
            {
                KwField.Choice("shape", "Shape", KwShapeTool.Shapes, Array.IndexOf(KwRibs.Families, values["shape"])),
                KwField.Number("width", "Width", width, KwFrame.MinBeam, KwFrame.MaxBeam, Step(width, 0.25f)),
                KwField.Number("height", "Height", height, KwFrame.MinHeight, KwFrame.MaxHeight, Step(height, 0.1f)),
                Material(KwPart.Keel, values),
            };
        }

        public override KwPartValues Values(KwFormValues form, KwPartValues was)
        {
            KwPartValues values = was.Copy();
            values["shape"] = KwRibs.Families[Mathf.Clamp(form.ChoiceIndex("shape"), 0, KwRibs.Families.Length - 1)];
            values.Set("width", form.Number("width"));
            values.Set("height", form.Number("height"));
            Material(values, form);
            return values;
        }

        public override string Note(List<string> changed, int count) =>
            changed.Exists(key => Array.IndexOf(Shaping, key) >= 0)
                ? $"Planking, decks and railings on {(count == 1 ? "this rib keep their" : "these ribs keep their")} old shape; build them again to fit."
                : "";
    }
}
