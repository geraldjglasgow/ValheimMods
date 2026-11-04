using System;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One dockside decoration as plain data (<see cref="KwDockside"/>): its prefab (<c>BS_dock_&lt;id&gt;</c>), its workshop
    /// bundle and prefab (<c>bs_dock_&lt;id&gt;</c>, with the sprite <c>bs_dock_&lt;id&gt;_icon</c>), its name and words, the
    /// game piece it is a copy of (its placement, health, wear and sounds), the gloss of its metal (the workshop's
    /// <c>METAL_GLOSS</c>; 0 where the model has no metal mask), its recipe (items by the game's prefab names, given back
    /// when it is removed) and what it does beyond standing there (<see cref="Fit"/>: the beacon's fire, the vane's turning,
    /// the tar pot's and steam box's small fires, the lantern pole's night light, the bell's ringing, the shavings' floor box).
    /// </summary>
    public sealed class KwDockDef
    {
        public KwDockDef(string id, string label, string blurb, float metalGloss, params (string Item, int Amount)[] cost)
        {
            (Prefab, Bundle, Label, Blurb) = (KwDockside.Prefix + id, "bs_dock_" + id, label, blurb);
            (MetalGloss, Cost) = (metalGloss, cost);
        }

        // A plain decoration's copy in a wood tier: its wood (the plain Wood) the tier's, the rest the same.
        private KwDockDef(KwDockDef plain, KwDockTier tier)
        {
            (Prefab, Bundle, Label, Blurb, Tier) = (tier.Prefab(plain.Prefab), plain.Bundle, $"{plain.Label} ({tier.Words})", plain.Blurb, tier);
            (MetalGloss, Cost) = (plain.MetalGloss, Array.ConvertAll(plain.Cost, c => c.Item == "Wood" ? (tier.Item, c.Amount) : c));
            (Base, Usage, KeepsComfort, NotOnWood, Fit, Decor) = (plain.Base, plain.Usage, plain.KeepsComfort, plain.NotOnWood, plain.Fit, plain.Decor);
        }

        public string Prefab { get; }

        /// <summary>Its workshop bundle and prefab: <c>bs_dock_&lt;id&gt;</c> unless set (the dock lantern wears <c>bs_dock_lanternhead</c>).</summary>
        public string Bundle { get; set; }

        public string Label { get; }

        /// <summary>Its id: the prefab without <c>BS_dock_</c> (a tier copy's ends in <c>_core</c> or <c>_fine</c>).</summary>
        public string Id => Prefab.Substring(KwDockside.Prefix.Length);

        /// <summary>The wood tier of a copy made in one (<see cref="KwDockTiers"/>); null for the plain decoration.</summary>
        public KwDockTier? Tier { get; }

        /// <summary>This decoration in a wood tier: the same piece wearing the tier's albedo and costing its wood.</summary>
        public KwDockDef In(KwDockTier tier) => new KwDockDef(this, tier);

        /// <summary>What it is, in a sentence.</summary>
        public string Blurb { get; }

        public float MetalGloss { get; }
        public (string Item, int Amount)[] Cost { get; }

        /// <summary>The game piece copied; the game's table (wood furniture, no component of its own) unless set.</summary>
        public string Base { get; set; } = KwDockside.Table;

        /// <summary>A build menu filter besides Broadside's (Transportation) and Decoration: Lighting for the beacon.</summary>
        public Piece.UsageTagFlags Usage { get; set; }

        /// <summary>Whether the base's comfort stays (the beacon's fire); a decoration otherwise gives none.</summary>
        public bool KeepsComfort { get; set; }

        /// <summary>Kept off wooden floors as the game keeps its fires (<c>m_notOnWood</c>): the tar pot and steam box burn a small fire.</summary>
        public bool NotOnWood { get; set; }

        /// <summary>
        /// Run on the copy (still holding its base's children) and its workshop model just put under it, before the base's
        /// children are stripped: what the piece keeps of its base or adds of its own. Null for a plain decoration.
        /// </summary>
        public Action<GameObject, GameObject>? Fit { get; set; }

        /// <summary>
        /// Where the auto-decorate tool may place it, and how (<see cref="KwDecorFit"/>, Decorate/DECORATE.md); null: the tool
        /// never places it. A <see cref="KwDecorFit.Wet"/> piece may also be placed by hand at the waterline and in shallow water.
        /// </summary>
        public KwDecorFit? Decor { get; set; }

        public string Description => Tier == null ? $"{Blurb} A decoration, built at a workbench." : $"{Blurb} In {Tier.Words}, to go with a {Tier.Words} dock. A decoration, built at a workbench.";
    }
}
