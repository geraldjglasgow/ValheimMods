using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// The look a creature ends with, from every definition of its chain, base-most first: sizes multiply (a size is
    /// relative to the base, so a copy of a creature twice the troll's size made half as big again is three times the
    /// troll), and for every other value the last definition that sets it wins (a tint, a texture or an overlay is what the
    /// creature looks like, not a change to stack). The look is put on once, in the chain's last pass, from this.
    /// </summary>
    internal sealed class LookPlan
    {
        public Vector3 Size = Vector3.one;
        public Color? BodyTint;
        public Color? ItemTint;
        public OverlayKind? Overlay;
        public Color? OverlayColour;
        public string? Texture;

        /// <summary>Whether its size differs from the base's.</summary>
        public bool Sized => Size != Vector3.one;

        /// <summary>Whether its body's materials change (on a machine that draws).</summary>
        public bool Dresses => BodyTint != null || Texture != null;

        /// <summary>
        /// Whether its corpse is its own copy of the base's: decided from the definitions alone, so every peer registers
        /// the same parts whatever files or graphics it has.
        /// </summary>
        public bool OwnCorpse => Sized || Dresses || ItemTint != null;

        public static LookPlan Of(IReadOnlyList<CreatureDefinition> chain)
        {
            LookPlan plan = new LookPlan();
            foreach (CreatureDefinition definition in chain)
            {
                plan.Take(definition.Look);
                plan.Texture = definition.Texture ?? plan.Texture;
            }
            return plan;
        }

        private void Take(LookBlock? look)
        {
            if (look == null)
            {
                return;
            }
            Size = Vector3.Scale(Size, look.Size ?? Vector3.one);
            BodyTint = look.BodyTint ?? BodyTint;
            ItemTint = look.ItemTint ?? ItemTint;
            Overlay = look.Overlay ?? Overlay;
            OverlayColour = look.OverlayColour ?? OverlayColour;
        }
    }
}
