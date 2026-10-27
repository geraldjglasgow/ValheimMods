using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Editing cabins (<see cref="KwEdit"/>): the Cabin tool's click on a built cabin's walls (or the underside of its
    /// roof), while no first section waits, opens the Cabin tool's window (<see cref="KwCabinForm"/>) on its height, its
    /// four insets and its material; Shift edits every cabin on the dock, old castles too. A click on a cabin's roof's top
    /// stays the tool's: that is a section to build a cabin on. The cabin keeps the hull and deck it was built on. An edit
    /// that brings a side back to the hull takes the railing there away, in the same undo step, as building does
    /// (<see cref="KwCabinRails"/>); one that draws a side in gives no railing back. The cabins on its roof, all the way up, are
    /// set on its new roof and what stands on the roofs goes with them, in the same undo step and the same preview
    /// (<see cref="KwCabinFollow"/>). The window shows the values as they were
    /// asked and the note what was changed to fit them to the hull (<see cref="KwCabinFit"/>, an end drawn in where the hull
    /// narrows); values no cabin can be built from keep the window open on OK, the last cabin that could on show.
    /// </summary>
    public sealed class KwCabinEdit : KwEditKind
    {
        public KwCabinEdit() : base(KwCabinTool.Tool, "cabin", "cabins")
        {
        }

        /// <summary>A cabin part, or an old castle's (built again as a cabin by the edit).</summary>
        public override bool Edits(string prefab) => KwHouses.IsPart(prefab);

        public override bool Keeps(KwClick click) => KwCabinTool.Pending != null || KwCabinRoofs.OnRoof(click.Piece, click.Point);

        public override bool HoldsRefused => true;

        /// <summary>A cabin's values as they were asked (what its text saves), not as the fit built them.</summary>
        public override KwPartValues? Read(string saved, KwBiomeTier tier)
        {
            if (!KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                return null;
            KwCabinSizes asked = KwCabinSizes.Of(spec);
            KwPartValues values = new KwPartValues();
            values.Set(KwCabinForm.Height, asked.Height);
            values.Set(KwCabinForm.Back, asked.Back);
            values.Set(KwCabinForm.Front, asked.Front);
            values.Set(KwCabinForm.Left, asked.Left);
            values.Set(KwCabinForm.Right, asked.Right);
            values[KwBiomeField.Key] = tier.Id;
            return values;
        }

        public override string? Write(KwPartValues values, string saved, out string why)
        {
            why = "the cabin's saved shape does not read";
            if (!KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                return null;
            KwCabinSpec edited = spec.With(values.Number(KwCabinForm.Height), values.Number(KwCabinForm.Back), values.Number(KwCabinForm.Front),
                values.Number(KwCabinForm.Left), values.Number(KwCabinForm.Right));
            string? refused = edited.Unbuilt;
            why = refused != null ? char.ToLowerInvariant(refused[0]) + refused.Substring(1) : "";
            return refused == null ? edited.ToString() : null;
        }

        /// <summary>What was changed to fit the cabin as planned to its hull ("The hull is too narrow there: the back end is drawn in 0.62 m to fit.").</summary>
        public override string Said(string text) => KwCabinSpec.TryParse(text, out KwCabinSpec spec) ? string.Join(" ", spec.Fitted) : "";

        public override List<KwField> Fields(KwPartValues values, string saved)
        {
            KwBiomeTier tier = KwBiomeTiers.Of(values[KwBiomeField.Key]);
            if (!KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                return new List<KwField> { KwBiomeField.Field(KwPart.Hull, tier) };
            return KwCabinForm.Fields(KwCabinSizes.Of(spec), spec.Length, KwCabinForm.Wide(spec.Stations, spec.Floor), tier);
        }

        public override KwPartValues Values(KwFormValues form, KwPartValues was)
        {
            KwPartValues values = was.Copy();
            KwCabinSizes sizes = KwCabinForm.Read(form);
            values.Set(KwCabinForm.Height, sizes.Height);
            values.Set(KwCabinForm.Back, sizes.Back);
            values.Set(KwCabinForm.Front, sizes.Front);
            values.Set(KwCabinForm.Left, sizes.Left);
            values.Set(KwCabinForm.Right, sizes.Right);
            Material(values, form);
            return values;
        }

        public override string Note(List<string> changed, int count) =>
            changed.Contains(KwCabinForm.Left) || changed.Contains(KwCabinForm.Right)
                ? "A side brought back to the hull takes the railing there away; a side drawn in gets none back (build it with the railing tool)."
                : "";

        /// <summary>
        /// What goes with the cabins (<see cref="KwCabinFollow"/>): the cabins on their roofs set on the new roofs, all the way
        /// up, and the roof railings, ladders and masts on every roof that changes.
        /// </summary>
        public override void Follow(List<KwEditPlan> plans, List<string> changed, KwEditFollow follow) => KwCabinFollow.Plan(plans, changed, follow);

        /// <summary>
        /// The cabins built again with what goes with them (<see cref="Follow"/>), then in the same undo step the hull's railing
        /// taken away where a cabin's side now stands at the hull.
        /// </summary>
        public override void Rebuild(Player player, List<KwEditPlan> work, Action replace)
        {
            List<(Vector3 At, Vector3 Forward, KwCabinSpec Spec)> placed = new List<(Vector3, Vector3, KwCabinSpec)>();
            foreach (KwEditPlan plan in work)
            {
                Piece? piece = plan.Target.Piece;
                if (piece == null || plan.Take || !KwHouses.IsPart(Utils.GetPrefabName(piece.gameObject)) || !KwCabinSpec.TryParse(plan.Text, out KwCabinSpec spec))
                    continue;
                Vector3 forward = piece.transform.forward;
                placed.Add((piece.transform.position + plan.Shift, new Vector3(forward.x, 0f, forward.z).normalized, spec));
            }
            replace();
            foreach ((Vector3 at, Vector3 forward, KwCabinSpec spec) in placed)
                KwCabinRails.Take(player, at, forward, spec);
        }
    }
}
