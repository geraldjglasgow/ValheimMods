using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a railing wears and which tier it is priced in. Every railing, hull or roof, with a tier or without, wears the
    /// game's plank wood (<see cref="Look"/>), as railings always have: its planar UVs (<see cref="Solid"/>, two metres a
    /// texture) are made for that material's own texture scale, which draws the wood's grain along the rails and up the posts.
    /// A tier's look does not fit them: the deck textures (128 px, 4 m of boards running along v, a Plains deck of linen)
    /// drew boards across the rails at twice the deck's density, and the long planking texture would draw 64 m of hull in
    /// 2 m (see PROBLEMS.md, "Railings in the deck wood"). A railing built since the railing window (a hull railing or a roof
    /// railing, <see cref="KwRoofRail"/>) saves a biome tier of its own in its ZDO (<see cref="KwBiomeTiers.Key"/>), the
    /// window's Material, for its cost only; one saved without a tier, built before, is priced in the tier of the planking it
    /// stands on, as before (<see cref="KwBpCost"/>). The same on the dock, in a preview, in a blueprint's ghost and on a
    /// launched ship.
    /// </summary>
    public static class KwRailLooks
    {
        /// <summary>A railing's look, whatever its tier: the plank wood (null before the kit is made).</summary>
        public static Material? Look => KwPrefabs.Planks;

        /// <summary>Whether a part's ZDO saves a tier of its own.</summary>
        public static bool Tiered(ZDO? zdo) => zdo != null && zdo.GetString(KwBiomeTiers.Key).Length > 0;

        /// <summary>Whether a blueprint piece's saved values hold a tier of its own.</summary>
        public static bool Tiered(List<KwBpValue> data) => !string.IsNullOrEmpty(KwBpData.Text(data, KwBiomeTiers.Hash));
    }
}
