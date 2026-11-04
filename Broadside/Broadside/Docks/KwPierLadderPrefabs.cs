using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The dock ladders' prefabs, <c>BS_pier_ladder_wood</c>, <c>_corewood</c>, <c>_finewood</c> ("Dock ladder (wood)", ...),
    /// made with the docks (<see cref="KwPierPrefabs"/>) on every peer and never in the hammer: the ladder tool builds them
    /// on a dock's edge (<see cref="KwPierLadderTool"/>). Each a copy of the game's wood floor (its Piece, WearNTear, network
    /// view, place and break effects) stripped of its look and colliders, with an empty model its shape fills
    /// (<see cref="KwPierLadder"/>), the kit's climb (<see cref="KwLadderClimb"/>), its material's support rules and a
    /// ladder's health, supporting nothing built against it, at a workbench for removal.
    /// </summary>
    public static class KwPierLadderPrefabs
    {
        public const string Prefix = "BS_pier_ladder_";

        public static IEnumerable<GameObject> Make(GameObject source, CraftingStation? bench)
        {
            foreach (KwPierKind kind in KwPierKinds.All)
                yield return Ladder(source, kind, bench);
        }

        /// <summary>The ladder prefab's name for a material.</summary>
        public static string Name(KwPierKind kind) => Prefix + kind.Id;

        private static GameObject Ladder(GameObject source, KwPierKind kind, CraftingStation? bench)
        {
            GameObject copy = PrefabBench.Copy(source, Name(kind));
            KwPrefabs.Strip(copy);
            GameObject model = KwPierPrefabs.Model(copy);
            Piece piece = copy.GetComponent<Piece>();
            KwPierPrefabs.Configure(piece, $"Dock ladder ({kind.Label.ToLowerInvariant()})", bench);
            piece.m_description = "A ladder hung from a dock's edge with the ladder tool, down to the water. Removing it gives back all it cost.";
            WearNTear wear = copy.GetComponent<WearNTear>();
            (wear.m_health, wear.m_materialType, wear.m_noSupportWear, wear.m_supports, wear.m_autoCreateFragments) = (kind.Health * 0.4f, kind.Wear, true, false, false);
            (wear.m_new, wear.m_worn, wear.m_broken, wear.m_wet, wear.m_fragmentRoots) = (model, model, model, null, null);
            copy.AddComponent<KwLadderClimb>();
            copy.AddComponent<KwPierLadder>().Kind = kind.Id;
            return copy;
        }
    }
}
