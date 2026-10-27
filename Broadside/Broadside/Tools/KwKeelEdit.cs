using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Editing keels (<see cref="KwEdit"/>). The keel tool's click on a keel within <see cref="EndReach"/> along it of one of
    /// its open ends (a "Keel joint") picks, joins and carries keels on as before (<see cref="KwKeelPick"/>); a click
    /// anywhere else on it opens the keel window on that keel's length, rise, curve, post height, thickness and material
    /// (and forgets a keel picked before). A keel joining two others keeps no post, so it climbs by a curve only, never to a
    /// flat stern. Ribs, post caps and rudders on an edited keel stay where they are; planking ending at its end meets it as
    /// it now stands (<see cref="KwSpanEnds"/>).
    /// </summary>
    public sealed class KwKeelEdit : KwEditKind
    {
        /// <summary>Metres along a keel from an open end within which the keel tool's click is the end's, not an edit.</summary>
        public const float EndReach = 0.75f;

        // the saved curve letters in the order of KwKeelCurve and the keel window's curves
        private static readonly string[] Letters = { "q", "o", "f" };
        private static readonly string[] Shaping = { "length", "rise", "curve", "post_height", "width", "depth" };

        public KwKeelEdit() : base(KwShapeTool.Keel, "keel", "keels")
        {
        }

        public override bool Keeps(KwClick click) => NearEnd(click.Point, click.Piece.transform.forward, Joints(click.Piece));

        /// <summary>Whether a point on a keel is within <see cref="EndReach"/> along it (its forward) of one of its open ends.</summary>
        public static bool NearEnd(Vector3 point, Vector3 forward, IEnumerable<Vector3> joints)
        {
            Vector3 along = forward.normalized;
            foreach (Vector3 joint in joints)
            {
                if (Mathf.Abs(Vector3.Dot(point - joint, along)) <= EndReach)
                    return true;
            }
            return false;
        }

        // A keel's open ends: its "Keel joint" snap points.
        private static List<Vector3> Joints(Piece keel)
        {
            List<Vector3> joints = new List<Vector3>();
            foreach (Transform child in keel.transform)
            {
                if (child.name == "Keel joint" && child.CompareTag("snappoint"))
                    joints.Add(child.position);
            }
            return joints;
        }

        public override void Open(KwEditSession session)
        {
            KwKeelPick.Drop();
            base.Open(session);
        }

        public override KwPartValues? Read(string saved, KwBiomeTier tier)
        {
            if (!KwKeelSpec.TryParse(saved, out KwKeelSpec spec))
                return null;
            KwPartValues values = new KwPartValues();
            values.Set("length", spec.Length);
            values.Set("rise", spec.Rise);
            values["curve"] = Letters[(int)spec.Curve];
            values.Set("post_height", spec.PostAbove);
            values.Set("width", spec.Body.Half * 2f);
            values.Set("depth", spec.Body.Depth);
            values[KwBiomeField.Key] = tier.Id;
            return values;
        }

        public override string? Write(KwPartValues values, string saved, out string why)
        {
            why = "the keel's saved sizes do not read";
            if (!KwKeelSpec.TryParse(saved, out KwKeelSpec was))
                return null;
            KwKeelCurve curve = (KwKeelCurve)Mathf.Max(0, Array.IndexOf(Letters, values["curve"]));
            why = "a keel joining two others has no post, so it climbs by a curve, never to a flat stern";
            if (!was.Post && curve == KwKeelCurve.Flat)
                return null;
            KwKeelBody body = new KwKeelBody(values.Number("width") / 2f, values.Number("depth"));
            KwKeelSpec spec = new KwKeelSpec(values.Number("length"), values.Number("rise"), curve, was.Post, values.Number("post_height"), body);
            return Fits(spec, out why) ? spec.ToString() : null;
        }

        // Within the keel tool's limits: length, rise, post height, width and depth.
        private static bool Fits(KwKeelSpec spec, out string why) =>
            Within(spec.Length, KwKeelSpec.MinLength, KwKeelSpec.MaxLength, "a keel would be {0} m long; keels are {1} to {2} m", out why)
            && Within(spec.Rise, 0f, KwKeelSpec.MaxRise, "a keel end would climb {0} m; at most {2} m", out why)
            && Within(spec.PostAbove, KwKeelSpec.MinPost, KwKeelSpec.MaxPost, "a post would stand {0} m; posts are {1} to {2} m", out why)
            && Within(spec.Body.Half * 2f, KwKeelBody.MinWidth, KwKeelBody.MaxWidth, "a keel would be {0} m wide; keels are {1} to {2} m", out why)
            && Within(spec.Body.Depth, KwKeelBody.MinDepth, KwKeelBody.MaxDepth, "a keel would be {0} m deep; keels are {1} to {2} m", out why);

        /// <summary>The keel window's rows as the keel tool has them, on the keel's own values (a keel with no post climbs by a curve only).</summary>
        public override List<KwField> Fields(KwPartValues values, string saved)
        {
            bool post = !KwKeelSpec.TryParse(saved, out KwKeelSpec spec) || spec.Post;
            float length = values.Number("length"), rise = values.Number("rise"), top = values.Number("post_height");
            float width = values.Number("width"), depth = values.Number("depth");
            return new List<KwField>
            {
                KwField.Number("length", "Length", length, KwKeelSpec.MinLength, KwKeelSpec.MaxLength, Step(length, 0.5f)),
                KwField.Number("rise", "Rise at its end", rise, 0f, KwKeelSpec.MaxRise, Step(rise, 0.1f)),
                KwField.Choice("curve", "Curve", post ? KwShapeTool.Curves : KwShapeTool.JoinCurves, Array.IndexOf(Letters, values["curve"])),
                KwField.Number("post_height", "Post height", top, KwKeelSpec.MinPost, KwKeelSpec.MaxPost, Step(top, 0.25f)),
                KwField.Number("width", "Keel width", width, KwKeelBody.MinWidth, KwKeelBody.MaxWidth, Step(width, 0.02f)),
                KwField.Number("depth", "Keel depth", depth, KwKeelBody.MinDepth, KwKeelBody.MaxDepth, Step(depth, 0.05f)),
                Material(KwPart.Keel, values),
            };
        }

        public override KwPartValues Values(KwFormValues form, KwPartValues was)
        {
            KwPartValues values = was.Copy();
            foreach (string key in new[] { "length", "rise", "post_height", "width", "depth" })
                values.Set(key, form.Number(key));
            values["curve"] = Letters[Mathf.Clamp(form.ChoiceIndex("curve"), 0, Letters.Length - 1)];
            Material(values, form);
            return values;
        }

        public override string Note(List<string> changed, int count) =>
            changed.Exists(key => Array.IndexOf(Shaping, key) >= 0)
                ? $"Ribs, post caps and rudders on {(count == 1 ? "this keel" : "these keels")} stay where they are: place them again to fit."
                : "";
    }
}
