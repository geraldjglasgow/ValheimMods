using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The storage crates (CLAUDE.md, "Storage crates"): a small (4 x 2 slots) and a large (6 x 3) rope-lashed plank crate,
    /// and a small and a large slatted crate, each slatted crate wearing marks of its own (<see cref="KwCrateCoat"/>),
    /// copies of the game's wood chest (its Container, whose items the game keeps in the ZDO so every player sees the
    /// same, its placement, health, wear and sounds) wearing their workshop models (<see cref="KwCrateModel"/>), the lid
    /// opening while someone has the crate open (<see cref="KwCrateLid"/>). Built on land like any chest and, unlike the
    /// game's pieces, on a launched ship too, which it then rides (<see cref="KwShipRiderRules"/>). Made once when the
    /// network scene first wakes, after the cannons, on every peer (the dedicated server too), registered every time it
    /// wakes; then the hammer is refreshed (<see cref="KwCrateHammer"/>). Every name starts with <c>BS_</c>, never
    /// <c>KW_</c> (a ship part's). Without its bundle a crate keeps the wood chest's look (logged once).
    /// </summary>
    public static class KwCrates
    {
        public const string Prefix = "BS_crate_";
        private const string Source = "piece_chest_wood";
        private static readonly List<GameObject> built = new List<GameObject>();

        /// <summary>The crates, smallest first: the hammer's order.</summary>
        public static readonly KwCrateDef[] Defs =
        {
            new KwCrateDef("small", "Small storage crate", 4, 2, ("Wood", 8), ("LeatherScraps", 2)),
            new KwCrateDef("large", "Large storage crate", 6, 3, ("FineWood", 10), ("LeatherScraps", 4)),
            new KwCrateDef("slat_small", "Small slatted crate", 4, 2, ("Wood", 10)) { Marked = true },
            new KwCrateDef("slat_large", "Large slatted crate", 6, 3, ("FineWood", 12)) { Marked = true },
        };

        /// <summary>The crate pieces, once made.</summary>
        public static IReadOnlyList<GameObject> All => built;

        /// <summary>Once, from the plugin, after the cannons (<see cref="KwCannonPrefabs.Install"/>).</summary>
        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside storage crates", () => Build(scene)));

        private static void Build(ZNetScene scene)
        {
            if (built.Count == 0)
                Create(scene);
            foreach (GameObject prefab in built)
                NetPrefabs.Register(scene, prefab);
            KwHammer.Refresh();
        }

        private static void Create(ZNetScene scene)
        {
            GameObject? chest = scene.GetPrefab(Source);
            if (chest == null || chest.GetComponent<Container>() == null || chest.GetComponent<Piece>() == null)
            {
                Log.Warn($"Broadside: the game's {Source} was not found; no storage crates.");
                return;
            }
            foreach (KwCrateDef def in Defs)
                built.Add(Make(chest, def));
            Log.Info($"Broadside: {built.Count} storage crates built.");
        }

        private static GameObject Make(GameObject chest, KwCrateDef def)
        {
            GameObject copy = PrefabBench.Copy(chest, def.Prefab);
            Container box = copy.GetComponent<Container>();
            (box.m_name, box.m_width, box.m_height) = (def.Label, def.Width, def.Height);
            Piece piece = copy.GetComponent<Piece>();
            (piece.m_name, piece.m_description) = (def.Label, def.Description);
            // in the Misc tab with Broadside's pieces; the chest's own build menu filter kept, the Transportation one added
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_usage |= Piece.UsageTagFlags.Transport;
            if (!KwCrateModel.Wear(copy, chest, def.Bundle, def.Marked))
                return copy;
            // the model's own lid opens instead of the chest's open and closed states, which went with its children
            (box.m_open, box.m_closed) = (null, null);
            copy.AddComponent<KwCrateLid>();
            if (def.Marked)
                copy.AddComponent<KwCrateCoat>().Bundle = def.Bundle;
            return copy;
        }
    }
}
