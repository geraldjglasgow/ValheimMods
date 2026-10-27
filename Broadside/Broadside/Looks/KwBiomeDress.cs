using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A generated hull part dressed by its saved tier as it builds (<see cref="KwGenerated"/>), on every peer: its model
    /// wears the tier's look (<see cref="KwBiomeLooks"/>); a railing, tiered or not, the plank wood (<see cref="KwRailLooks"/>).
    /// </summary>
    public static class KwBiomeDress
    {
        public static void Apply(GameObject part, ZDO zdo, string tool)
        {
            KwPart? kind = KwBiomeTiers.PartOf(tool);
            if (kind != null)
                Wear(part.transform, KwBiomeLooks.For(kind.Value, KwBiomeTiers.Of(zdo)));
            else if (tool == KwBiomeCost.Rail)
                Wear(part.transform, KwRailLooks.Look);
        }

        /// <summary>A kit piece's model wearing a look (nothing changes without one).</summary>
        public static void Wear(Transform piece, Material? look)
        {
            MeshRenderer? renderer = piece.Find("model")?.GetComponent<MeshRenderer>();
            if (renderer != null && look != null && renderer.sharedMaterial != look)
                renderer.sharedMaterial = look;
        }
    }
}
