using System;
using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The dockside decorations (CLAUDE.md, "Dockside decorations"): the first eight here (a mooring post, a stone anchor, an
    /// oar rack, a shield rack, a net drying rack, lobster pots, a harbour beacon and a weather vane), then the shipwright's
    /// yard (<see cref="KwDockYard"/>) and the quay (<see cref="KwDockQuay"/>), decoration only (no storage, no station).
    /// Each is a copy of a game piece, its placement, health, wear and sounds kept: the game's table (<c>piece_table</c>,
    /// wood furniture with no component of its own) for all but the beacon, a copy of the standing brazier
    /// (<c>piece_brazierfloor01</c>) whose fire burns in its basket (<see cref="KwDockFire"/>); each wears its workshop model
    /// in the wood chest's material (<see cref="KwDockModel"/>), and the vane turns to the wind (<see cref="KwDockVane"/>);
    /// the tar pot and steam box burn a small fire (<see cref="KwDockHearth"/>), the lantern pole lights at night
    /// (<see cref="KwDockLantern"/>) and the bell rings (<see cref="KwDockBell"/>).
    /// Built at a workbench on land; on a launched ship only as any piece is, with the .cfg's AllowBuildingOnShips
    /// (<see cref="KwShipRiderRules"/>). Made once when the network scene first wakes, on every peer (the dedicated server
    /// too), registered every time it wakes; then the hammer is refreshed (<see cref="KwDockHammer"/>). A piece whose bundle
    /// or base is missing is left out with a warning; one that fails is left out alone, never stopping the scene's wake.
    /// </summary>
    public static class KwDockside
    {
        public const string Prefix = "BS_dock_";
        public const string Table = "piece_table";
        private const string Brazier = "piece_brazierfloor01", Chest = "piece_chest_wood", ChestBody = "woodchest", Workbench = "piece_workbench";
        private static readonly List<GameObject> built = new List<GameObject>();
        private static bool made;

        // the first eight (2026-11-02), by the water and on the docks
        private static readonly KwDockDef[] Harbour =
        {
            new KwDockDef("mooringpost", "Mooring post", "A thick oak bollard, iron-banded, with a hawser wound round it.", 0.4f, ("Wood", 6), ("LeatherScraps", 2))
            {
                Fit = KwDockMooring.Fit,
                Decor = new KwDecorFit(0.45f, 0.45f, 1.05f, KwDecorPlace.Shore) { Spacing = 0.8f, Wet = true },
            },
            new KwDockDef("anchor", "Stone anchor", "A killick anchor: a stone held in a lashed timber frame, its line coiled.", 0.6f, ("Stone", 6), ("Wood", 4), ("LeatherScraps", 2))
            {
                Decor = new KwDecorFit(1.6f, 0.9f, 0.45f, KwDecorPlace.Shore) { Spacing = 0.8f, Wet = true },
            },
            new KwDockDef("oarrack", "Oar rack", "Eight oars standing blades up in a rack.", 0f, ("Wood", 12))
            {
                Decor = new KwDecorFit(2f, 0.7f, 2.9f, KwDecorPlace.Deck | KwDecorPlace.Ground) { Spacing = 0.8f },
            },
            new KwDockDef("shieldrack", "Shield rack", "Four painted round shields hung ready for a ship's rail.", 0.3f, ("Wood", 10), ("Resin", 2), ("LeatherScraps", 4))
            {
                Decor = new KwDecorFit(2f, 0.6f, 1.3f, KwDecorPlace.Deck | KwDecorPlace.Ground) { Spacing = 0.8f },
            },
            new KwDockDef("netrack", "Net drying rack", "A fishing net drying over a pole, cork floats along its head and stones at its foot.", 0f, ("Wood", 6), ("LeatherScraps", 4), ("Stone", 2))
            {
                Decor = new KwDecorFit(3.4f, 0.8f, 2.3f, KwDecorPlace.Ground | KwDecorPlace.Deck | KwDecorPlace.Shore) { Spacing = 1f },
            },
            new KwDockDef("creels", "Lobster pots", "Three domed lobster pots, a marker buoy and a coil of line.", 0f, ("Wood", 6), ("LeatherScraps", 3), ("Resin", 2))
            {
                Decor = new KwDecorFit(1.3f, 0.8f, 0.9f, KwDecorPlace.Deck | KwDecorPlace.Ground | KwDecorPlace.Shore) { Spacing = 0.6f },
            },
            new KwDockDef("beacon", "Harbour beacon", "A landing beacon: an iron fire basket on a tall post, fed like a standing brazier.", 0.4f, ("Wood", 10), ("Bronze", 2))
            {
                Base = Brazier, Usage = Piece.UsageTagFlags.Lighting, KeepsComfort = true, Fit = KwDockFire.Light,
                Decor = new KwDecorFit(1.2f, 1.2f, 4.6f, KwDecorPlace.Deck | KwDecorPlace.Ground | KwDecorPlace.Shore) { Spacing = 1.5f },
            },
            new KwDockDef("vane", "Weather vane", "A gilt vane on a tall post that turns to the wind.", 0.7f, ("Wood", 6), ("Stone", 4), ("Bronze", 2))
            {
                Fit = KwDockVane.Fit,
                Decor = new KwDecorFit(1f, 1f, 4.2f, KwDecorPlace.Ground | KwDecorPlace.Deck | KwDecorPlace.Shore) { Spacing = 1.5f },
            },
        };

        /// <summary>
        /// The decorations in the hammer's order: the first eight, the yard's, the quay's; each made of a dock's wood followed
        /// by its core wood and fine wood copies (<see cref="KwDockTiers"/>).
        /// </summary>
        public static readonly KwDockDef[] Defs = KwDockTiers.Expand(Harbour.Concat(KwDockYard.Defs).Concat(KwDockQuay.Defs));

        /// <summary>The decoration pieces, once made.</summary>
        public static IReadOnlyList<GameObject> All => built;

        /// <summary>Once, from the plugin, after the ammunition piles (<see cref="KwPiles.Install"/>).</summary>
        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside dockside decorations", () => Build(scene)));

        private static void Build(ZNetScene scene)
        {
            if (!made)
            {
                made = true;
                Create(scene);
            }
            foreach (GameObject prefab in built)
                NetPrefabs.Register(scene, prefab);
            KwHammer.Refresh();
        }

        private static void Create(ZNetScene scene)
        {
            GameObject? chest = scene.GetPrefab(Chest);
            Material? wood = KwIcons.CanRender ? GameMaterials.Borrow(chest, ChestBody) : null;
            if (KwIcons.CanRender && wood == null)
                Log.Warn($"Broadside: no {ChestBody} material on the game's {Chest}; the dockside decorations wear their bundles' placeholders.");
            CraftingStation? bench = scene.GetPrefab(Workbench)?.GetComponent<CraftingStation>();
            foreach (KwDockDef def in Defs)
            {
                GameObject? piece = Attempt(scene, def, wood, bench);
                if (piece != null)
                    built.Add(piece);
            }
            KwDockTiers.Icons(built);
            Log.Info($"Broadside: {built.Count} of {Defs.Length} dockside decorations built.");
        }

        // One decoration; null (logged) when it could not be made, so the others still are.
        private static GameObject? Attempt(ZNetScene scene, KwDockDef def, Material? wood, CraftingStation? bench)
        {
            try
            {
                return Make(scene, def, wood, bench);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: {def.Prefab} could not be made and is left out: {e}");
                return null;
            }
        }

        private static GameObject? Make(ZNetScene scene, KwDockDef def, Material? wood, CraftingStation? bench)
        {
            (GameObject Model, Sprite? Icon)? look = KwDockModel.Load(def.Bundle);
            GameObject? source = scene.GetPrefab(def.Base);
            if (look == null)
                return null;
            if (source == null || source.GetComponent<Piece>() == null || source.GetComponent<WearNTear>() == null)
            {
                Log.Warn($"Broadside: the game's {def.Base} was not found; no {def.Label}.");
                return null;
            }
            GameObject copy = PrefabBench.Copy(source, def.Prefab);
            Configure(copy.GetComponent<Piece>(), def, bench);
            KwDockModel.Wear(copy, look.Value.Model, look.Value.Icon, def, wood);
            return copy;
        }

        // In Broadside's build menu entry (Transportation's flag) and the game's Decoration filter, at a workbench.
        private static void Configure(Piece piece, KwDockDef def, CraftingStation? bench)
        {
            (piece.m_name, piece.m_description) = (def.Label, def.Description);
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_usage = Piece.UsageTagFlags.Transport | Piece.UsageTagFlags.Decor | def.Usage;
            piece.m_enabled = true;
            if (bench != null)
                piece.m_craftingStation = bench;
            else
                Log.Warn($"Broadside: the game's {Workbench} was not found; {def.Prefab} keeps its base's station.");
            if (!def.KeepsComfort)
                (piece.m_comfort, piece.m_comfortGroup, piece.m_comfortObject) = (0, Piece.ComfortGroup.None, null);
            if (def.NotOnWood)
                piece.m_notOnWood = true;
            // made for the water (the auto-decorate metadata's Wet): placed at the waterline and in shallow water, by hand too
            if (def.Decor != null && def.Decor.Wet)
                piece.m_noInWater = false;
            // its recipe is set with the hammer (KwDockHammer), once the item database is up
            piece.m_resources = new Piece.Requirement[0];
        }
    }
}
